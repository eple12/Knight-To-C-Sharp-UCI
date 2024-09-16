using System.Collections;
using System.Collections.Generic;

public class MoveGenerator
{
    static readonly int[] directionOffsets = PreComputedData.directions;

    // VARIABLES USED IN MOVE GENERATION
    List<Move> moves = new List<Move>();

    bool genQuietMoves;

    int[] position;
    Board board;
    bool turn;
    int friendlyColor;
    int enemyColor;
    int friendlyKingSquare;

    ulong enemyAttackMap;
    ulong enemyAttackMapNoPawns;
    ulong enemySlidingAttackMap;
    ulong enemyKnightAttackMap;
    ulong enemyPawnAttackMap;

    bool pinsExistInPosition;
    ulong pinRayBitmask;

    ulong checkRayBitmask;
    bool inCheck;
    bool inDoubleCheck;

    bool kingsideCastling;
    bool queensideCastling;

    // Piece Index
    int friendlyBishopIndex;
    int friendlyRookIndex;
    int friendlyQueenIndex;
    int enemyBishopIndex;
    int enemyRookIndex;
    int enemyQueenIndex;


    // Bitboards
    ulong[] bitboards;
    ulong allBitboard;
    ulong enemyStraightSliders;
    ulong enemyDiagonalSliders;
    
    public MoveGenerator(Board board)
    {
        this.board = board;
        position = board.Squares;
        bitboards = board.BitboardSet.Bitboards;
    }

    public List<Move> GenerateMoves(bool genOnlyCaptures = false)
    {
        genQuietMoves = !genOnlyCaptures;

        Initialize();

        CalculateAttackData();
        
        GenerateKingMoves();

        if (inDoubleCheck)
        {
            return moves;
        }

        GenerateSlidingMoves();
        GenerateKnightMoves();
        
        GeneratePawnMoves();

        return moves;
    }

    void Initialize()
    {
        moves = new List<Move>();

        position = board.Squares;
        bitboards = board.BitboardSet.Bitboards;

        turn = board.Turn;

        friendlyColor = turn ? Piece.White : Piece.Black;
        enemyColor = turn ? Piece.Black : Piece.White;

        enemyAttackMap = 0;
        enemyAttackMapNoPawns = 0;
        enemySlidingAttackMap = 0;
        enemyKnightAttackMap = 0;
        enemyPawnAttackMap = 0;

        friendlyKingSquare = board.PieceSquares[turn ? PieceIndex.WhiteKing : PieceIndex.BlackKing].squares[0];

        pinsExistInPosition = false;
        pinRayBitmask = 0;

        checkRayBitmask = 0;

        inCheck = false;
        inDoubleCheck = false;

        kingsideCastling = turn ? board.WKCastle : board.BKCastle;
        queensideCastling = turn ? board.WQCastle : board.BQCastle;

        // Bitboard Index
        friendlyBishopIndex = turn ? PieceIndex.BlackBishop : PieceIndex.WhiteBishop;
        friendlyRookIndex = turn ? PieceIndex.BlackRook : PieceIndex.WhiteRook;
        friendlyQueenIndex = turn ? PieceIndex.BlackQueen : PieceIndex.WhiteQueen;

        enemyBishopIndex = turn ? PieceIndex.BlackBishop : PieceIndex.WhiteBishop;
        enemyRookIndex = turn ? PieceIndex.BlackRook : PieceIndex.WhiteRook;
        enemyQueenIndex = turn ? PieceIndex.BlackQueen : PieceIndex.WhiteQueen;

        // Bitboards
        allBitboard = bitboards[PieceIndex.WhiteAll] | bitboards[PieceIndex.BlackAll];
        enemyStraightSliders = bitboards[enemyRookIndex] | bitboards[enemyQueenIndex];
        enemyDiagonalSliders = bitboards[enemyBishopIndex] | bitboards[enemyQueenIndex];
    }

    // Note: This will only return correct value only after GenerateMoves() call.
    public bool InCheck()
    {
        return inCheck;
    }

    public ulong PawnAttackMap()
    {
        return enemyPawnAttackMap;
    }

    public ulong AttackMapNoPawn()
    {
        return enemyAttackMapNoPawns;
    }

    void GenerateSlidingMoves()
    {
        PieceList rooks = board.PieceSquares[turn ? PieceIndex.WhiteRook : PieceIndex.BlackRook];
        for (int i = 0; i < rooks.count; i++) {
            GenerateSingleSlider(rooks.squares[i], 0, 4);
        }

        PieceList bishops = board.PieceSquares[turn ? PieceIndex.WhiteBishop : PieceIndex.BlackBishop];
        for (int i = 0; i < bishops.count; i++) {
            GenerateSingleSlider(bishops.squares[i], 4, 8);
        }

        PieceList queens = board.PieceSquares[turn ? PieceIndex.WhiteQueen : PieceIndex.BlackQueen];
        for (int i = 0; i < queens.count; i++) {
            GenerateSingleSlider(queens.squares[i], 0, 8);
        }
    }

    void GenerateSingleSlider(in int startSquare, in int startDirIndex, in int endDirIndex)
    {
        bool isPinned = IsPinned(startSquare);

        // If this piece is pinned, and if the king is in check, this piece cannot move
        if (inCheck && isPinned)
        {
            return;
        }
        
        for (int dirIndex = startDirIndex; dirIndex < endDirIndex; dirIndex++)
        {
            int currentOffset = directionOffsets[dirIndex];

            // If this piece is pinned, it can only move along the pin ray
            if (isPinned && !IsMovingAlongRay(currentOffset, friendlyKingSquare, startSquare))
            {
                continue;
            }

            for (int n = 0; n < PreComputedData.numSquaresToEdge[startSquare, dirIndex]; n++)
            {
                int targetSquare = startSquare + currentOffset * (n + 1);

                // Blocked by a friendly piece; Move on to the next direction.
                if (Piece.IsColor(position[targetSquare], friendlyColor))
                {
                    break;
                }

                bool movePreventsCheck = SquareIsInCheckRay(targetSquare);

                if (movePreventsCheck || !inCheck)
                {
                    // Capturing enemy piece
                    if (Piece.IsColor(position[targetSquare], enemyColor))
                    {
                        moves.Add(new Move(startSquare, targetSquare));
                        break;
                    }
                    
                    if (genQuietMoves)
                    {
                        moves.Add(new Move(startSquare, targetSquare));
                    }
                }
                // Check, but this piece cannot help because it is blocked by an enemy piece
                else if (Piece.IsColor(position[targetSquare], enemyColor))
                {
                    break;
                }

                if (movePreventsCheck)
                {
                    break;
                }
            }
        }
    }
    
    void GenerateKingMoves()
    {
        for (int index = 0; index < PreComputedData.kingSquares[friendlyKingSquare].Count; index++)
        {
            int targetSquare = PreComputedData.kingSquares[friendlyKingSquare][index];

            if (Piece.IsColor(position[targetSquare], friendlyColor))
            {
                continue;
            }

            if (!Bitboard.Contains(enemyAttackMap, targetSquare))
            {
                if (genQuietMoves)
                {
                    moves.Add(new Move(friendlyKingSquare, targetSquare));
                }
                else
                {
                    if (Piece.IsColor(position[targetSquare], enemyColor))
                    {
                        moves.Add(new Move(friendlyKingSquare, targetSquare));
                    }
                }
            }
        }

        // Kingside Castling
        if (!inCheck && kingsideCastling)
        {
            int targetSquare = friendlyKingSquare + 2;

            if (position[targetSquare - 1] == Piece.None && position[targetSquare] == Piece.None && 
            !Bitboard.Contains(enemyAttackMap, targetSquare - 1) && !Bitboard.Contains(enemyAttackMap, targetSquare))
            {
                moves.Add(new Move(friendlyKingSquare, targetSquare, MoveFlag.Castling));
            }
        }
        // Queenside Castling
        if (!inCheck && queensideCastling)
        {
            int targetSquare = friendlyKingSquare - 2;

            if (position[targetSquare - 1] == Piece.None && position[targetSquare + 1] == Piece.None && 
            position[targetSquare] == Piece.None && 
            !Bitboard.Contains(enemyAttackMap, targetSquare + 1) && 
            !Bitboard.Contains(enemyAttackMap, targetSquare))
            {
                moves.Add(new Move(friendlyKingSquare, targetSquare, MoveFlag.Castling));
            }
        }
    }

    void GenerateKnightMoves()
    {
        PieceList knights = board.PieceSquares[turn ? PieceIndex.WhiteKnight : PieceIndex.BlackKnight];

        for (int i = 0; i < knights.count; i++)
        {
            int startSquare = knights.squares[i];

            // Knight cannot move if it is pinned
            if (IsPinned(startSquare))
            {
                continue;
            }

            for (int index = 0; index < PreComputedData.knightSquares[startSquare].Count; index++)
            {
                int targetSquare = PreComputedData.knightSquares[startSquare][index];

                if (Piece.IsColor(position[targetSquare], friendlyColor) || (inCheck && !SquareIsInCheckRay(targetSquare)))
                {
                    continue;
                }

                if (genQuietMoves)
                {
                    moves.Add(new Move(startSquare, targetSquare));
                }
                else
                {
                    if (Piece.IsColor(position[targetSquare], enemyColor))
                    {
                        moves.Add(new Move(startSquare, targetSquare));
                    }
                }
            }
        }
    }

    void GeneratePawnMoves()
    {
        PieceList pawns = board.PieceSquares[turn ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];

        for (int i = 0; i < pawns.count; i++)
        {
            int startSquare = pawns.squares[i];
            int pushOffset = turn ? 8 : -8;

            int targetSquare = startSquare + pushOffset;

            bool generatePromotionMoves = startSquare / 8 == (turn ? 6 : 1);

            // Forward movements
            if (position[targetSquare] == Piece.None && genQuietMoves)
            {
                // This pawn is not pinned, or it is moving along the pin ray
                if (!IsPinned(startSquare) || IsMovingAlongRay(pushOffset, friendlyKingSquare, startSquare))
                {
                    // The king is not in check, or this pawn is going to block the check ray
                    if (!inCheck || SquareIsInCheckRay(targetSquare))
                    {
                        if (generatePromotionMoves)
                        {
                            GeneratePromotionMoves(startSquare, targetSquare);
                        }
                        else
                        {
                            moves.Add(new Move(startSquare, targetSquare));
                        }
                    }

                    // Two squares forward
                    if ((startSquare / 8 == (turn ? 1 : 6)) && position[targetSquare + pushOffset] == Piece.None)
                    {
                        // The king is not in check, or this pawn is going to block the check ray
                        if (!inCheck || SquareIsInCheckRay(targetSquare + pushOffset))
                        {
                            moves.Add(new Move(startSquare, targetSquare + pushOffset, MoveFlag.PawnTwoForward));
                        }
                    }
                }
            }

            // Capture Right
            if (startSquare % 8 < 7)
            {
                int captureOffset = turn ? 9 : - 7;
                targetSquare = startSquare + captureOffset;

                // This pawn is not pinned, or it is moving along the pin ray
                if (!IsPinned(startSquare) || IsMovingAlongRay(captureOffset, friendlyKingSquare, startSquare))
                {
                    // The king is not in check, or this pawn is going to block the check ray
                    if (!inCheck || SquareIsInCheckRay(targetSquare))
                    {
                        // There is an enemy piece
                        if (Piece.IsColor(position[targetSquare], enemyColor))
                        {
                            if (generatePromotionMoves)
                            {
                                GeneratePromotionMoves(startSquare, targetSquare);
                            }
                            else
                            {
                                moves.Add(new Move(startSquare, targetSquare));
                            }
                        }
                    }

                    // En passant
                    if (targetSquare == Square.EnpassantCaptureIndex(board.EnpassantFile, turn) && (!inCheck || SquareIsInCheckRay(targetSquare) || SquareIsInCheckRay(targetSquare + (turn ? - 8 : 8))))
                    {
                        position[targetSquare + (turn ? - 8 : 8)] = Piece.None;
                        position[startSquare] = Piece.None;

                        if (!IsHorizontalChecked())
                        {
                            moves.Add(new Move(startSquare, targetSquare, MoveFlag.EnpassantCapture));
                        }

                        position[targetSquare + (turn ? - 8 : 8)] = (turn ? Piece.Black : Piece.White) | Piece.Pawn;
                        position[startSquare] = (turn ? Piece.White : Piece.Black) | Piece.Pawn;
                    }
                }
            }
            // Capture Left
            if (startSquare % 8 > 0)
            {
                int captureOffset = turn ? 7 : - 9;
                targetSquare = startSquare + captureOffset;

                // This pawn is not pinned, or it is moving along the pin ray
                if (!IsPinned(startSquare) || IsMovingAlongRay(captureOffset, friendlyKingSquare, startSquare))
                {
                    // The king is not in check, or this pawn is going to block the check ray
                    if (!inCheck || SquareIsInCheckRay(targetSquare))
                    {
                        // There is an enemy piece
                        if (Piece.IsColor(position[targetSquare], enemyColor))
                        {
                            if (generatePromotionMoves)
                            {
                                GeneratePromotionMoves(startSquare, targetSquare);
                            }
                            else
                            {
                                moves.Add(new Move(startSquare, targetSquare));
                            }
                        }
                    }

                    // En passant
                    if (targetSquare == Square.EnpassantCaptureIndex(board.EnpassantFile, turn) && (!inCheck || SquareIsInCheckRay(targetSquare) || SquareIsInCheckRay(targetSquare + (turn ? - 8 : 8))))
                    {
                        position[targetSquare + (turn ? - 8 : 8)] = Piece.None;
                        position[startSquare] = Piece.None;

                        if (!IsHorizontalChecked())
                        {
                            moves.Add(new Move(startSquare, targetSquare, MoveFlag.EnpassantCapture));
                        }

                        position[targetSquare + (turn ? - 8 : 8)] = (turn ? Piece.Black : Piece.White) | Piece.Pawn;
                        position[startSquare] = (turn ? Piece.White : Piece.Black) | Piece.Pawn;
                    }
                }
            }
        }
    }

    void GeneratePromotionMoves(int startSquare, int targetSquare)
    {
        // Generate promotion moves
        // Promote to: Queen, Rook, Knight, Bishop
        moves.Add(new Move(startSquare, targetSquare, MoveFlag.PromoteToQueen));
        moves.Add(new Move(startSquare, targetSquare, MoveFlag.PromoteToRook));
        moves.Add(new Move(startSquare, targetSquare, MoveFlag.PromoteToKnight));
        moves.Add(new Move(startSquare, targetSquare, MoveFlag.PromoteToBishop));
    }


    void CalculateAttackData()
    {
        CalculateSlidingAttackMap();

        int startDirIndex = 0;
        int endDirIndex = 8;
        
        // Pins / Checks for enemy sliding pieces
        // Skip directions if there is not a piece left to attack in that direction.
        // Only if there is no queen as a queen moves for all 8 directions.
        if (board.PieceSquares[enemyQueenIndex].count == 0)
        {
            startDirIndex = (board.PieceSquares[enemyRookIndex].count > 0) ? 0 : 4;
            endDirIndex = (board.PieceSquares[enemyBishopIndex].count > 0) ? 8 : 4;
        }

        // Check and Pin
        for (int dir = startDirIndex; dir < endDirIndex; dir++)
        {
            bool isDiagonal = dir > 3;
            ulong sliders = isDiagonal ? enemyDiagonalSliders : enemyStraightSliders;

            // No enemy piece that can attack in this direction
            if ((PreComputedData.dirRayMask[dir, friendlyKingSquare] & sliders) == 0)
            {
                continue;
            }

            int n = PreComputedData.numSquaresToEdge[friendlyKingSquare, dir];
            int directionOffset = directionOffsets[dir];
            bool isFriendlyPieceAlongRay = false;
            ulong rayMask = 0;

            for (int i = 0; i < n; i++) {
                int squareIndex = friendlyKingSquare + directionOffset * (i + 1);
                rayMask |= (ulong) 1 << squareIndex;
                int piece = position[squareIndex];

                // This square contains a friendly piece
                if (Piece.IsColor(piece, friendlyColor))
                {
                    // First friendly piece we have come across in this direction, so it might be pinned
                    if (!isFriendlyPieceAlongRay) {
                        isFriendlyPieceAlongRay = true;
                    }
                    // This is the second friendly piece we've found in this direction, therefore pin is not possible
                    else
                    {
                        break;
                    }
                }
                    
                // This square contains an enemy piece
                else if (Piece.IsColor(piece, enemyColor))
                {
                    // Check if piece is in bitmask of pieces able to move in current direction
                    if (isDiagonal && Piece.IsDiagonalPiece(piece) || !isDiagonal && Piece.IsStraightPiece(piece))
                    {
                        // Friendly piece blocks the check, so this is a pin
                        if (isFriendlyPieceAlongRay)
                        {
                            pinsExistInPosition = true;
                            pinRayBitmask |= rayMask;
                        }
                        // No friendly piece blocking the attack, so this is a check
                        else
                        {
                            checkRayBitmask |= rayMask;
                            inDoubleCheck = inCheck; // If already in check, then this is double check
                            inCheck = true;
                        }
                        break;
                    }
                    else
                    {
                        // This enemy piece is not able to move in the current direction, and so is blocking any checks/pins
                        break;
                    }
                }
            }

            // Stop searching for pins if in double check, as the king is the only piece able to move in that case anyway
            if (inDoubleCheck) {
                break;
            }
        }

        // Knight attacks
        PieceList opponentKnights = board.PieceSquares[turn ? PieceIndex.BlackKnight : PieceIndex.WhiteKnight];
        
        bool isKnightCheck = false;

        for (int knightIndex = 0; knightIndex < opponentKnights.count; knightIndex++)
        {
            int startSquare = opponentKnights.squares[knightIndex];
            enemyKnightAttackMap |= PreComputedData.knightMap[startSquare];

            if (!isKnightCheck && Bitboard.Contains(enemyKnightAttackMap, friendlyKingSquare))
            {
                isKnightCheck = true;
                inDoubleCheck = inCheck; // if already in check, then this is double check
                inCheck = true;
                checkRayBitmask |= (ulong) 1 << startSquare;
            }
        }

        // Pawn attacks
        PieceList opponentPawns = board.PieceSquares[turn ? PieceIndex.BlackPawn : PieceIndex.WhitePawn];
        
        bool isPawnCheck = false;

        for (int pawnIndex = 0; pawnIndex < opponentPawns.count; pawnIndex++) {
            int pawnSquare = opponentPawns.squares[pawnIndex];
            ulong pawnAttacks = turn ? PreComputedData.blackPawnAttackMap[pawnSquare] : PreComputedData.whitePawnAttackMap[pawnSquare];
            enemyPawnAttackMap |= pawnAttacks;

            if (!isPawnCheck && Bitboard.Contains(pawnAttacks, friendlyKingSquare)) {
                isPawnCheck = true;
                inDoubleCheck = inCheck; // If already in check, then this is double check
                inCheck = true;
                checkRayBitmask |= (ulong) 1 << pawnSquare;
            }
        }

        int enemyKingSquare = board.PieceSquares[turn ? PieceIndex.BlackKing : PieceIndex.WhiteKing].squares[0];

        enemyAttackMapNoPawns = enemySlidingAttackMap | enemyKnightAttackMap | PreComputedData.kingMap[enemyKingSquare];
        enemyAttackMap = enemyAttackMapNoPawns | enemyPawnAttackMap;
    }

    void CalculateSlidingAttackMap()
    {
        UpdateSlidingAttack(enemyStraightSliders, isDiagonal: false);
        UpdateSlidingAttack(enemyDiagonalSliders, isDiagonal: true);
    }

    void UpdateSlidingAttack(ulong pieces, bool isDiagonal)
    {
        ulong blockers = allBitboard & ~((ulong) 1 << friendlyKingSquare);
        // Bitboard.Print(blockers);

        while (pieces != 0)
        {
            int startSquare = Bitboard.PopLSB(ref pieces);
            ulong moveBoard = Magic.GetSliderAttacks(startSquare, blockers, isDiagonal);
            // Console.WriteLine($"start {Square.Name(startSquare)}");
            // Bitboard.Print(moveBoard);

            enemySlidingAttackMap |= moveBoard;
        }
    }

    bool IsPinned(int square)
    {
        return pinsExistInPosition && (pinRayBitmask & (ulong) 1 << square) != 0;
    }

    bool SquareIsInCheckRay (int square)
    {
        return inCheck && (checkRayBitmask & (ulong) 1 << square) != 0;
    }

    bool IsMovingAlongRay(int dirOffset, int startSquare, int targetSquare)
    {
        int moveDir = PreComputedData.directionLookup[targetSquare - startSquare + 63];
        
		return dirOffset == moveDir || -dirOffset == moveDir;
    }

    bool IsHorizontalChecked()
    {
        for (int n = 0; n < PreComputedData.numSquaresToEdge[friendlyKingSquare, 0]; n++)
        {
            if (position[friendlyKingSquare + n + 1] != Piece.None)
            {
                if (Piece.IsColor(position[friendlyKingSquare + n + 1], enemyColor) && 
                Piece.IsStraightPiece(position[friendlyKingSquare + n + 1]))
                {
                    return true;
                }

                break;
            }
        }

        for (int n = 0; n < PreComputedData.numSquaresToEdge[friendlyKingSquare, 2]; n++)
        {
            if (position[friendlyKingSquare - n - 1] != Piece.None)
            {
                if (Piece.IsColor(position[friendlyKingSquare - n - 1], enemyColor) && 
                Piece.IsStraightPiece(position[friendlyKingSquare - n - 1]))
                {
                    return true;
                }
                
                break;
            }
        }

        return false;
    }

}
