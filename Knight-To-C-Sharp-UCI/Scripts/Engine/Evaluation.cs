
using Microsoft.AspNetCore.Components.Web;

public class Evaluation
{
    Engine engine;
    Board board;

    // Checkmate evaluation detection
    public static int CheckmateEval = 99999;
    int maxDepth;

    // Piece Material Values
    public const int PawnValue = 100;
    public const int KnightValue = 320;
    public const int BishopValue = 325;
    public const int RookValue = 500;
    public const int QueenValue = 900;
    static readonly int[] MaterialValues = {PawnValue, KnightValue, BishopValue, RookValue, QueenValue};

    // Piece Square Tables
    static readonly int[] PawnSquareTable = {
         0,   0,   0,   0,   0,   0,   0,   0,
        25,  25,  35,  35,  35,  30,  25,  25,
        10,  10,  20,  30,  30,  20,  10,  10,
         5,   5,  10,  10,  15,  10,   5,   5,
         0,  -5,  10,  15,  15,   5,  -5,   0,
         5,   5,   5,   5,   5, -10,   0,   5,
         5,   5,   5, -25, -25,   5,   5,   5,
         0,   0,   0,   0,   0,   0,   0,   0
    };
    static readonly int[] KnightSquareTable = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0,  5,  5,  5,  5,  0,-30,
        -30,  0,  5, 15, 15,  5,  0,-30,
        -30,  0,  5, 15, 15,  5,  0,-30,
        -30,  5, 10,  5,  5, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };
    static readonly int[] BishopSquareTable =  {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -80,-90,-90,-90,-90,-90,-90,-80,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10, 10,  0,  0,  0,  0, 10,-10,
        -20,-10,-30,-10,-10,-30,-10,-20,
    };
    static readonly int[] RookSquareTable =  {
         0,  0,  0,  0,  0,  0,  0,  0,
        15, 20, 20, 20, 20, 20, 20, 15,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -15, 0,  0,  0,  0,  0,  0, -15,
        -20,-20,  0, 15, 15,  0,-20,-20
    };
    static readonly int[] QueenSquareTable =  {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
        -5,   0,  5,  5,  5,  5,  0, -5,
         0,   0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };
    static readonly int[] KingMidSquareTable = 
    {
        -80, -70, -70, -70, -70, -70, -70, -80, 
        -60, -60, -60, -60, -60, -60, -60, -60, 
        -40, -50, -50, -60, -60, -50, -50, -40, 
        -30, -40, -40, -50, -50, -40, -40, -30, 
        -20, -30, -30, -40, -40, -30, -30, -20, 
        -10, -20, -20, -20, -20, -20, -20, -10, 
         20,  20,  -5,  -5,  -5,  -5,  20,  20, 
         20,  25,  10,  -5,   0,  -5,  25,  25
    };
    static readonly int[] PawnEndSquareTable = 
    {
         0,   0,   0,   0,   0,   0,   0,   0,
        200, 200, 200, 200, 200, 200, 200, 200,
        80,  80,  80,  80,  80,  80,  80,  80,
        40,  40,  40,  40,  40,  40,  35,  35,
        20,  25,  25,  25,  25,  25,  10,  15,
         5,  10,  10,  10,  10, -10,  10,   5,
         5,  10,  10, -25, -25,  10,  10,   5,
         0,   0,   0,   0,   0,   0,   0,   0
    };
    static readonly int[] KingEndSquareTable = 
    {
        -20, -10, -10, -10, -10, -10, -10, -20,
        -5,   0,   5,   5,   5,   5,   0,  -5,
        -10, -5,   20,  30,  30,  20,  -5, -10,
        -15, -10,  35,  45,  45,  35, -10, -15,
        -20, -15,  30,  40,  40,  30, -15, -20,
        -25, -20,  20,  25,  25,  20, -20, -25,
        -30, -25,   0,   0,   0,   0, -25, -30,
        -50, -20, -30, -30, -30, -30, -10, -50
    };
    static readonly int[][] PieceSquareTables = {KnightSquareTable, BishopSquareTable, RookSquareTable, QueenSquareTable};

    // Endgame weight constants
    const int QueenEndgameWeight = 115;
    const int RookEndgameWeight = 75;
    const int BishopEndgameWeight = 50;
    const int KnightEndgameWeight = 45;
    const int PawnEndgameWeight = 25;
    const int NumPieceEndgameWeight = 50;
    const int TotalEndgameWeight = 4 * QueenEndgameWeight + 4 * RookEndgameWeight + 4 * BishopEndgameWeight + 
    4 * KnightEndgameWeight + 16 * PawnEndgameWeight + 8 * NumPieceEndgameWeight;

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
    const int OpenFileBonus = 25;
    const int SemiOpenFileBonus = 25;
    const ulong FileMask = 0x0101010101010101; // Represents A file

    // Calculation Variables
    int whiteKingSquare;
    int blackKingSquare;

    public Evaluation(Engine _engine)
    {
        engine = _engine;
        board = engine.GetBoard();

        maxDepth = engine.GetSettings().unlimitedMaxDepth;
    }

    public int Evaluate()
    {
        whiteKingSquare = board.PieceSquares[PieceIndex.WhiteKing].squares[0];
        blackKingSquare = board.PieceSquares[PieceIndex.BlackKing].squares[0];
        int eval = 0;
        int sign = board.Turn ? 1 : -1;

        double endgameWeight = GetEndgameWeight();
        // double middlegameWeight = GetMiddlegameWeight(endgameWeight);
        // Console.WriteLine("endWeight: " + endgameWeight + " midWeight: " + middlegameWeight);

        int materialCount = CountMaterial();
        // Console.WriteLine("material: " + materialCount);
        eval += materialCount;

        int pieceSquareBonus = PieceSquareTable(endgameWeight);
        // Console.WriteLine("pieceSquareBonus: " + pieceSquareBonus);
        eval += pieceSquareBonus;

        int openFileBonus = CalculateOpenFileBonus();
        // Console.WriteLine("openFileBonus: " + openFileBonus);
        eval += openFileBonus;

        // int kingSafety = (int) (KingSafety() * middlegameWeight);
        // // Console.WriteLine("eval kingSafety: " + kingSafety);
        // eval += kingSafety;

        return eval * sign;
    }

    // King Safety
    int KingSafety()
    {
        int r = 0;
        double whiteKingSafetyWeight = GetKingSafetyWeight(white: true);
        double blackKingSafetyWeight = GetKingSafetyWeight(white: false);

        // Console.WriteLine("white: " + whiteKingSafetyWeight + " black: " + blackKingSafetyWeight);

        r += FrontPawnsPenalty(whiteKingSafetyWeight, whiteKingSafetyWeight);
        r += CalculateKingOpenFilePenalty(whiteKingSafetyWeight, blackKingSafetyWeight);
        
        // Console.WriteLine("total kingSafety: " + r);

        return r;
    }
    int CalculateKingOpenFilePenalty(double whiteWeight = 1, double blackWeight = 1)
    {
        int r = 0;

        int white = 0;

        // White King: Open File Or Semi-Open File for Black
        if (IsSemiOpenFile(whiteKingSquare))
        {
            white -= KingOpenFilePenalty;
        }
        if (whiteKingSquare % 8 > 0)
        {
            if (IsSemiOpenFile(whiteKingSquare - 1))
            {
                white -= KingSideOpenFilePenalty;
            }
        }
        if (whiteKingSquare % 8 < 7)
        {
            if (IsSemiOpenFile(whiteKingSquare + 1))
            {
                white -= KingSideOpenFilePenalty;
            }
        }
        
        int black = 0;

        // Black King: Open File Or Semi-Open File for White
        if (IsSemiOpenFile(blackKingSquare))
        {
            black += KingOpenFilePenalty;
        }
        if (blackKingSquare % 8 > 0)
        {
            if (IsSemiOpenFile(blackKingSquare - 1))
            {
                black += KingSideOpenFilePenalty;
            }
        }
        if (blackKingSquare % 8 < 7)
        {
            if (IsSemiOpenFile(blackKingSquare + 1))
            {
                black += KingSideOpenFilePenalty;
            }
        }

        r += (int) (white * whiteWeight);
        r += (int) (black * blackWeight);

        return r;
    }
    int FrontPawnsPenalty(double whiteWeight = 1, double blackWeight = 1)
    {
        int r = 0;

        r += (int) (CalculateKingFrontPiecePenalty(white: true) * whiteWeight);
        r -= (int) (CalculateKingFrontPiecePenalty(white: false) * blackWeight);

        return r;
    }
    int CalculateKingFrontPiecePenalty(bool white)
    {
        int kingRank = white ? whiteKingSquare / 8 : blackKingSquare / 8;

        if ((white && kingRank > 1) || (!white && kingRank < 6))
        {
            return -3 * DirectKingFrontPawnPenalty;
        }

        int r = 0;

        ulong directMask = GetFrontMask(white: white, isDirect: true);
        ulong distantMask = GetFrontMask(white: white, isDirect: false);

        r += -Bitboard.Count(directMask) * DirectKingFrontPawnPenalty;

        int pawnIndex = white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn;
        int allIndex = white ? PieceIndex.WhiteAll : PieceIndex.BlackAll;

        ulong pawnBitboard = board.BitboardSet.Bitboards[pawnIndex];
        ulong allBitboardNoPawns = board.BitboardSet.Bitboards[allIndex] ^ pawnBitboard;

        r += Bitboard.Count(directMask & pawnBitboard) * DirectKingFrontPawnPenalty;
        r += Bitboard.Count(distantMask & pawnBitboard) * DistantKingFrontPawnPenalty;

        r += Bitboard.Count(directMask & allBitboardNoPawns) * DirectKingFrontPiecePenalty;
        r += Bitboard.Count(distantMask & allBitboardNoPawns) * DistantKingFrontPiecePenalty;

        // Second Rank Penalty
        if ((white && kingRank == 1) || (!white && kingRank == 7))
        {
            r = (int) (r * SecondRankKingFrontPieceMultiplier);
        }

        return r;
    }
    ulong GetFrontMask(bool white, bool isDirect)
    {
        ulong mask = 0;

        int kingSquare = white ? whiteKingSquare : blackKingSquare;
        int rank = kingSquare / 8;
        int file = kingSquare % 8;
        
        int rankOffset = isDirect ? 1 : 2;
        int offset = 8 * rankOffset;

        if (white ? rank + rankOffset <= 7 : rank - rankOffset >= 0)
        {
            int upOffset = white ? offset : - offset;
            
            mask |= (ulong) 1 << (kingSquare + upOffset);

            if (file > 0)
            {
                mask |= (ulong) 1 << (kingSquare + upOffset - 1);
            }
            if (file < 7)
            {
                mask |= (ulong) 1 << (kingSquare + upOffset + 1);
            }
        }

        return mask;
    }

    double GetKingSafetyWeight(bool white)
    {
        int kingSquare = white ? whiteKingSquare : blackKingSquare;
        ulong adjacentFiles = GetAdjacentFileMask(kingSquare);

        int enemyQueenIndex = white ? PieceIndex.BlackQueen : PieceIndex.WhiteQueen;
        int enemyRookIndex = white ? PieceIndex.BlackRook : PieceIndex.WhiteRook;

        ulong enemyQueens = board.BitboardSet.Bitboards[enemyQueenIndex];
        ulong enemyRooks = board.BitboardSet.Bitboards[enemyRookIndex];

        // int enemyNumQueens = board.PieceSquares[enemyQueenIndex].count;
        // int enemyNumRooks = board.PieceSquares[enemyRookIndex].count;

        int queenCount = Bitboard.Count(adjacentFiles & enemyQueens);
        int rookCount = Bitboard.Count(adjacentFiles & enemyRooks);

        double weight = (queenCount * FrontQueenKingSafetyWeight + rookCount * FrontRookKingSafetyWeight) / (double) TotalKingSafetyWeight;

        return Math.Min(weight, 1);
    }
    ulong GetAdjacentFileMask(int square)
    {
        ulong mask = 0;

        int file = square % 8;
        if (file > 0)
        {
            mask |= FileMask << (file - 1);
        }
        if (file < 7)
        {
            mask |= FileMask << (file + 1);
        }

        mask |= FileMask << file;

        return mask;
    }

    // Material Count
    int CountMaterial()
    {
        int value = 0;

        for (int i = 0; i < 5; i++)
        {
            value += MaterialValues[i] * (board.PieceSquares[i].count - board.PieceSquares[i + 6].count);
        }

        return value;
    }

    // Piece square table
    int PieceSquareTable(double endgameWeight = 0)
    {
        int value = 0;
        for (int i = 0; i < 4; i++) // Knight ~ Queen
        {
            // White
            for (int j = 0; j < board.PieceSquares[i + 1].count; j++)
            {
                value += PieceSquareTables[i][Square.FlipIndex(board.PieceSquares[i + 1].squares[j])];
            }
            // Black
            for (int j = 0; j < board.PieceSquares[i + 7].count; j++)
            {
                value -= PieceSquareTables[i][board.PieceSquares[i + 7].squares[j]];
            }
        }

        // Pawns' and Kings' middle and endgame table
        // Pawns
        for (int j = 0; j < board.PieceSquares[PieceIndex.WhitePawn].count; j++)
        {
            int pawnSquare = Square.FlipIndex(board.PieceSquares[PieceIndex.WhitePawn].squares[j]);

            int pawnMid = PawnSquareTable[pawnSquare];
            int pawnEnd = PawnEndSquareTable[pawnSquare];

            value += (int) (pawnMid + (pawnEnd - pawnMid) * endgameWeight);
        }
        for (int j = 0; j < board.PieceSquares[PieceIndex.BlackPawn].count; j++)
        {
            int pawnSquare = board.PieceSquares[PieceIndex.BlackPawn].squares[j];

            int pawnMid = PawnSquareTable[pawnSquare];
            int pawnEnd = PawnEndSquareTable[pawnSquare];

            value -= (int) (pawnMid + (pawnEnd - pawnMid) * endgameWeight);
        }
        
        // Kings
        int wk = Square.FlipIndex(whiteKingSquare);
        int bk = blackKingSquare;

        int whiteKingMid = KingMidSquareTable[wk];
        int blackKingMid = KingMidSquareTable[bk];
        int whiteKingEnd = KingEndSquareTable[wk];
        int blackKingEnd = KingEndSquareTable[bk];

        value += (int) (whiteKingMid + (whiteKingEnd - whiteKingMid) * endgameWeight);
        value -= (int) (blackKingMid + (blackKingEnd - blackKingMid) * endgameWeight);

        return value;
    }
    
    // Open Files
    int CalculateOpenFileBonus()
    {
        int r = 0;

        PieceList whiteRooks = board.PieceSquares[PieceIndex.WhiteRook];
        PieceList blackRooks = board.PieceSquares[PieceIndex.BlackRook];

        for (int i = 0; i < whiteRooks.count; i++)
        {
            int square = whiteRooks.squares[i];
            if (IsOpenFile(square))
            {
                r += OpenFileBonus;
            }
            else if (IsOpenFileFromSide(square, white: true))
            {
                r += SemiOpenFileBonus;
            }
        }
        for (int i = 0; i < blackRooks.count; i++)
        {
            int square = blackRooks.squares[i];
            if (IsOpenFile(square))
            {
                r -= OpenFileBonus;
            }
            else if (IsOpenFileFromSide(square, white: false))
            {
                r -= SemiOpenFileBonus;
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
            return QueenValue;
        }
        else if (pieceType == Piece.Rook)
        {
            return RookValue;
        }
        else if (pieceType == Piece.Knight)
        {
            return KnightValue;
        }
        else if (pieceType == Piece.Bishop)
        {
            return BishopValue;
        }
        else if (pieceType == Piece.Pawn)
        {
            return PawnValue;
        }

        // FailSafe
        return 0;
    }

    // Endgame Weight
    public double GetEndgameWeight()
    {
        int numQueens = board.PieceSquares[PieceIndex.WhiteQueen].count + 
                        board.PieceSquares[PieceIndex.BlackQueen].count;
        int numRooks = board.PieceSquares[PieceIndex.WhiteRook].count + 
                        board.PieceSquares[PieceIndex.BlackRook].count;
        int numBishops = board.PieceSquares[PieceIndex.WhiteBishop].count + 
                        board.PieceSquares[PieceIndex.BlackBishop].count;
        int numKnights = board.PieceSquares[PieceIndex.WhiteKnight].count + 
                        board.PieceSquares[PieceIndex.BlackKnight].count;
        int numPawns = board.PieceSquares[PieceIndex.WhitePawn].count + 
                        board.PieceSquares[PieceIndex.BlackPawn].count;

        int numPiecesNoPawns = numQueens + numRooks + numBishops + numKnights;

        int totalWeight = numQueens * QueenEndgameWeight + numRooks * RookEndgameWeight + numBishops * BishopEndgameWeight + numKnights * KnightEndgameWeight + numPawns * PawnEndgameWeight + NumPieceEndgameWeight * numPiecesNoPawns;

        // Console.WriteLine("debug endweight max: " + TotalEndgameWeight);
        // Console.WriteLine("debug endweight nums: " + numQueens + ' ' + numRooks + ' ' + numBishops + ' ' + numKnights + ' ' + numPawns);
        // Console.WriteLine("debug endweight total: " + totalWeight);

        return Math.Min(Math.Max(TotalEndgameWeight - totalWeight, 0), TotalEndgameWeight) / (double) TotalEndgameWeight;
    }
    public double GetMiddlegameWeight(double endgameWeight)
    {
        return -2 * (endgameWeight - 0.3d) * (endgameWeight - 0.3) + 1;
    }

    // Checkmate Detection
    public bool IsMateScore(int score)
    {
        if (Math.Abs(score) >= CheckmateEval - maxDepth)
        {
            return true;
        }

        return false;
    }

}