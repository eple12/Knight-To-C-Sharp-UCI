public class MoveOrder
{
    Board board;
    TranspositionTable tt;

    List<Move> moves;
    List<int> moveScores;
    Move bestMoveLastIteration;

    readonly int captureValueMultiplier = 10;
    readonly int squareAttackedByPawnPenalty = 350;

    public MoveOrder(Engine engine)
    {
        board = engine.GetBoard();
        tt = engine.GetTT();

        moves = new List<Move>();
        moveScores = new List<int>();
    }

    public List<Move> GetOrderedList(List<Move> legalMoves, Move lastIteration)
    {
        bestMoveLastIteration = lastIteration;
        moves = legalMoves;

        GetScores();

        SortMoves();

        moveScores.Clear();

        return moves;
    }

    public List<Move> GetOrderedList(List<Move> legalMoves)
    {
        bestMoveLastIteration = Move.NullMove;
        moves = legalMoves;

        GetScores();

        SortMoves();

        moveScores.Clear();

        return moves;
    }

    void GetScores()
    {
        Move hashMove = tt.GetStoredMove();

        foreach (Move move in moves)
        {
            int score = 0;

            int movingPiece = board.Squares[move.startSquare];
            int capturedPiece = board.Squares[move.targetSquare];

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
                score -= Evaluation.GetAbsPieceValue(board.Squares[move.startSquare]) / 2;
            }

            if (Bitboard.Contains(MoveGen.AttackMapNoPawn(), move.startSquare))
            {
                score += Evaluation.GetAbsPieceValue(board.Squares[move.startSquare]);
            }

            if (Move.IsSame(move, hashMove))
            {
                score += Infinity.PositiveInfinity;
            }
            
            if (Move.IsSame(move, bestMoveLastIteration))
            {
                score += Infinity.PositiveInfinity;
            }

            moveScores.Add(score);
        }
    }

    void SortMoves()
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