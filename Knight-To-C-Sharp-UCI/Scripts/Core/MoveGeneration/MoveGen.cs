using System.Collections;
using System.Collections.Generic;

public class MoveGenerator
{
    static readonly int[] directionOffsets = PreComputedData.Directions;

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
    ulong friendlyAll;
    ulong enemyAll;
    ulong allBitboard;
    ulong friendlyStraightSliders;
    ulong friendlyDiagonalSliders;
    ulong enemyStraightSliders;
    ulong enemyDiagonalSliders;

    ulong emptySquares;
    ulong emptyOrEnemySquares;
    ulong moveTypeMask;
    
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

        if (!inDoubleCheck)
        {
            GenerateSlidingMoves();
            GenerateKnightMoves();
            
            GeneratePawnMoves();
        }

        

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
        friendlyBishopIndex = !turn ? PieceIndex.BlackBishop : PieceIndex.WhiteBishop;
        friendlyRookIndex = !turn ? PieceIndex.BlackRook : PieceIndex.WhiteRook;
        friendlyQueenIndex = !turn ? PieceIndex.BlackQueen : PieceIndex.WhiteQueen;

        enemyBishopIndex = turn ? PieceIndex.BlackBishop : PieceIndex.WhiteBishop;
        enemyRookIndex = turn ? PieceIndex.BlackRook : PieceIndex.WhiteRook;
        enemyQueenIndex = turn ? PieceIndex.BlackQueen : PieceIndex.WhiteQueen;

        // Bitboards
        friendlyAll = bitboards[turn ? PieceIndex.WhiteAll : PieceIndex.BlackAll]; 
        enemyAll = bitboards[!turn ? PieceIndex.WhiteAll : PieceIndex.BlackAll]; 
        allBitboard = friendlyAll | enemyAll;
        friendlyStraightSliders = bitboards[friendlyRookIndex] | bitboards[friendlyQueenIndex];
        friendlyDiagonalSliders = bitboards[friendlyBishopIndex] | bitboards[friendlyQueenIndex];
        enemyStraightSliders = bitboards[enemyRookIndex] | bitboards[enemyQueenIndex];
        enemyDiagonalSliders = bitboards[enemyBishopIndex] | bitboards[enemyQueenIndex];

        emptySquares = ulong.MaxValue ^ allBitboard;
        emptyOrEnemySquares = ulong.MaxValue ^ friendlyAll;
        
        moveTypeMask = genQuietMoves ? ulong.MaxValue : enemyAll;
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
        // Limit the moves to empty or enemy squares, resolve the check if the king is in check
        ulong moveMask = emptyOrEnemySquares & checkRayBitmask & moveTypeMask;

        ulong straight = friendlyStraightSliders;
        ulong diagonal = friendlyDiagonalSliders;

        if (inCheck)
        {
            straight &= ~pinRayBitmask;
            diagonal &= ~pinRayBitmask;
        }

        // Straight
        while (straight != 0)
        {
            int startSquare = Bitboard.PopLSB(ref straight);
            ulong moveSquares = Magic.GetRookAttacks(startSquare, allBitboard) & moveMask;

            // If piece is pinned, it can only move along the pin ray
            if (IsPinned(startSquare))
            {
                moveSquares &= PreComputedData.AlignMask[startSquare, friendlyKingSquare];
            }

            while (moveSquares != 0)
            {
                int targetSquare = Bitboard.PopLSB(ref moveSquares);
                moves.Add(new Move(startSquare, targetSquare));
            }
        }

        // Diagonal
        while (diagonal != 0)
        {
            int startSquare = Bitboard.PopLSB(ref diagonal);
            ulong moveSquares = Magic.GetBishopAttacks(startSquare, allBitboard) & moveMask;

            // If piece is pinned, it can only move along the pin ray
            if (IsPinned(startSquare))
            {
                moveSquares &= PreComputedData.AlignMask[startSquare, friendlyKingSquare];
            }

            while (moveSquares != 0)
            {
                int targetSquare = Bitboard.PopLSB(ref moveSquares);
                moves.Add(new Move(startSquare, targetSquare));
            }
        }
    }
    
    void GenerateKingMoves()
    {
        ulong legalMask = ~(enemyAttackMap | friendlyAll);
        ulong kingMoves = PreComputedData.KingMap[friendlyKingSquare] & legalMask & moveTypeMask;

        while (kingMoves != 0)
        {
            int targetSquare = Bitboard.PopLSB(ref kingMoves);
            moves.Add(new Move(friendlyKingSquare, targetSquare));
        }

        // Castling
        if (genQuietMoves && !inCheck)
        {
            ulong castlingBlockers = enemyAttackMap | allBitboard;

            // Kingside Castling
            if (kingsideCastling)
            {
                ulong castlingMask = turn ? PreComputedData.WhiteKingSideCastlingMask : PreComputedData.BlackKingSideCastlingMask;
                if ((castlingMask & castlingBlockers) == 0)
                {
                    int targetSquare = turn ? 6 : 62;
                    moves.Add(new Move(friendlyKingSquare, targetSquare, MoveFlag.Castling));
                }
            }
            // Queenside Castling
            if (queensideCastling)
            {
                ulong castlingMask = turn ? PreComputedData.WhiteQueenSideCastlingMask : PreComputedData.BlackQueenSideCastlingMask;
                ulong castlingBlockMask = turn ? PreComputedData.WhiteQueenSideCastlingBlockMask : PreComputedData.BlackQueenSideCastlingBlockMask;
                if (((castlingMask & enemyAttackMap) == 0) && ((castlingBlockMask & allBitboard) == 0))
                {
                    int targetSquare = turn ? 2 : 58;
                    moves.Add(new Move(friendlyKingSquare, targetSquare, MoveFlag.Castling));
                }
            }
        }
    }

    void GenerateKnightMoves()
    {
        ulong knights = bitboards[turn ? PieceIndex.WhiteKnight : PieceIndex.BlackKnight] & ~pinRayBitmask;

        ulong moveMask = emptyOrEnemySquares & checkRayBitmask & moveTypeMask;

        while (knights != 0)
        {
            int knightSquare = Bitboard.PopLSB(ref knights);
            ulong moveSquares = PreComputedData.KnightMap[knightSquare] & moveMask;

            while (moveSquares != 0)
            {
                int targetSquare = Bitboard.PopLSB(ref moveSquares);
                moves.Add(new Move(knightSquare, targetSquare));
            }
        }
    }

    void GeneratePawnMoves()
    {
        int pushDir = turn ? 1 : -1;
        int pushOffset = pushDir * 8;

        ulong pawns = bitboards[turn ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];

        ulong promotionRankMask = turn ? PreComputedData.Rank8 : PreComputedData.Rank1;

        ulong singlePush = (Bitboard.Shift(pawns, pushOffset)) & emptySquares;

        ulong pushPromotions = singlePush & promotionRankMask & checkRayBitmask;


        ulong captureEdgeFileMask = turn ? PreComputedData.NotFileA : PreComputedData.NotFileH;
        ulong captureEdgeFileMask2 = turn ? PreComputedData.NotFileH : PreComputedData.NotFileA;
        ulong captureA = Bitboard.Shift(pawns & captureEdgeFileMask, pushDir * 7) & enemyAll;
        ulong captureB = Bitboard.Shift(pawns & captureEdgeFileMask2, pushDir * 9) & enemyAll;

        ulong singlePushNoPromotions = singlePush & ~promotionRankMask & checkRayBitmask;

        ulong capturePromotionsA = captureA & promotionRankMask & checkRayBitmask;
        ulong capturePromotionsB = captureB & promotionRankMask & checkRayBitmask;

        captureA &= checkRayBitmask & ~promotionRankMask;
        captureB &= checkRayBitmask & ~promotionRankMask;

        // Single / double push
        if (genQuietMoves)
        {
            // Generate single pawn pushes
            while (singlePushNoPromotions != 0)
            {
                int targetSquare = Bitboard.PopLSB(ref singlePushNoPromotions);
                int startSquare = targetSquare - pushOffset;
                if (!IsPinned(startSquare) || PreComputedData.AlignMask[startSquare, friendlyKingSquare] == PreComputedData.AlignMask[targetSquare, friendlyKingSquare])
                {
                    moves.Add(new Move(startSquare, targetSquare));
                }
            }

            // Generate double pawn pushes
            ulong doublePushTargetRankMask = PreComputedData.Rank1 << (turn ? 24 : 32);
            ulong doublePush = Bitboard.Shift(singlePush, pushOffset) & emptySquares & doublePushTargetRankMask & checkRayBitmask;

            while (doublePush != 0)
            {
                int targetSquare = Bitboard.PopLSB(ref doublePush);
                int startSquare = targetSquare - pushOffset * 2;
                if (!IsPinned(startSquare) || PreComputedData.AlignMask[startSquare, friendlyKingSquare] == PreComputedData.AlignMask[targetSquare, friendlyKingSquare])
                {
                    moves.Add(new Move(startSquare, targetSquare, MoveFlag.PawnTwoForward));
                }
            }
        }

        // Captures
        while (captureA != 0)
        {
            int targetSquare = Bitboard.PopLSB(ref captureA);
            int startSquare = targetSquare - pushDir * 7;

            if (!IsPinned(startSquare) || PreComputedData.AlignMask[startSquare, friendlyKingSquare] == PreComputedData.AlignMask[targetSquare, friendlyKingSquare])
            {
                moves.Add(new Move(startSquare, targetSquare));
            }
        }

        while (captureB != 0)
        {
            int targetSquare = Bitboard.PopLSB(ref captureB);
            int startSquare = targetSquare - pushDir * 9;

            if (!IsPinned(startSquare) || PreComputedData.AlignMask[startSquare, friendlyKingSquare] == PreComputedData.AlignMask[targetSquare, friendlyKingSquare])
            {
                moves.Add(new Move(startSquare, targetSquare));
            }
        }

        // Promotions
        while (pushPromotions != 0)
        {
            int targetSquare = Bitboard.PopLSB(ref pushPromotions);
            int startSquare = targetSquare - pushOffset;
            if (!IsPinned(startSquare))
            {
                GeneratePromotionMoves(startSquare, targetSquare);
            }
        }


        while (capturePromotionsA != 0)
        {
            int targetSquare = Bitboard.PopLSB(ref capturePromotionsA);
            int startSquare = targetSquare - pushDir * 7;

            if (!IsPinned(startSquare) || PreComputedData.AlignMask[startSquare, friendlyKingSquare] == PreComputedData.AlignMask[targetSquare, friendlyKingSquare])
            {
                GeneratePromotionMoves(startSquare, targetSquare);
            }
        }

        while (capturePromotionsB != 0)
        {
            int targetSquare = Bitboard.PopLSB(ref capturePromotionsB);
            int startSquare = targetSquare - pushDir * 9;

            if (!IsPinned(startSquare) || PreComputedData.AlignMask[startSquare, friendlyKingSquare] == PreComputedData.AlignMask[targetSquare, friendlyKingSquare])
            {
                GeneratePromotionMoves(startSquare, targetSquare);
            }
        }

        // En passant
        if (board.EnpassantFile != 8)
        {
            int epFileIndex = board.EnpassantFile;
            int epRankIndex = turn ? 5 : 2;
            int targetSquare = epRankIndex * 8 + epFileIndex;
            int capturedPawnSquare = targetSquare - pushOffset;

            if (Bitboard.Contains(checkRayBitmask, capturedPawnSquare))
            {
                ulong pawnsThatCanCaptureEp = pawns & (turn ? PreComputedData.blackPawnAttackMap[targetSquare] : PreComputedData.whitePawnAttackMap[targetSquare]);

                while (pawnsThatCanCaptureEp != 0)
                {
                    int startSquare = Bitboard.PopLSB(ref pawnsThatCanCaptureEp);
                    if (!IsPinned(startSquare) || PreComputedData.AlignMask[startSquare, friendlyKingSquare] == PreComputedData.AlignMask[targetSquare, friendlyKingSquare])
                    {
                        if (!InCheckAfterEnPassant(startSquare, targetSquare, capturedPawnSquare))
                        {
                            moves.Add(new Move(startSquare, targetSquare, MoveFlag.EnpassantCapture));
                        }
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

        // Generate only Queen Promotion in QSearch
        if (genQuietMoves)
        {
            moves.Add(new Move(startSquare, targetSquare, MoveFlag.PromoteToRook));
            moves.Add(new Move(startSquare, targetSquare, MoveFlag.PromoteToKnight));
            moves.Add(new Move(startSquare, targetSquare, MoveFlag.PromoteToBishop));
        }
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
            if ((PreComputedData.DirRayMask[dir, friendlyKingSquare] & sliders) == 0)
            {
                continue;
            }

            int n = PreComputedData.NumSquaresToEdge[friendlyKingSquare, dir];
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
            enemyKnightAttackMap |= PreComputedData.KnightMap[startSquare];

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

        enemyAttackMapNoPawns = enemySlidingAttackMap | enemyKnightAttackMap | PreComputedData.KingMap[enemyKingSquare];
        enemyAttackMap = enemyAttackMapNoPawns | enemyPawnAttackMap;

        if (!inCheck)
        {
            checkRayBitmask = ulong.MaxValue;
        }
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
        int moveDir = PreComputedData.DirectionLookup[targetSquare - startSquare + 63];
        
		return dirOffset == moveDir || -dirOffset == moveDir;
    }

    bool IsHorizontalChecked()
    {
        for (int n = 0; n < PreComputedData.NumSquaresToEdge[friendlyKingSquare, 0]; n++)
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

        for (int n = 0; n < PreComputedData.NumSquaresToEdge[friendlyKingSquare, 2]; n++)
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
    bool InCheckAfterEnPassant(int startSquare, int targetSquare, int epCaptureSquare)
    {
        ulong enemyStraight = enemyStraightSliders;

        if (enemyStraight != 0)
        {
            ulong maskedBlockers = allBitboard ^ (1ul << epCaptureSquare | 1ul << startSquare | 1ul << targetSquare);
            ulong rookAttacks = Magic.GetRookAttacks(friendlyKingSquare, maskedBlockers);
            return (rookAttacks & enemyStraight) != 0;
        }

        return false;
    }
}
