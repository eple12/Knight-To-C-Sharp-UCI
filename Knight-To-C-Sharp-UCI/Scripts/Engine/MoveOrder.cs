using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

public static class MoveOrder
{
    static Board board;
    static TranspositionTable tt;

    static List<Move> moves;
    static List<int> moveScores;

    static readonly int captureValueMultiplier = 10;
    static readonly int squareAttackedByPawnPenalty = 350;

    public static void Initialize(Engine engine)
    {
        // board = Main.mainBoard;
        // tt = Main.engine.tt;
        board = engine.board;
        tt = engine.tt;

        moveScores = new List<int>();
    }

    public static List<Move> GetOrderedList(List<Move> legalMoves)
    {
        moves = legalMoves;

        GetScores();

        SortMoves();

        moveScores.Clear();

        return moves;
    }

    static void GetScores()
    {
        Move hashMove = tt.GetStoredMove();

        foreach (Move move in moves)
        {
            int score = 0;

            int movingPiece = board.position[move.startSquare];
            int capturedPiece = board.position[move.targetSquare];

            // Capture
            if (capturedPiece != Piece.None)
            {
                score += captureValueMultiplier * Evaluation.GetAbsPieceValue(capturedPiece) - Evaluation.GetAbsPieceValue(movingPiece);
            }
            
            if (Piece.GetType(movingPiece) == Piece.Pawn)
            {
                if (MoveFlag.IsPromotion(move.flag))
                {
                    score += MoveFlag.GetPromotionPieceValue(move.flag);
                }
            }
            else
            {
                // Moving to a square attacked by an enemy pawn
                if (Bitboard.Contains(MoveGen.PawnAttackMap(), move.targetSquare))
                {
                    score -= squareAttackedByPawnPenalty;
                }
            }

            if (Bitboard.Contains(MoveGen.AttackMapNoPawn(), move.targetSquare))
            {
                score -= Evaluation.GetAbsPieceValue(board.position[move.startSquare]) / 2;
            }

            if (Bitboard.Contains(MoveGen.AttackMapNoPawn(), move.startSquare))
            {
                score += Evaluation.GetAbsPieceValue(board.position[move.startSquare]);
            }

            if (move.moveValue == hashMove.moveValue)
            {
                score += 100000000;
            }

            moveScores.Add(score);
        }
    }

    static void SortMoves()
    {
        // Bubble Sort
        for (int i = 0; i < moves.Count - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (moveScores[swapIndex] < moveScores[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                }
            }
        }
    }
}