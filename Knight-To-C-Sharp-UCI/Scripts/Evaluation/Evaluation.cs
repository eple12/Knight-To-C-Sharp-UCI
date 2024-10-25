

public class Evaluation
{
    Searcher engine;
    Board board;

    // Checkmate evaluation detection
    public static int CheckmateEval = 99999;
    int maxDepth;

    // Pawns
    static readonly int[] PassedPawnBonus = { 0, 120, 80, 60, 40, 30, 15, 15 };
    static readonly int[] IsolatedPawnPenaltyByCount = { 0, 10, 25, 50, 75, 75, 75, 75, 75 };

    // King Safety
    const int DirectKingFrontPawnPenalty = 50;
    const int DistantKingFrontPawnPenalty = 30;
    const int DirectKingFrontPiecePenalty = 30;
    const int DistantKingFrontPiecePenalty = 20;
    // const double SecondRankKingFrontPieceMultiplier = 0.5d;

    const int KingOpenPenalty = 75;
    const int KingAdjacentOpenPenalty = 25;

    // King Safety Weight
    const int KingSafetyQueenWeight = 150;
    const int KingSafetyRookWeight = 100;
    const int KingSafetyMinorWeight = 50;
    const int KingSafetyMaxQueens = 1;
    const int KingSafetyMaxRooks = 2;
    const int KingSafetyMaxMinors = 3;
    const int KingSafetyTotalWeight = KingSafetyMaxQueens * KingSafetyQueenWeight + KingSafetyMaxRooks * KingSafetyRookWeight + KingSafetyMaxMinors * KingSafetyMinorWeight;

    // const int FrontQueenKingSafetyWeight = 50;
    // const int FrontRookKingSafetyWeight = 35;
    // const int TotalKingSafetyWeight = 1 * FrontQueenKingSafetyWeight + 2 * FrontRookKingSafetyWeight;

    // Open File
    const int OpenFileBonus = 20;
    const int SemiOpenFileBonus = 20;
    const ulong FileMask = 0x0101010101010101; // Represents A file

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

        maxDepth = engine.GetSettings().unlimitedMaxDepth;

        whiteEval = new EvaluationData();
        blackEval = new EvaluationData();
    }

    public int Evaluate()
    {
        // int eval = 0;
        int sign = board.Turn ? 1 : -1;

        whiteMaterial = MaterialInfo.GetMaterialInfo(board, white: true);
        blackMaterial = MaterialInfo.GetMaterialInfo(board, white: false);

        whiteEval.Clear();
        blackEval.Clear();

        bitboards.Get(board);

        // double endgameWeight = GetEndgameWeight();
        // double middlegameWeight = GetMiddlegameWeight(endgameWeight);
        // double reverseEndWeight = 1 - endgameWeight;
        // Console.WriteLine("endWeight: " + endgameWeight + " midWeight: " + middlegameWeight);

        
        // Console.WriteLine("material: " + materialCount);
        // eval += whiteMaterial.materialValue - blackMaterial.materialValue;
        whiteEval.materialScore = whiteMaterial.materialValue;
        blackEval.materialScore = blackMaterial.materialValue;

        // int pieceSquareBonus = PieceSquareTable();
        // Console.WriteLine("pieceSquareBonus: " + pieceSquareBonus);
        // eval += pieceSquareBonus;
        whiteEval.pieceSquareScore = PieceSquareTableScore(white: true, blackMaterial.endgameWeight);
        blackEval.pieceSquareScore = PieceSquareTableScore(white: false, whiteMaterial.endgameWeight);

        whiteEval.mopUpScore = CalculateMopUpScore(white: true);
        blackEval.mopUpScore = CalculateMopUpScore(white: false);

        // int openFileBonus = CalculateOpenFileBonus();
        // // Console.WriteLine("openFileBonus: " + openFileBonus);
        // eval += openFileBonus;
        whiteEval.openFileScore = CalculateOpenFileBonus(white: true);
        blackEval.openFileScore = CalculateOpenFileBonus(white: false);

        // int pawnBonus = PawnBonus();
        // eval += pawnBonus;
        whiteEval.pawnScore = EvaluatePawns(white: true);
        blackEval.pawnScore = EvaluatePawns(white: false);

        // int kingSafety = (int) (KingSafety() * reverseEndWeight);
        // // // Console.WriteLine("eval kingSafety: " + kingSafety);
        // eval += kingSafety;
        whiteEval.kingSafety = KingSafety(white: true);
        blackEval.kingSafety = KingSafety(white: false);

        // Console.WriteLine($"{whiteEval.kingSafety} {blackEval.kingSafety}");

        // Console.WriteLine(whiteEval.pieceSquareScore);
        // Console.WriteLine(blackEval.pieceSquareScore);

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
        for (int i = 0; i < pawns.count; i++)
        {
            int square = pawns.squares[i];

            ulong passedPawnMask = (white ? Bits.WhitePassedPawnMask : Bits.BlackPassedPawnMask)[square];
            if ((passedPawnMask & enemyPawns) == 0)
            {
                int rank = square / 8;
                eval += PassedPawnBonus[white ? 7 - rank : rank];
            }

            if ((frinedlyPawns & Bits.AdjacentFilesMask[square % 8]) == 0)
            {
                numIsolated++;
            }
        }

        eval -= IsolatedPawnPenaltyByCount[numIsolated];

        return eval;
    }

    // King Safety
    // TODO: FIX Over-reacting
    int KingSafety(bool white)
    {
        int r = 0;
        double kingSafetyWeight = GetKingSafetyWeight(white: white);

        // Console.WriteLine($"kingSafetyWeight {kingSafetyWeight} white {white}");
        // double blackKingSafetyWeight = GetKingSafetyWeight(white: false);

        // Console.WriteLine("white: " + whiteKingSafetyWeight + " black: " + blackKingSafetyWeight);

        r -= (int) ((FrontPawnsPenalty(white: white) + KingOpenFilePenalty(white: white)) * kingSafetyWeight);
        // r += CalculateKingOpenFilePenalty(whiteKingSafetyWeight, blackKingSafetyWeight);
        
        // Console.WriteLine("total kingSafety: " + r);

        // Console.WriteLine(r);

        return r;
    }
    int FrontPawnsPenalty(bool white)
    {
        int r = 0;
        int kingSquare = (white ? whiteMaterial : blackMaterial).kingSquare;
        int rank = kingSquare / 8;

        if ((white && rank > 1) || (!white && rank < 6))
        {
            // Too far away from safe square
            r += 3 * DirectKingFrontPawnPenalty;
        }
        else
        {
            int frontRank = rank + (white ? 1 : -1);
            int distantRank = rank + (white ? 2 : -2);

            ulong triple = Bits.TripleFileMask[kingSquare % 8];

            ulong frontMask = (Bits.RankMask[frontRank]) & triple;
            ulong distantMask = (Bits.RankMask[distantRank]) & triple;

            ulong pawns = board.BitboardSet.Bitboards[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];
            ulong pieces = board.BitboardSet.Bitboards[white ? PieceIndex.WhiteAll : PieceIndex.BlackAll] ^ pawns;

            ulong shieldPawns = pawns & frontMask;
            ulong shieldPieces = pieces & frontMask;
            ulong distantPawns = pawns & distantMask;
            ulong distantPieces = pieces & distantMask;

            // Bitboard.Print(frontMask);
            // Bitboard.Print(distantMask);

            int totalProtection = Bitboard.Count(frontMask) * DirectKingFrontPawnPenalty;
            int protection = 
            Bitboard.Count(shieldPawns) * DirectKingFrontPawnPenalty
             + Bitboard.Count(shieldPieces) * DirectKingFrontPiecePenalty
             + Bitboard.Count(distantPawns) * DistantKingFrontPawnPenalty
             + Bitboard.Count(distantPieces) * DistantKingFrontPiecePenalty;
            
            // Console.WriteLine($"totalProt {totalProtection}");
            // Console.WriteLine($"prot {protection}");

            r += Math.Max(totalProtection - protection, 0);
        }
        
        // Console.WriteLine($"{white} pawnPenalty {r}");

        return r;
    }
    int KingOpenFilePenalty(bool white)
    {
        int r = 0;

        int kingFile = (white ? whiteMaterial : blackMaterial).kingSquare % 8;
        
        if (IsOpenFile(kingFile) || IsSemiOpenFile(kingFile))
        // if (IsSemiFromOrOpenFile(kingFile, white))
        {
            r += KingOpenPenalty;
        }
        if (kingFile < 7)
        {
            if (IsOpenFile(kingFile + 1) || IsSemiOpenFile(kingFile + 1))
            // if (IsSemiFromOrOpenFile(kingFile + 1, white))
            {
                r += KingAdjacentOpenPenalty;
            }
        }
        if (kingFile > 0)
        {
            if (IsOpenFile(kingFile - 1) || IsSemiOpenFile(kingFile - 1))
            // if (IsSemiFromOrOpenFile(kingFile - 1, white))
            {
                r += KingAdjacentOpenPenalty;
            }
        }

        return r;
    }
    // int CalculateKingOpenFilePenalty(double whiteWeight = 1, double blackWeight = 1)
    // {
    //     int r = 0;

    //     int white = 0;

    //     // White King: Open File Or Semi-Open File for Black
    //     if (IsSemiOpenFile(whiteKingSquare))
    //     {
    //         white -= KingOpenFilePenalty;
    //     }
    //     if (whiteKingSquare % 8 > 0)
    //     {
    //         if (IsSemiOpenFile(whiteKingSquare - 1))
    //         {
    //             white -= KingSideOpenFilePenalty;
    //         }
    //     }
    //     if (whiteKingSquare % 8 < 7)
    //     {
    //         if (IsSemiOpenFile(whiteKingSquare + 1))
    //         {
    //             white -= KingSideOpenFilePenalty;
    //         }
    //     }
        
    //     int black = 0;

    //     // Black King: Open File Or Semi-Open File for White
    //     if (IsSemiOpenFile(blackKingSquare))
    //     {
    //         black += KingOpenFilePenalty;
    //     }
    //     if (blackKingSquare % 8 > 0)
    //     {
    //         if (IsSemiOpenFile(blackKingSquare - 1))
    //         {
    //             black += KingSideOpenFilePenalty;
    //         }
    //     }
    //     if (blackKingSquare % 8 < 7)
    //     {
    //         if (IsSemiOpenFile(blackKingSquare + 1))
    //         {
    //             black += KingSideOpenFilePenalty;
    //         }
    //     }

    //     r += (int) (white * whiteWeight);
    //     r += (int) (black * blackWeight);

    //     return r;
    // }
    // int FrontPawnsPenalty(double whiteWeight = 1, double blackWeight = 1)
    // {
    //     int r = 0;

    //     r += (int) (CalculateKingFrontPiecePenalty(white: true) * whiteWeight);
    //     r -= (int) (CalculateKingFrontPiecePenalty(white: false) * blackWeight);

    //     return r;
    // }
    // int CalculateKingFrontPiecePenalty(bool white)
    // {
    //     int kingRank = white ? whiteKingSquare / 8 : blackKingSquare / 8;

    //     if ((white && kingRank > 1) || (!white && kingRank < 6))
    //     {
    //         return -3 * DirectKingFrontPawnPenalty;
    //     }

    //     int r = 0;

    //     ulong directMask = GetFrontMask(white: white, isDirect: true);
    //     ulong distantMask = GetFrontMask(white: white, isDirect: false);

    //     r += -Bitboard.Count(directMask) * DirectKingFrontPawnPenalty;

    //     int pawnIndex = white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn;
    //     int allIndex = white ? PieceIndex.WhiteAll : PieceIndex.BlackAll;

    //     ulong pawnBitboard = board.BitboardSet.Bitboards[pawnIndex];
    //     ulong allBitboardNoPawns = board.BitboardSet.Bitboards[allIndex] ^ pawnBitboard;

    //     r += Bitboard.Count(directMask & pawnBitboard) * DirectKingFrontPawnPenalty;
    //     r += Bitboard.Count(distantMask & pawnBitboard) * DistantKingFrontPawnPenalty;

    //     r += Bitboard.Count(directMask & allBitboardNoPawns) * DirectKingFrontPiecePenalty;
    //     r += Bitboard.Count(distantMask & allBitboardNoPawns) * DistantKingFrontPiecePenalty;

    //     // Second Rank Penalty
    //     if ((white && kingRank == 1) || (!white && kingRank == 7))
    //     {
    //         r = (int) (r * SecondRankKingFrontPieceMultiplier);
    //     }

    //     return r;
    // }
    // ulong GetFrontMask(bool white, bool isDirect)
    // {
    //     ulong mask = 0;

    //     int kingSquare = white ? whiteKingSquare : blackKingSquare;
    //     int rank = kingSquare / 8;
    //     int file = kingSquare % 8;
        
    //     int rankOffset = isDirect ? 1 : 2;
    //     int offset = 8 * rankOffset;

    //     if (white ? rank + rankOffset <= 7 : rank - rankOffset >= 0)
    //     {
    //         int upOffset = white ? offset : - offset;
            
    //         mask |= (ulong) 1 << (kingSquare + upOffset);

    //         if (file > 0)
    //         {
    //             mask |= (ulong) 1 << (kingSquare + upOffset - 1);
    //         }
    //         if (file < 7)
    //         {
    //             mask |= (ulong) 1 << (kingSquare + upOffset + 1);
    //         }
    //     }

    //     return mask;
    // }

    double GetKingSafetyWeight(bool white)
    {
        double middleSquared = Math.Pow(1 - (!white ? whiteMaterial : blackMaterial).endgameWeight, 2);
        // Console.WriteLine($"middleSq {middleSquared}");
        // Console.WriteLine(whiteMaterial.endgameWeight);
        // Console.WriteLine(blackMaterial.endgameWeight);

        // int kingSquare = (white ? whiteMaterial : blackMaterial).kingSquare;
        // int kingFile = kingSquare % 8;

        // ulong triple = Bits.TripleFileMask[kingFile];
        // ulong enemyQueens = board.BitboardSet.Bitboards[white ? PieceIndex.BlackQueen : PieceIndex.WhiteQueen];
        // ulong enemyRooks = board.BitboardSet.Bitboards[white ? PieceIndex.BlackRook : PieceIndex.WhiteRook];
        // ulong enemyAll = board.BitboardSet.Bitboards[white ? PieceIndex.BlackAll : PieceIndex.WhiteAll];
        // ulong enemyKing = board.BitboardSet.Bitboards[white ? PieceIndex.BlackKing : PieceIndex.WhiteKing];
        // ulong enemyMinors = enemyAll ^ enemyQueens ^ enemyRooks ^ enemyKing;

        // // Bitboard.Print(triple & enemyQueens);
        // // Bitboard.Print(~(white ? bitboards.blackPawnBehind : bitboards.whitePawnBehind));
        // // Console.WriteLine(Bitboard.Count(triple & enemyQueens & ~(white ? bitboards.blackPawnBehind : bitboards.whitePawnBehind)));

        // int queens = Math.Min(Bitboard.Count(triple & enemyQueens & ~(white ? bitboards.blackPawnBehind : bitboards.whitePawnBehind)), KingSafetyMaxQueens);
        // int rooks = Math.Min(Bitboard.Count(triple & enemyRooks & ~(white ? bitboards.blackPawnBehind : bitboards.whitePawnBehind)), KingSafetyMaxRooks);
        // int minors = Math.Min(Bitboard.Count(triple & enemyMinors & ~(white ? bitboards.blackPawnBehind : bitboards.whitePawnBehind)), KingSafetyMaxMinors);

        // // Console.WriteLine($"{queens} {rooks} {minors}");

        // double attacked = (queens * KingSafetyQueenWeight + rooks * KingSafetyRookWeight + minors * KingSafetyMinorWeight) / (double) KingSafetyTotalWeight;

        // Console.WriteLine($"attack {attacked}");

        // return (middleSquared + 3 * attacked) / 4.0d;
        // double 
        return middleSquared;
    }

    // Piece square table
    int PieceSquareTableScore(bool white, double endgameWeight = 0.5)
    {
        int value = 0;
        
        value += ReadPieceSquareTable(PieceSquareTable.Knight, board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn], white);
        value += ReadPieceSquareTable(PieceSquareTable.Bishop, board.PieceSquares[white ? PieceIndex.WhiteBishop : PieceIndex.BlackBishop], white);
        value += ReadPieceSquareTable(PieceSquareTable.Rook, board.PieceSquares[white ? PieceIndex.WhiteRook : PieceIndex.BlackRook], white);
        value += ReadPieceSquareTable(PieceSquareTable.Queen, board.PieceSquares[white ? PieceIndex.WhiteQueen : PieceIndex.BlackQueen], white);

        double reverseEndWeight = 1 - endgameWeight;

        value += (int) (ReadPieceSquareTable(PieceSquareTable.Pawn, board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn], white) * reverseEndWeight);
        value += (int) (ReadPieceSquareTable(PieceSquareTable.PawnEnd, board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn], white) * endgameWeight);
        // value += ReadPieceSquareTable(PieceSquareTable.Pawn, board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn], white);

        value += (int) (ReadPieceSquareTable(PieceSquareTable.King, board.PieceSquares[white ? PieceIndex.WhiteKing : PieceIndex.BlackKing], white) * reverseEndWeight);
        value += (int) (ReadPieceSquareTable(PieceSquareTable.KingEnd, board.PieceSquares[white ? PieceIndex.WhiteKing : PieceIndex.BlackKing], white) * endgameWeight);
        // value += ReadPieceSquareTable(PieceSquareTable.King, board.PieceSquares[white ? PieceIndex.WhiteKing : PieceIndex.BlackKing], white);

        return value;
    }
    int ReadPieceSquareTable(int[] table, PieceList pieceList, bool white)
    {
        int value = 0;

        for (int i = 0; i < pieceList.count; i++)
        {
            value += PieceSquareTable.Read(table, pieceList.squares[i], white);
        }

        return value;
    }
    
    // Mop-Up
    int CalculateMopUpScore(bool white)
    {
        int myMaterial = (white? whiteMaterial : blackMaterial).materialValue;
        int enemyMaterial = (white ? blackMaterial : whiteMaterial).materialValue;

        double enemyEndgameWeight = (white ? blackMaterial : whiteMaterial).endgameWeight;

        if (myMaterial >= enemyMaterial + MaterialInfo.PawnValue * 2 && enemyEndgameWeight > 0d)
        {
            int mopUpScore = 0;
            int friendlyKingSquare = (white ? whiteMaterial : blackMaterial).kingSquare;
            int enemyKingSquare = (white ? blackMaterial : whiteMaterial).kingSquare;

            mopUpScore += (14 - PreComputedEvalData.DistanceFromSquare[friendlyKingSquare, enemyKingSquare]) * 4;

            mopUpScore += PreComputedEvalData.DistanceFromCenter[enemyKingSquare] * 10;

            return (int) (mopUpScore * enemyEndgameWeight);
        }

        return 0;
    }


    // Open Files
    int CalculateOpenFileBonus(bool white)
    {
        int r = 0;

        PieceList rooks = board.PieceSquares[white ? PieceIndex.WhiteRook : PieceIndex.BlackRook];

        for (int i = 0; i < rooks.count; i++)
        {
            int square = rooks.squares[i];
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
    bool IsOpenFile(int square)
    {
        int file = square % 8;

        ulong pawnsMask = board.BitboardSet.Bitboards[PieceIndex.WhitePawn] | board.BitboardSet.Bitboards[PieceIndex.BlackPawn];
        return ((FileMask << file) & pawnsMask) == 0;
    }
    // bool IsSemiOpenFile(int square)
    // {
    //     int file = square % 8;

    //     ulong pawnsMask = board.BitboardSet.Bitboards[PieceIndex.WhitePawn] | board.BitboardSet.Bitboards[PieceIndex.BlackPawn];
    //     ulong resultMask = (FileMask << file) & pawnsMask;
    //     return Bitboard.Count(resultMask) == 1;
    // }
    bool IsOpenFileFromSide(int square, bool white)
    {
        int file = square % 8;

        ulong pawnsMask = board.BitboardSet.Bitboards[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];
        ulong resultMask = (FileMask << file) & pawnsMask;
        return resultMask == 0;
    }
    
    bool IsSemiFromOrOpenFile(int square, bool white)
    {
        return IsOpenFileFromSide(square, white) || IsOpenFile(square);
    }
    bool IsSemiOpenFile(int square)
    {
        return IsOpenFileFromSide(square, white: true) || IsOpenFileFromSide(square, white: false);
    }

    // Move Ordering
    public static int GetAbsPieceValue(int piece)
    {
        int pieceType = Piece.GetType(piece);

        if (pieceType == Piece.Queen)
        {
            return MaterialInfo.QueenValue;
        }
        else if (pieceType == Piece.Rook)
        {
            return MaterialInfo.RookValue;
        }
        else if (pieceType == Piece.Knight)
        {
            return MaterialInfo.KnightValue;
        }
        else if (pieceType == Piece.Bishop)
        {
            return MaterialInfo.BishopValue;
        }
        else if (pieceType == Piece.Pawn)
        {
            return MaterialInfo.PawnValue;
        }

        // FailSafe
        return 0;
    }

    // // Endgame Weight
    // public double GetEndgameWeight()
    // {
    //     int numQueens = board.PieceSquares[PieceIndex.WhiteQueen].count + 
    //                     board.PieceSquares[PieceIndex.BlackQueen].count;
    //     int numRooks = board.PieceSquares[PieceIndex.WhiteRook].count + 
    //                     board.PieceSquares[PieceIndex.BlackRook].count;
    //     int numBishops = board.PieceSquares[PieceIndex.WhiteBishop].count + 
    //                     board.PieceSquares[PieceIndex.BlackBishop].count;
    //     int numKnights = board.PieceSquares[PieceIndex.WhiteKnight].count + 
    //                     board.PieceSquares[PieceIndex.BlackKnight].count;
    //     int numPawns = board.PieceSquares[PieceIndex.WhitePawn].count + 
    //                     board.PieceSquares[PieceIndex.BlackPawn].count;

    //     int numPiecesNoPawns = numQueens + numRooks + numBishops + numKnights;

    //     int totalWeight = numQueens * QueenEndgameWeight + numRooks * RookEndgameWeight + numBishops * BishopEndgameWeight + numKnights * KnightEndgameWeight + numPawns * PawnEndgameWeight + NumPieceEndgameWeight * numPiecesNoPawns;

    //     // Console.WriteLine("debug endweight max: " + TotalEndgameWeight);
    //     // Console.WriteLine("debug endweight nums: " + numQueens + ' ' + numRooks + ' ' + numBishops + ' ' + numKnights + ' ' + numPawns);
    //     // Console.WriteLine("debug endweight total: " + totalWeight);

    //     return Math.Min(Math.Max(TotalEndgameWeight - totalWeight, 0), TotalEndgameWeight) / (double) TotalEndgameWeight;
    // }
    // public double GetMiddlegameWeight(double endgameWeight)
    // {
    //     return -2 * (endgameWeight - 0.3d) * (endgameWeight - 0.3) + 1;
    // }

    // Checkmate Detection
    public bool IsMateScore(int score)
    {
        if (Math.Abs(score) >= CheckmateEval - maxDepth)
        {
            return true;
        }

        return false;
    }

    public int MateInPly(int mateScore)
    {
        return CheckmateEval - Math.Abs(mateScore);
    }
    

    struct MaterialInfo
    {
        // Piece Material Values
        public const int PawnValue = 100;
        public const int KnightValue = 320;
        public const int BishopValue = 325;
        public const int RookValue = 500;
        public const int QueenValue = 900;

        // Endgame weight constants
        const int QueenEndgameWeight = 115;
        const int RookEndgameWeight = 75;
        const int BishopEndgameWeight = 50;
        const int KnightEndgameWeight = 45;
        // const int PawnEndgameWeight = 25;
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
            int numQueens = board.PieceSquares[white ? PieceIndex.WhiteQueen : PieceIndex.BlackQueen].count;
            int numRooks = board.PieceSquares[white ? PieceIndex.WhiteRook : PieceIndex.BlackRook].count;
            int numBishops = board.PieceSquares[white ? PieceIndex.WhiteBishop : PieceIndex.BlackBishop].count;
            int numKnights = board.PieceSquares[white ? PieceIndex.WhiteKnight : PieceIndex.BlackKnight].count;
            int numPawns = board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn].count;

            int kingSquare = board.PieceSquares[white ? PieceIndex.WhiteKing : PieceIndex.BlackKing].squares[0];

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
            whiteKing = blackKing = 0;
            whiteQueens = whiteRooks = whiteBishops = whiteKnights = whitePawns = 0;
            blackQueens = blackRooks = blackBishops = blackKnights = blackPawns = 0;
            whitePawnBehind = blackPawnBehind = 0;

            // bool white = board.Turn;
            ulong[] bitboards = board.BitboardSet.Bitboards;
            
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
                int square = Bitboard.PopLSB(ref whitePawnClone);
                int file = square % 8;
                int rank = square / 8;

                ulong fileMask = Bits.FileMask[file];
                whitePawnBehind |= fileMask & Bits.BlackForwardMask[rank];
            }
            whitePawnBehind |= Bits.Rank1;

            ulong blackPawnClone = blackPawns;
            while (blackPawnClone != 0)
            {
                int square = Bitboard.PopLSB(ref blackPawnClone);
                int file = square % 8;
                int rank = square / 8;

                ulong fileMask = Bits.FileMask[file];
                blackPawnBehind |= fileMask & Bits.WhiteForwardMask[rank];
            }
            blackPawnBehind |= Bits.Rank1 << 7 * 8;
            // Bitboard.Print(friendlyKing);
            // Bitboard.Print(enemyKing);

            // Bitboard.Print(friendlyPawnBehind);
            // Bitboard.Print(enemyPawnBehind);
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

        public int Sum()
        {
            return materialScore + pieceSquareScore + mopUpScore + openFileScore + pawnScore + kingSafety;
        }

        public void Clear()
        {
            materialScore = pieceSquareScore = mopUpScore = openFileScore = pawnScore = kingSafety = 0;
        }
    }



}