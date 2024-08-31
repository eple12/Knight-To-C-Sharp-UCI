using System;

public class Evaluation
{
    Engine engine;
    Board board;
    public static int checkmateEval = 99999;
    int maxDepth;

    int eval;
    int sign;

    public static readonly int pawnValue = 100;
    public static readonly int knightValue = 320;
    public static readonly int bishopValue = 325;
    public static readonly int rookValue = 500;
    public static readonly int queenValue = 900;

    static readonly int[] pawnSquareTable = {
        0,   0,   0,   0,   0,   0,   0,   0,
        50,  50,  50,  50,  50,  50,  50,  50,
        10,  10,  20,  30,  30,  20,  10,  10,
        5,   5,  10,  25,  25,  10,   5,   5,
        0,  -5,  20,  20,  25,   0,  -5,   0,
        5,  10,  10,  10,  10, -10,  10,   5,
        5,  10,  10, -25, -25,  10,  10,   5,
        0,   0,   0,   0,   0,   0,   0,   0
    };
    static readonly int[] knightSquareTable = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0,  5,  5,  5,  5,  0,-30,
        -30,  0,  5, 15, 15,  5,  0,-30,
        -30,  0,  5, 15, 15,  5,  0,-30,
        -30,  5, 10,  5,  5, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };
    static readonly int[] bishopSquareTable =  {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10, 10,  0,  0,  0,  0, 10,-10,
        -20,-10,-30,-10,-10,-30,-10,-20,
    };
    static readonly int[] rookSquareTable =  {
         0,  0,  0,  0,  0,  0,  0,  0,
        15, 20, 20, 20, 20, 20, 20, 15,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5, -5,  0, 15, 15,  5, -5, -5
    };
    static readonly int[] queenSquareTable =  {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
        -5,   0,  5,  5,  5,  5,  0, -5,
         0,   0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };
    static readonly int[] kingMidSquareTable = 
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
    static readonly int[] kingEndSquareTable = 
    {
        -20, -10, -10, -10, -10, -10, -10, -20,
        -5,   0,   5,   5,   5,   5,   0,  -5,
        -10, -5,   20,  30,  30,  20,  -5, -10,
        -15, -10,  35,  45,  45,  35, -10, -15,
        -20, -15,  30,  40,  40,  30, -15, -20,
        -25, -20,  20,  25,  25,  20, -20, -25,
        -30, -25,   0,   0,   0,   0, -25, -30,
        -50, -30, -30, -30, -30, -30, -30, -50
    };
    
    static readonly int[][] pieceSquareTables = {pawnSquareTable, knightSquareTable, bishopSquareTable, rookSquareTable, queenSquareTable, kingMidSquareTable, kingEndSquareTable};

    static readonly int[] materialValue = {pawnValue, knightValue, bishopValue, rookValue, queenValue};

    public Evaluation(Engine _engine)
    {
        engine = _engine;
        board = engine.GetBoard();

        maxDepth = engine.GetSettings().unlimitedMaxDepth;
    }

    public int Evaluate(Board _board)
    {
        board = _board;
        eval = 0;
        sign = board.isWhiteTurn ? 1 : -1;

        CountMaterial();

        PieceSquareTable();

        return eval;
    }

    void CountMaterial()
    {
        for (int i = 0; i < 5; i++)
        {
            eval += materialValue[i] * (board.pieceSquares[i].count - board.pieceSquares[i + 6].count) * sign;
        }
    }

    // Piece square table
    void PieceSquareTable()
    {
        for (int i = 0; i < 6; i++) // Pawn ~ King
        {
            for (int j = 0; j < board.pieceSquares[i].count; j++)
            {
                eval += pieceSquareTables[i][GetFlippedPieceSquareIndex(board.pieceSquares[i].squares[j])] * sign;
            }
            for (int j = 0; j < board.pieceSquares[i + 6].count; j++)
            {
                eval -= pieceSquareTables[i][board.pieceSquares[i + 6].squares[j]] * sign;
            }
        }
    }
    int GetFlippedPieceSquareIndex(int square)
    {
        return (square % 8) + (7 - square / 8) * 8;
    }

    // Move Ordering
    public int GetAbsPieceValue(int piece)
    {
        int pieceType = Piece.GetType(piece);

        if (pieceType == Piece.Queen)
        {
            return queenValue;
        }
        else if (pieceType == Piece.Rook)
        {
            return rookValue;
        }
        else if (pieceType == Piece.Knight)
        {
            return knightValue;
        }
        else if (pieceType == Piece.Bishop)
        {
            return bishopValue;
        }
        else if (pieceType == Piece.Pawn)
        {
            return pawnValue;
        }

        // FailSafe
        return 0;
    }

    // Endgame Weight
    public double GetEndgameWeight()
    {
        double weight = 0;

        return weight;
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