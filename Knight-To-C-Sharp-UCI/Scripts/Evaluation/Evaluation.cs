
public class Evaluation
{
    Engine engine;
    Board board;

    // Checkmate evaluation detection
    public static int CheckmateEval = 99999;
    int maxDepth;

    // Pawns
    static readonly int[] PassedPawnBonus = { 0, 120, 80, 60, 40, 30, 30, 0 };
    static readonly int[] IsolatedPawnPenaltyByCount = { 0, 10, 25, 50, 75, 75, 75, 75, 75 };

    // King Safety
    const int DirectKingFrontPawnPenalty = 30;
    const int DistantKingFrontPawnPenalty = 25;
    const int DirectKingFrontPiecePenalty = 15;
    const int DistantKingFrontPiecePenalty = 10;
    const double SecondRankKingFrontPieceMultiplier = 0.5d;

    const int KingOpenFilePenalty = 50;
    const int KingSideOpenFilePenalty = 25;

    const int FrontQueenKingSafetyWeight = 50;
    const int FrontRookKingSafetyWeight = 35;
    const int TotalKingSafetyWeight = 1 * FrontQueenKingSafetyWeight + 2 * FrontRookKingSafetyWeight;

    // Open File
    const int OpenFileBonus = 15;
    const int SemiOpenFileBonus = 15;
    const ulong FileMask = 0x0101010101010101; // Represents A file

    // Calculation Variables
    MaterialInfo whiteMaterial;
    MaterialInfo blackMaterial;

    EvaluationData whiteEval;
    EvaluationData blackEval;


    public Evaluation(Engine _engine)
    {
        engine = _engine;
        board = engine.GetBoard();

        maxDepth = engine.GetSettings().unlimitedMaxDepth;
    }

    public int Evaluate()
    {
        // int eval = 0;
        int sign = board.Turn ? 1 : -1;

        whiteMaterial = MaterialInfo.GetMaterialInfo(board, white: true);
        blackMaterial = MaterialInfo.GetMaterialInfo(board, white: false);

        whiteEval = new EvaluationData();
        blackEval = new EvaluationData();

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
        whiteEval.pawnScore = PawnBonus(white: true);
        blackEval.pawnScore = PawnBonus(white: false);

        // int kingSafety = (int) (KingSafety() * reverseEndWeight);
        // // // Console.WriteLine("eval kingSafety: " + kingSafety);
        // eval += kingSafety;

        return (whiteEval.Sum() - blackEval.Sum()) * sign;
    }

    // Pawns
    int PawnBonus(bool white)
    {
        return PassedPawns(white: white) + IsolatedPawns(white: white);
    }
    int PassedPawns(bool white)
    {
        int eval = 0;
        PieceList pawns = board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];
        ulong enemyPawns = board.BitboardSet.Bitboards[white ? PieceIndex.BlackPawn : PieceIndex.WhitePawn];

        for (int i = 0; i < pawns.count; i++)
        {
            int square = pawns.squares[i];

            ulong passMask = (white ? Bits.WhitePassedPawnMask : Bits.BlackPassedPawnMask)[square];
            if ((passMask & enemyPawns) == 0)
            {
                int numSquareFromPromotion = white ? 7 - (square / 8) : square / 8;
                eval += PassedPawnBonus[numSquareFromPromotion];
                // Console.WriteLine(numSquareFromPromotion);
            }
        }

        return eval;
    }
    int IsolatedPawns(bool white)
    {
        int eval = 0;
        PieceList pawns = board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];
        ulong friendlyPawns = board.BitboardSet.Bitboards[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];
        int numIsolatedPawns = 0;

        for (int i = 0; i < pawns.count; i++)
        {
            int square = pawns.squares[i];

            ulong adjMask = Bits.AdjacentFilesMask[square % 8];
            if ((adjMask & friendlyPawns) == 0)
            {
                numIsolatedPawns++;
            }
        }

        eval -= IsolatedPawnPenaltyByCount[numIsolatedPawns];

        return eval;
    }

    // King Safety
    // int KingSafety()
    // {
    //     int r = 0;
    //     double whiteKingSafetyWeight = GetKingSafetyWeight(white: true);
    //     double blackKingSafetyWeight = GetKingSafetyWeight(white: false);

    //     // Console.WriteLine("white: " + whiteKingSafetyWeight + " black: " + blackKingSafetyWeight);

    //     r += FrontPawnsPenalty(whiteKingSafetyWeight, whiteKingSafetyWeight);
    //     r += CalculateKingOpenFilePenalty(whiteKingSafetyWeight, blackKingSafetyWeight);
        
    //     // Console.WriteLine("total kingSafety: " + r);

    //     return r;
    // }
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

    // double GetKingSafetyWeight(bool white)
    // {
    //     int kingSquare = white ? whiteKingSquare : blackKingSquare;
    //     ulong adjacentFiles = GetAdjacentFileMask(kingSquare);

    //     int enemyQueenIndex = white ? PieceIndex.BlackQueen : PieceIndex.WhiteQueen;
    //     int enemyRookIndex = white ? PieceIndex.BlackRook : PieceIndex.WhiteRook;

    //     ulong enemyQueens = board.BitboardSet.Bitboards[enemyQueenIndex];
    //     ulong enemyRooks = board.BitboardSet.Bitboards[enemyRookIndex];

    //     // int enemyNumQueens = board.PieceSquares[enemyQueenIndex].count;
    //     // int enemyNumRooks = board.PieceSquares[enemyRookIndex].count;

    //     int queenCount = Bitboard.Count(adjacentFiles & enemyQueens);
    //     int rookCount = Bitboard.Count(adjacentFiles & enemyRooks);

    //     double weight = (queenCount * FrontQueenKingSafetyWeight + rookCount * FrontRookKingSafetyWeight) / (double) TotalKingSafetyWeight;

    //     return Math.Min(weight, 1);
    // }
    // ulong GetAdjacentFileMask(int square)
    // {
    //     ulong mask = 0;

    //     int file = square % 8;
    //     if (file > 0)
    //     {
    //         mask |= FileMask << (file - 1);
    //     }
    //     if (file < 7)
    //     {
    //         mask |= FileMask << (file + 1);
    //     }

    //     mask |= FileMask << file;

    //     return mask;
    // }

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
        value += (int) (ReadPieceSquareTable(PieceSquareTable.Pawn, board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn], white) * endgameWeight);

        value += (int) (ReadPieceSquareTable(PieceSquareTable.King, board.PieceSquares[white ? PieceIndex.WhiteKing : PieceIndex.BlackKing], white) * reverseEndWeight);
        value += (int) (ReadPieceSquareTable(PieceSquareTable.King, board.PieceSquares[white ? PieceIndex.WhiteKing : PieceIndex.BlackKing], white) * endgameWeight);

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
            endgameWeight = Math.Min(1, (double) endSum / TotalEndgameWeight);
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

    struct EvaluationData
    {
        public int materialScore;
        public int pieceSquareScore;
        public int mopUpScore;
        public int openFileScore;
        public int pawnScore;

        public int Sum()
        {
            return materialScore + pieceSquareScore + mopUpScore + openFileScore + pawnScore;
        }
    }



}