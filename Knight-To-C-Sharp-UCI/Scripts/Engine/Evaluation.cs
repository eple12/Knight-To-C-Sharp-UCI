
public class Evaluation
{
    Engine engine;
    Board board;

    // Checkmate evaluation detection
    public static int checkmateEval = 99999;
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
         0,  -5,   5,  15,  15,   0,  -5,   0,
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
        -10,  0,  0,  0,  0,  0,  0,-10,
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
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5, -5,  0, 15, 15,  5, -5, -5
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
         20,  30,  10,  -5,   0,  -5,  30,  20
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
    static readonly int[][] PieceSquareTables = {PawnSquareTable, KnightSquareTable, BishopSquareTable, RookSquareTable, QueenSquareTable, KingMidSquareTable, PawnEndSquareTable, KingEndSquareTable};

    // Endgame weight constants
    const int QueenEndgameWeight = 115;
    const int RookEndgameWeight = 75;
    const int BishopEndgameWeight = 50;
    const int KnightEndgameWeight = 45;
    const int PawnEndgameWeight = 25;
    const int TotalEndgameWeight = 4 * QueenEndgameWeight + 4 * RookEndgameWeight + 4 * BishopEndgameWeight + 
    4 * KnightEndgameWeight + 16 * PawnEndgameWeight;

    public Evaluation(Engine _engine)
    {
        engine = _engine;
        board = engine.GetBoard();

        maxDepth = engine.GetSettings().unlimitedMaxDepth;
    }

    public int Evaluate(Board _board)
    {
        board = _board;
        int eval = 0;
        int sign = board.Turn ? 1 : -1;
        double endgameWeight = GetEndgameWeight();

        eval += CountMaterial() * sign;

        eval += PieceSquareTable(endgameWeight) * sign;

        return eval;
    }

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
        for (int i = BitboardIndex.WhiteKnight; i <= BitboardIndex.WhiteQueen; i++) // Knight ~ Queen
        {
            // White
            for (int j = 0; j < board.PieceSquares[i].count; j++)
            {
                value += PieceSquareTables[i][Square.FlipIndex(board.PieceSquares[i].squares[j])];
            }
            // Black
            for (int j = 0; j < board.PieceSquares[i + 6].count; j++)
            {
                value -= PieceSquareTables[i][board.PieceSquares[i + 6].squares[j]];
            }
        }

        // Pawns' and Kings' middle and endgame table
        // Pawns
        for (int j = 0; j < board.PieceSquares[BitboardIndex.WhitePawn].count; j++)
        {
            int pawnSquare = Square.FlipIndex(board.PieceSquares[BitboardIndex.WhitePawn].squares[j]);

            int pawnMid = PieceSquareTables[BitboardIndex.WhitePawn][pawnSquare];
            int pawnEnd = PieceSquareTables[BitboardIndex.WhiteKing + 1][pawnSquare];

            value += (int) (pawnMid + (pawnEnd - pawnMid) * endgameWeight);
        }
        for (int j = 0; j < board.PieceSquares[BitboardIndex.BlackPawn].count; j++)
        {
            int pawnSquare = board.PieceSquares[BitboardIndex.BlackPawn].squares[j];

            int pawnMid = PieceSquareTables[BitboardIndex.WhitePawn][pawnSquare];
            int pawnEnd = PieceSquareTables[BitboardIndex.WhiteKing + 1][pawnSquare];

            value -= (int) (pawnMid + (pawnEnd - pawnMid) * endgameWeight);
        }
        
        // Kings
        int whiteKingSquare = Square.FlipIndex(board.PieceSquares[BitboardIndex.WhiteKing].squares[0]);
        int blackKingSquare = board.PieceSquares[BitboardIndex.BlackKing].squares[0];

        int whiteKingMid = PieceSquareTables[BitboardIndex.WhiteKing][whiteKingSquare];
        int blackKingMid = PieceSquareTables[BitboardIndex.WhiteKing][blackKingSquare];
        int whiteKingEnd = PieceSquareTables[BitboardIndex.WhiteKing + 2][whiteKingSquare];
        int blackKingEnd = PieceSquareTables[BitboardIndex.WhiteKing + 2][blackKingSquare];

        value += (int) (whiteKingMid + (whiteKingEnd - whiteKingMid) * endgameWeight);
        value -= (int) (blackKingMid + (blackKingEnd - blackKingMid) * endgameWeight);

        return value;
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
        int numQueens = board.PieceSquares[BitboardIndex.WhiteQueen].count + 
                        board.PieceSquares[BitboardIndex.BlackQueen].count;
        int numRooks = board.PieceSquares[BitboardIndex.WhiteRook].count + 
                        board.PieceSquares[BitboardIndex.BlackRook].count;
        int numBishops = board.PieceSquares[BitboardIndex.WhiteBishop].count + 
                        board.PieceSquares[BitboardIndex.BlackBishop].count;
        int numKnights = board.PieceSquares[BitboardIndex.WhiteKnight].count + 
                        board.PieceSquares[BitboardIndex.BlackKnight].count;
        int numPawns = board.PieceSquares[BitboardIndex.WhitePawn].count + 
                        board.PieceSquares[BitboardIndex.BlackPawn].count;

        int totalWeight = numQueens * QueenEndgameWeight + numRooks * RookEndgameWeight + numBishops * BishopEndgameWeight + numKnights * KnightEndgameWeight + numPawns * PawnEndgameWeight;

        // Console.WriteLine("debug endweight max: " + TotalEndgameWeight);
        // Console.WriteLine("debug endweight nums: " + numQueens + ' ' + numRooks + ' ' + numBishops + ' ' + numKnights + ' ' + numPawns);
        // Console.WriteLine("debug endweight total: " + totalWeight);

        return Math.Min(Math.Max(TotalEndgameWeight - totalWeight, 0), TotalEndgameWeight) / (double) TotalEndgameWeight;
    }


    public bool IsMateScore(int score)
    {
        if (Math.Abs(score) >= checkmateEval - maxDepth)
        {
            return true;
        }

        return false;
    }

}