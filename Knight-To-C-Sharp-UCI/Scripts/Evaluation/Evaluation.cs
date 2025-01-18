using static EvaluationConstants;

public class Evaluation
{
    Searcher engine;
    Board board;
    // Calculation Variables
    MaterialInfo whiteMaterial;
    MaterialInfo blackMaterial;

    EvaluationData whiteEval;
    EvaluationData blackEval;

    EvaluationBitboards bitboards;

    public Evaluation(Searcher _engine)
    {
        engine = _engine;
        board = engine.GetBoard();

        whiteEval = new EvaluationData();
        blackEval = new EvaluationData();
    }

    public int Evaluate(bool verbose = false)
    {
        int sign = board.Turn ? 1 : -1;

        whiteMaterial = MaterialInfo.GetMaterialInfo(board, white: true);
        blackMaterial = MaterialInfo.GetMaterialInfo(board, white: false);

        whiteEval.Clear();
        blackEval.Clear();

        bitboards.Get(board);

        whiteEval.materialScore = whiteMaterial.materialValue;
        blackEval.materialScore = blackMaterial.materialValue;

        whiteEval.pieceSquareScore = PieceSquareTableScore(white: true, blackMaterial.endgameWeight);
        blackEval.pieceSquareScore = PieceSquareTableScore(white: false, whiteMaterial.endgameWeight);

        whiteEval.mopUpScore = CalculateMopUpScore(white: true);
        blackEval.mopUpScore = CalculateMopUpScore(white: false);

        whiteEval.openFileScore = CalculateOpenFileBonus(white: true);
        blackEval.openFileScore = CalculateOpenFileBonus(white: false);

        whiteEval.pawnScore = EvaluatePawns(white: true);
        blackEval.pawnScore = EvaluatePawns(white: false);

        whiteEval.kingSafety = KingSafety(white: true);
        blackEval.kingSafety = KingSafety(white: false);

        // Print Evaluation Data
        if (verbose)
        {
            Console.WriteLine("-----White Evaluation-----");
            whiteEval.Print();
            Console.WriteLine("");
            Console.WriteLine("-----Black Evaluation-----");
            blackEval.Print();
            Console.WriteLine("");
        }

        return (whiteEval.Sum() - blackEval.Sum()) * sign;
    }

    // Pawns
    int EvaluatePawns(bool white)
    {
        int eval = 0;
        ulong frinedlyPawns = white ? bitboards.whitePawns : bitboards.blackPawns;
        ulong enemyPawns = white ? bitboards.blackPawns : bitboards.whitePawns;

        int numIsolated = 0;

        PieceList pawns = board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];
        for (int i = 0; i < pawns.Count; i++)
        {
            int square = pawns[i];

            ulong passedPawnMask = (white ? Bits.WhitePassedPawnMask : Bits.BlackPassedPawnMask)[square];
            if ((passedPawnMask & enemyPawns) == 0)
            {
                int rank = square.Rank();
                eval += PassedPawnBonus[white ? 7 - rank : rank];
            }

            if ((frinedlyPawns & Bits.AdjacentFilesMask[square.File()]) == 0)
            {
                numIsolated++;
            }
        }

        eval -= IsolatedPawnPenaltyByCount[numIsolated];

        return eval;
    }

    // King Safety
    [Inline]
    int KingSafety(bool white)
    {
        return -(int) ((FrontPawnsPenalty(white: white) + KingOpenFilePenalty(white: white)) * GetKingSafetyWeight(white: white));
    }
    int FrontPawnsPenalty(bool white)
    {
        int r = 0;
        int kingSquare = (white ? whiteMaterial : blackMaterial).kingSquare;
        int rank = kingSquare.Rank();

        if ((white && rank > 1) || (!white && rank < 6))
        {
            // Too far away from safe square
            r += 3 * DirectKingFrontPawnPenalty;
        }
        else
        {
            int frontRank = rank + (white ? 1 : -1);
            int distantRank = rank + (white ? 2 : -2);

            ulong triple = Bits.TripleFileMask[kingSquare.File()];

            ulong frontMask = (Bits.RankMask[frontRank]) & triple;
            ulong distantMask = (Bits.RankMask[distantRank]) & triple;

            ulong pawns = board.BBSet[PieceIndex.MakePawn(white)];
            ulong pieces = board.BBSet[PieceIndex.MakeAll(white)] ^ pawns;

            ulong shieldPawns = pawns & frontMask;
            ulong shieldPieces = pieces & frontMask;
            ulong distantPawns = pawns & distantMask;
            ulong distantPieces = pieces & distantMask;

            int totalProtection = frontMask.Count() * DirectKingFrontPawnPenalty;
            int protection = 
            shieldPawns.Count() * DirectKingFrontPawnPenalty
             + shieldPieces.Count() * DirectKingFrontPiecePenalty
             + distantPawns.Count() * DistantKingFrontPawnPenalty
             + distantPieces.Count() * DistantKingFrontPiecePenalty;

            r += Math.Max(totalProtection - protection, 0);
        }

        return r;
    }
    int KingOpenFilePenalty(bool white)
    {
        int r = 0;

        int kingFile = (white ? whiteMaterial : blackMaterial).kingSquare.File();
        
        if (IsOpenFile(kingFile) || IsSemiOpenFile(kingFile))
        {
            r += KingOpenPenalty;
        }
        if (kingFile < 7)
        {
            if (IsOpenFile(kingFile + 1) || IsSemiOpenFile(kingFile + 1))
            {
                r += KingAdjacentOpenPenalty;
            }
        }
        if (kingFile > 0)
        {
            if (IsOpenFile(kingFile - 1) || IsSemiOpenFile(kingFile - 1))
            {
                r += KingAdjacentOpenPenalty;
            }
        }

        return r;
    }
    [Inline]
    double GetKingSafetyWeight(bool white)
    {
        double middleSquared = Math.Pow(1 - (!white ? whiteMaterial : blackMaterial).endgameWeight, 2);
        return middleSquared;
    }

    // Piece square table
    int PieceSquareTableScore(bool white, double endgameWeight = 0.5)
    {
        int value = 0;
        
        value += ReadPieceSquareTable(PieceSquareTable.Knight, board.PieceSquares[PieceIndex.MakeKnight(white)], white);
        value += ReadPieceSquareTable(PieceSquareTable.Bishop, board.PieceSquares[PieceIndex.MakeBishop(white)], white);
        value += ReadPieceSquareTable(PieceSquareTable.Rook, board.PieceSquares[PieceIndex.MakeRook(white)], white);
        value += ReadPieceSquareTable(PieceSquareTable.Queen, board.PieceSquares[PieceIndex.MakeQueen(white)], white);

        double reverseEndWeight = 1 - endgameWeight;

        value += (int) (ReadPieceSquareTable(PieceSquareTable.Pawn, board.PieceSquares[PieceIndex.MakePawn(white)], white) * reverseEndWeight);
        value += (int) (ReadPieceSquareTable(PieceSquareTable.PawnEnd, board.PieceSquares[PieceIndex.MakePawn(white)], white) * endgameWeight);
        value += (int) (ReadPieceSquareTable(PieceSquareTable.King, board.PieceSquares[PieceIndex.MakeKing(white)], white) * reverseEndWeight);
        value += (int) (ReadPieceSquareTable(PieceSquareTable.KingEnd, board.PieceSquares[PieceIndex.MakeKing(white)], white) * endgameWeight);

        return value;
    }
    [Inline]
    int ReadPieceSquareTable(int[] table, PieceList pieceList, bool white)
    {
        int value = 0;

        for (int i = 0; i < pieceList.Count; i++)
        {
            value += PieceSquareTable.Read(table, pieceList[i], white);
        }

        return value;
    }
    
    // Mop-Up
    int CalculateMopUpScore(bool white)
    {
        int myMaterial = (white? whiteMaterial : blackMaterial).materialValue;
        int enemyMaterial = (white ? blackMaterial : whiteMaterial).materialValue;

        double enemyEndgameWeight = (white ? blackMaterial : whiteMaterial).endgameWeight;

        if (myMaterial >= enemyMaterial + PawnValue * 2 && enemyEndgameWeight > 0d)
        {
            int mopUpScore = 0;
            int friendlyKingSquare = (white ? whiteMaterial : blackMaterial).kingSquare;
            int enemyKingSquare = (white ? blackMaterial : whiteMaterial).kingSquare;

            // Friendly king closer to the enemy king
            mopUpScore += (14 - PreComputedEvalData.DistanceFromSquare[friendlyKingSquare, enemyKingSquare]) * 8;
            
            // Enemy king in the corner
            mopUpScore += PreComputedEvalData.DistanceFromCenter[enemyKingSquare] * 6;

            // Force the enemy king to the corner of the friendly bishop's color
            ulong friendlyBishop = white ? bitboards.whiteBishops : bitboards.blackBishops;
            if (friendlyBishop != 0) {
                // Has a light squared bishop
                if ((friendlyBishop & Bits.LightSquares) != 0) {
                    mopUpScore += (7 - Math.Min(
                        PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.a8], 
                        PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.h1]
                    )) * 8;
                }
                // Has a dark squared bishop
                if ((friendlyBishop & Bits.DarkSquares) != 0) {
                    mopUpScore += (7 - Math.Min(
                        PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.h8], 
                        PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.a1]
                    )) * 8;
                }
            }

            return (int) (mopUpScore * enemyEndgameWeight);
        }

        return 0;
    }

    // Open Files
    int CalculateOpenFileBonus(bool white)
    {
        int r = 0;

        PieceList rooks = board.PieceSquares[white ? PieceIndex.WhiteRook : PieceIndex.BlackRook];

        for (int i = 0; i < rooks.Count; i++)
        {
            int square = rooks[i];
            if (IsOpenFile(square))
            {
                r += OpenFileBonus;
            }
            else if (IsOpenFileFromSide(square, white: white))
            {
                r += SemiOpenFileBonus;
            }
        }

        return r;
    }

    [Inline]
    bool IsOpenFile(int square)
    {
        // (File & All Pawns) == 0
        return ((Bits.FileA << square.File()) & (board.BBSet[PieceIndex.WhitePawn] | board.BBSet[PieceIndex.BlackPawn])) == 0;
    }
    [Inline]
    bool IsOpenFileFromSide(int square, bool white)
    {
        return ((Bits.FileA << square.File()) & board.BBSet[PieceIndex.MakePawn(white)]) == 0;
    }
    [Inline]
    bool IsSemiOpenFile(int square)
    {
        return IsOpenFileFromSide(square, white: true) || IsOpenFileFromSide(square, white: false);
    }

    // Move Ordering
    [Inline]
    public static int GetAbsPieceValue(int piece)
    {
        int pieceType = piece.Type();

        if (pieceType == PieceUtils.Queen)
        {
            return QueenValue;
        }
        else if (pieceType == PieceUtils.Rook)
        {
            return RookValue;
        }
        else if (pieceType == PieceUtils.Knight)
        {
            return KnightValue;
        }
        else if (pieceType == PieceUtils.Bishop)
        {
            return BishopValue;
        }
        else if (pieceType == PieceUtils.Pawn)
        {
            return PawnValue;
        }

        // FailSafe
        return 0;
    }

    // Checkmate Detection
    [Inline]
    public bool IsMateScore(int score)
    {
        if (Math.Abs(score) >= CheckmateEval - Configuration.MaxDepth)
        {
            return true;
        }

        return false;
    }

    [Inline]
    public int MateInPly(int mateScore)
    {
        return CheckmateEval - Math.Abs(mateScore);
    }

    // Helper Structures
    struct MaterialInfo
    {
        // Endgame weight constants
        const int QueenEndgameWeight = 115;
        const int RookEndgameWeight = 75;
        const int BishopEndgameWeight = 50;
        const int KnightEndgameWeight = 45;
        const int TotalEndgameWeight = QueenEndgameWeight + 2 * RookEndgameWeight + 2 * BishopEndgameWeight + 
        2 * KnightEndgameWeight;

        public readonly int numQueens, numRooks, numBishops, numKnights, numPawns;
        public readonly int materialValue;
        public readonly int kingSquare;

        public readonly double endgameWeight;

        public MaterialInfo(int numQueens, int numRooks, int numBishops, int numKnights, int numPawns, int kingSquare)
        {
            this.numQueens = numQueens;
            this.numRooks = numRooks;
            this.numBishops = numBishops;
            this.numKnights = numKnights;
            this.numPawns = numPawns;

            materialValue = QueenValue * numQueens + RookValue * numRooks + BishopValue * numBishops + KnightValue * numKnights + PawnValue * numPawns;

            this.kingSquare = kingSquare;

            int endSum = QueenEndgameWeight * numQueens + RookEndgameWeight * numRooks + BishopEndgameWeight * numBishops + KnightEndgameWeight * numKnights;
            endgameWeight = 1 - Math.Min(1, (double) endSum / TotalEndgameWeight);
        }

        public static MaterialInfo GetMaterialInfo(Board board, bool white)
        {
            int numQueens = board.PieceSquares[PieceIndex.MakeQueen(white)].Count;
            int numRooks = board.PieceSquares[PieceIndex.MakeRook(white)].Count;
            int numBishops = board.PieceSquares[PieceIndex.MakeBishop(white)].Count;
            int numKnights = board.PieceSquares[PieceIndex.MakeKnight(white)].Count;
            int numPawns = board.PieceSquares[PieceIndex.MakePawn(white)].Count;

            int kingSquare = board.PieceSquares[PieceIndex.MakeKing(white)][0];

            return new MaterialInfo(numQueens, numRooks, numBishops, numKnights, numPawns, kingSquare);
        }
    }
    struct EvaluationBitboards
    {
        // Kings
        public ulong whiteKing;
        public ulong blackKing;

        // Pieces
        public ulong whiteQueens;
        public ulong whiteRooks;
        public ulong whiteBishops;
        public ulong whiteKnights;
        public ulong whitePawns;
        public ulong blackQueens;
        public ulong blackRooks;
        public ulong blackBishops;
        public ulong blackKnights;
        public ulong blackPawns;

        // Pawn Space & Pieces behind the pawns
        public ulong whitePawnBehind;
        public ulong blackPawnBehind;

        public void Get(Board board)
        {
            ref BitboardSet bitboards = ref board.BBSet;
            
            whiteKing = bitboards[PieceIndex.WhiteKing];
            blackKing = bitboards[PieceIndex.BlackKing];

            whiteQueens = bitboards[PieceIndex.WhiteQueen];
            whiteRooks = bitboards[PieceIndex.WhiteRook];
            whiteBishops = bitboards[PieceIndex.WhiteBishop];
            whiteKnights = bitboards[PieceIndex.WhiteKnight];
            whitePawns = bitboards[PieceIndex.WhitePawn];

            blackQueens = bitboards[PieceIndex.BlackQueen];
            blackRooks = bitboards[PieceIndex.BlackRook];
            blackBishops = bitboards[PieceIndex.BlackBishop];
            blackKnights = bitboards[PieceIndex.BlackKnight];
            blackPawns = bitboards[PieceIndex.BlackPawn];

            ulong whitePawnClone = whitePawns;
            while (whitePawnClone != 0)
            {
                int square = BitboardUtils.PopLSB(ref whitePawnClone);
                int file = square.File();
                int rank = square.Rank();

                ulong fileMask = Bits.FileMask[file];
                whitePawnBehind |= fileMask & Bits.BlackForwardMask[rank];
            }
            whitePawnBehind |= Bits.Rank1;

            ulong blackPawnClone = blackPawns;
            while (blackPawnClone != 0)
            {
                int square = BitboardUtils.PopLSB(ref blackPawnClone);
                int file = square.File();
                int rank = square.Rank();

                ulong fileMask = Bits.FileMask[file];
                blackPawnBehind |= fileMask & Bits.WhiteForwardMask[rank];
            }
            blackPawnBehind |= Bits.RankMask[7];
        }
    }
    struct EvaluationData
    {
        public int materialScore;
        public int pieceSquareScore;
        public int mopUpScore;
        public int openFileScore;
        public int pawnScore;
        public int kingSafety;

        [Inline]
        public int Sum()
        {
            return materialScore + pieceSquareScore + mopUpScore + openFileScore + pawnScore + kingSafety;
        }

        [Inline]
        public void Clear()
        {
            materialScore = pieceSquareScore = mopUpScore = openFileScore = pawnScore = kingSafety = 0;
        }

        [Inline]
        public void Print()
        {
            Console.WriteLine($"Material: {materialScore}\nPSQT: {pieceSquareScore}\nMopUp: {mopUpScore}\nPawns: {pawnScore}\nKingSafety: {kingSafety}");
        }
    }

    [Inline]
    public static double GetEndgameWeight(Board board, bool color) {
        return MaterialInfo.GetMaterialInfo(board, color).endgameWeight;
    }
}