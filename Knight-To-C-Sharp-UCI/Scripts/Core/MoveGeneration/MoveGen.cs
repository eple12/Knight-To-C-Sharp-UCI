public class MoveGenerator
{
    public const int MaxMoves = 218;
    static readonly int[] directionOffsets = PreComputedMoveGenData.Directions;

    // Move array
    int currMoveIndex;

    // QSearch
    bool genQuietMoves;

    // Board
    Board board;
    int[] position;
    bool turn;
    int friendlyColor;
    int enemyColor;
    int friendlyKingSquare;

    // Attack Data
    ulong enemyAttackMap;
    ulong enemyAttackMapNoPawns;
    ulong enemySlidingAttackMap;
    ulong enemyKnightAttackMap;
    ulong enemyPawnAttackMap;

    // Pins
    bool pinsExistInPosition;
    ulong pinRayBitmask;

    // Checks
    ulong checkRayBitmask;
    bool inCheck;
    bool inDoubleCheck;

    // Castling rights
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
    // ulong[] bitboards;
    BitboardSet bitboards => board.BBSet;
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
        // bitboards = board.BBSet.Bitboards;
    }

    public Span<Move> GenerateMoves(bool genOnlyCaptures = false)
    {
        Span<Move> moves = new Move[MaxMoves];
        GenerateMoves(ref moves, genOnlyCaptures);
        return moves;
    }

    public Span<Move> GenerateMoves(ref Span<Move> moves, bool genOnlyCaptures = false)
    {
        genQuietMoves = !genOnlyCaptures;

        Initialize();

        CalculateAttackData();
        
        GenerateKingMoves(moves);

        if (!inDoubleCheck)
        {
            GenerateSlidingMoves(moves);
            GenerateKnightMoves(moves);
            
            GeneratePawnMoves(moves);
        }

        moves = moves.Slice(0, currMoveIndex);

        return moves;
    }

    void Initialize()
    {
        // moves = new List<Move>();
        currMoveIndex = 0;

        position = board.Squares;
        // bitboards = board.BBSet.Bitboards;

        turn = board.Turn;

        friendlyColor = turn ? PieceUtils.White : PieceUtils.Black;
        enemyColor = turn ? PieceUtils.Black : PieceUtils.White;

        enemyAttackMap = 0;
        enemyAttackMapNoPawns = 0;
        enemySlidingAttackMap = 0;
        enemyKnightAttackMap = 0;
        enemyPawnAttackMap = 0;

        friendlyKingSquare = board.PieceSquares[PieceIndex.MakeKing(turn)][0];

        pinsExistInPosition = false;
        pinRayBitmask = 0;

        checkRayBitmask = 0;

        inCheck = false;
        inDoubleCheck = false;

        kingsideCastling = turn ? board.WKCastle : board.BKCastle;
        queensideCastling = turn ? board.WQCastle : board.BQCastle;

        // Bitboard Index
        friendlyBishopIndex = PieceIndex.MakeBishop(turn);
        friendlyRookIndex = PieceIndex.MakeRook(turn);
        friendlyQueenIndex = PieceIndex.MakeQueen(turn);

        enemyBishopIndex = PieceIndex.MakeBishop(!turn);
        enemyRookIndex = PieceIndex.MakeRook(!turn);
        enemyQueenIndex = PieceIndex.MakeQueen(!turn);

        // Bitboards
        friendlyAll = bitboards[PieceIndex.MakeAll(turn)]; 
        enemyAll = bitboards[PieceIndex.MakeAll(!turn)]; 
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

    public ulong OpponentAttackMap()
    {
        return enemyAttackMap;
    }
    public ulong PawnAttackMap()
    {
        return enemyPawnAttackMap;
    }
    public ulong AttackMapNoPawn()
    {
        return enemyAttackMapNoPawns;
    }

    void GenerateSlidingMoves(Span<Move> moves)
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
            int startSquare = BitboardUtils.PopLSB(ref straight);
            ulong moveSquares = Magic.GetRookAttacks(startSquare, allBitboard) & moveMask;

            // If piece is pinned, it can only move along the pin ray
            if (IsPinned(startSquare))
            {
                moveSquares &= PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare];
            }

            while (moveSquares != 0)
            {
                int targetSquare = BitboardUtils.PopLSB(ref moveSquares);
                moves[currMoveIndex++] = new Move(startSquare, targetSquare);
            }
        }

        // Diagonal
        while (diagonal != 0)
        {
            int startSquare = BitboardUtils.PopLSB(ref diagonal);
            ulong moveSquares = Magic.GetBishopAttacks(startSquare, allBitboard) & moveMask;

            // If piece is pinned, it can only move along the pin ray
            if (IsPinned(startSquare))
            {
                moveSquares &= PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare];
            }

            while (moveSquares != 0)
            {
                int targetSquare = BitboardUtils.PopLSB(ref moveSquares);
                moves[currMoveIndex++] = new Move(startSquare, targetSquare);
            }
        }
    }
    
    void GenerateKingMoves(Span<Move> moves)
    {
        ulong legalMask = ~(enemyAttackMap | friendlyAll);
        ulong kingMoves = PreComputedMoveGenData.KingMap[friendlyKingSquare] & legalMask & moveTypeMask;

        while (kingMoves != 0)
        {
            int targetSquare = BitboardUtils.PopLSB(ref kingMoves);
            moves[currMoveIndex++] = new Move(friendlyKingSquare, targetSquare);
        }

        // Castling
        if (genQuietMoves && !inCheck)
        {
            ulong castlingBlockers = enemyAttackMap | allBitboard;

            // Kingside Castling
            if (kingsideCastling)
            {
                ulong castlingMask = turn ? PreComputedMoveGenData.WhiteKingSideCastlingMask : PreComputedMoveGenData.BlackKingSideCastlingMask;
                if ((castlingMask & castlingBlockers) == 0)
                {
                    int targetSquare = turn ? 6 : 62;
                    moves[currMoveIndex++] = new Move(friendlyKingSquare, targetSquare, MoveFlag.Castling);
                }
            }
            // Queenside Castling
            if (queensideCastling)
            {
                ulong castlingMask = turn ? PreComputedMoveGenData.WhiteQueenSideCastlingMask : PreComputedMoveGenData.BlackQueenSideCastlingMask;
                ulong castlingBlockMask = turn ? PreComputedMoveGenData.WhiteQueenSideCastlingBlockMask : PreComputedMoveGenData.BlackQueenSideCastlingBlockMask;
                if (((castlingMask & enemyAttackMap) == 0) && ((castlingBlockMask & allBitboard) == 0))
                {
                    int targetSquare = turn ? 2 : 58;
                    moves[currMoveIndex++] = new Move(friendlyKingSquare, targetSquare, MoveFlag.Castling);
                }
            }
        }
    }

    void GenerateKnightMoves(Span<Move> moves)
    {
        ulong knights = bitboards[turn ? PieceIndex.WhiteKnight : PieceIndex.BlackKnight] & ~pinRayBitmask;

        ulong moveMask = emptyOrEnemySquares & checkRayBitmask & moveTypeMask;

        while (knights != 0)
        {
            int knightSquare = BitboardUtils.PopLSB(ref knights);
            ulong moveSquares = PreComputedMoveGenData.KnightMap[knightSquare] & moveMask;

            while (moveSquares != 0)
            {
                int targetSquare = BitboardUtils.PopLSB(ref moveSquares);
                moves[currMoveIndex++] = new Move(knightSquare, targetSquare);
            }
        }
    }

    void GeneratePawnMoves(Span<Move> moves)
    {
        int pushDir = turn ? 1 : -1;
        int pushOffset = pushDir * 8;

        ulong pawns = bitboards[turn ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];

        ulong promotionRankMask = turn ? Bits.Rank8 : Bits.Rank1;

        ulong singlePush = (pawns.Shift(pushOffset)) & emptySquares;

        ulong pushPromotions = singlePush & promotionRankMask & checkRayBitmask;


        ulong captureEdgeFileMask = turn ? Bits.NotFileA : Bits.NotFileH;
        ulong captureEdgeFileMask2 = turn ? Bits.NotFileH : Bits.NotFileA;
        ulong captureA = (pawns & captureEdgeFileMask).Shift(pushDir * 7) & enemyAll;
        ulong captureB = (pawns & captureEdgeFileMask2).Shift(pushDir * 9) & enemyAll;

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
                int targetSquare = BitboardUtils.PopLSB(ref singlePushNoPromotions);
                int startSquare = targetSquare - pushOffset;
                if (!IsPinned(startSquare) || PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare] == PreComputedMoveGenData.AlignMask[targetSquare, friendlyKingSquare])
                {
                    moves[currMoveIndex++] = new Move(startSquare, targetSquare);
                }
            }

            // Generate double pawn pushes
            ulong doublePushTargetRankMask = Bits.Rank1 << (turn ? 24 : 32);
            ulong doublePush = singlePush.Shift(pushOffset) & emptySquares & doublePushTargetRankMask & checkRayBitmask;

            while (doublePush != 0)
            {
                int targetSquare = BitboardUtils.PopLSB(ref doublePush);
                int startSquare = targetSquare - pushOffset * 2;
                if (!IsPinned(startSquare) || PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare] == PreComputedMoveGenData.AlignMask[targetSquare, friendlyKingSquare])
                {
                    moves[currMoveIndex++] = new Move(startSquare, targetSquare, MoveFlag.PawnTwoForward);
                }
            }
        }

        // Captures
        while (captureA != 0)
        {
            int targetSquare = BitboardUtils.PopLSB(ref captureA);
            int startSquare = targetSquare - pushDir * 7;

            if (!IsPinned(startSquare) || PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare] == PreComputedMoveGenData.AlignMask[targetSquare, friendlyKingSquare])
            {
                moves[currMoveIndex++] = new Move(startSquare, targetSquare);
            }
        }

        while (captureB != 0)
        {
            int targetSquare = BitboardUtils.PopLSB(ref captureB);
            int startSquare = targetSquare - pushDir * 9;

            if (!IsPinned(startSquare) || PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare] == PreComputedMoveGenData.AlignMask[targetSquare, friendlyKingSquare])
            {
                moves[currMoveIndex++] = new Move(startSquare, targetSquare);
            }
        }

        // Promotions
        while (pushPromotions != 0)
        {
            int targetSquare = BitboardUtils.PopLSB(ref pushPromotions);
            int startSquare = targetSquare - pushOffset;
            if (!IsPinned(startSquare))
            {
                GeneratePromotionMoves(moves, startSquare, targetSquare);
            }
        }


        while (capturePromotionsA != 0)
        {
            int targetSquare = BitboardUtils.PopLSB(ref capturePromotionsA);
            int startSquare = targetSquare - pushDir * 7;

            if (!IsPinned(startSquare) || PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare] == PreComputedMoveGenData.AlignMask[targetSquare, friendlyKingSquare])
            {
                GeneratePromotionMoves(moves, startSquare, targetSquare);
            }
        }

        while (capturePromotionsB != 0)
        {
            int targetSquare = BitboardUtils.PopLSB(ref capturePromotionsB);
            int startSquare = targetSquare - pushDir * 9;

            if (!IsPinned(startSquare) || PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare] == PreComputedMoveGenData.AlignMask[targetSquare, friendlyKingSquare])
            {
                GeneratePromotionMoves(moves, startSquare, targetSquare);
            }
        }

        // En passant
        if (board.EnpassantFile != 8)
        {
            int epFileIndex = board.EnpassantFile;
            int epRankIndex = turn ? 5 : 2;
            int targetSquare = epRankIndex * 8 + epFileIndex;
            int capturedPawnSquare = targetSquare - pushOffset;

            if (checkRayBitmask.Contains(capturedPawnSquare))
            {
                ulong pawnsThatCanCaptureEp = pawns & (turn ? PreComputedMoveGenData.blackPawnAttackMap[targetSquare] : PreComputedMoveGenData.whitePawnAttackMap[targetSquare]);

                while (pawnsThatCanCaptureEp != 0)
                {
                    int startSquare = BitboardUtils.PopLSB(ref pawnsThatCanCaptureEp);
                    if (!IsPinned(startSquare) || PreComputedMoveGenData.AlignMask[startSquare, friendlyKingSquare] == PreComputedMoveGenData.AlignMask[targetSquare, friendlyKingSquare])
                    {
                        if (!InCheckAfterEnPassant(startSquare, targetSquare, capturedPawnSquare))
                        {
                            moves[currMoveIndex++] = new Move(startSquare, targetSquare, MoveFlag.EnpassantCapture);
                        }
                    }
                }
            }
        }
    }

    void GeneratePromotionMoves(Span<Move> moves, int startSquare, int targetSquare)
    {
        // Generate promotion moves
        // Promote to: Queen, Rook, Knight, Bishop
        moves[currMoveIndex++] = new Move(startSquare, targetSquare, MoveFlag.PromoteToQueen);

        // Generate only Queen Promotion in QSearch
        if (genQuietMoves)
        {
            moves[currMoveIndex++] = new Move(startSquare, targetSquare, MoveFlag.PromoteToRook);
            moves[currMoveIndex++] = new Move(startSquare, targetSquare, MoveFlag.PromoteToKnight);
            moves[currMoveIndex++] = new Move(startSquare, targetSquare, MoveFlag.PromoteToBishop);
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
        if (board.PieceSquares[enemyQueenIndex].Count == 0)
        {
            startDirIndex = (board.PieceSquares[enemyRookIndex].Count > 0) ? 0 : 4;
            endDirIndex = (board.PieceSquares[enemyBishopIndex].Count > 0) ? 8 : 4;
        }

        // Check and Pin
        for (int dir = startDirIndex; dir < endDirIndex; dir++)
        {
            bool isDiagonal = dir > 3;
            ulong sliders = isDiagonal ? enemyDiagonalSliders : enemyStraightSliders;

            // No enemy piece that can attack in this direction
            if ((PreComputedMoveGenData.DirRayMask[dir, friendlyKingSquare] & sliders) == 0)
            {
                continue;
            }

            int n = PreComputedMoveGenData.NumSquaresToEdge[friendlyKingSquare, dir];
            int directionOffset = directionOffsets[dir];
            bool isFriendlyPieceAlongRay = false;
            ulong rayMask = 0;

            for (int i = 0; i < n; i++) {
                int squareIndex = friendlyKingSquare + directionOffset * (i + 1);
                rayMask |= (ulong) 1 << squareIndex;
                int piece = position[squareIndex];

                // This square contains a friendly piece
                if (PieceUtils.IsColor(piece, friendlyColor))
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
                else if (PieceUtils.IsColor(piece, enemyColor))
                {
                    // Check if piece is in bitmask of pieces able to move in current direction
                    if (isDiagonal && PieceUtils.IsDiagonalPiece(piece) || !isDiagonal && PieceUtils.IsStraightPiece(piece))
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
        PieceList opponentKnights = board.PieceSquares[PieceIndex.MakeKnight(!turn)];
        
        bool isKnightCheck = false;

        for (int knightIndex = 0; knightIndex < opponentKnights.Count; knightIndex++)
        {
            int startSquare = opponentKnights[knightIndex];
            enemyKnightAttackMap |= PreComputedMoveGenData.KnightMap[startSquare];

            if (!isKnightCheck && enemyKnightAttackMap.Contains(friendlyKingSquare))
            {
                isKnightCheck = true;
                inDoubleCheck = inCheck; // if already in check, then this is double check
                inCheck = true;
                checkRayBitmask |= (ulong) 1 << startSquare;
            }
        }

        // Pawn attacks
        PieceList opponentPawns = board.PieceSquares[PieceIndex.MakePawn(!turn)];
        
        bool isPawnCheck = false;

        for (int pawnIndex = 0; pawnIndex < opponentPawns.Count; pawnIndex++) {
            int pawnSquare = opponentPawns[pawnIndex];
            ulong pawnAttacks = turn ? PreComputedMoveGenData.blackPawnAttackMap[pawnSquare] : PreComputedMoveGenData.whitePawnAttackMap[pawnSquare];
            enemyPawnAttackMap |= pawnAttacks;

            if (!isPawnCheck && pawnAttacks.Contains(friendlyKingSquare)) {
                isPawnCheck = true;
                inDoubleCheck = inCheck; // If already in check, then this is double check
                inCheck = true;
                checkRayBitmask |= (ulong) 1 << pawnSquare;
            }
        }

        int enemyKingSquare = board.PieceSquares[PieceIndex.MakeKing(!turn)][0];

        enemyAttackMapNoPawns = enemySlidingAttackMap | enemyKnightAttackMap | PreComputedMoveGenData.KingMap[enemyKingSquare];
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
            int startSquare = BitboardUtils.PopLSB(ref pieces);
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
