public class MoveOrder
{
    Board board;
    TranspositionTable tt;
    Evaluation evaluation;

    List<Move> moves;
    List<int> moveScores;
    Move bestMoveLastIteration;

    readonly int captureValueMultiplier = 10;
    readonly int squareAttackedByPawnPenalty = 350;

    public MoveOrder(Engine engine)
    {
        board = engine.GetBoard();
        tt = engine.GetTT();
        evaluation = engine.GetEvaluation();

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

            int movingPiece = board.position[move.startSquare];
            int capturedPiece = board.position[move.targetSquare];

            // Capture
            if (capturedPiece != Piece.None)
            {
                score += captureValueMultiplier * evaluation.GetAbsPieceValue(capturedPiece) - evaluation.GetAbsPieceValue(movingPiece);
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
                score -= evaluation.GetAbsPieceValue(board.position[move.startSquare]) / 2;
            }

            if (Bitboard.Contains(MoveGen.AttackMapNoPawn(), move.startSquare))
            {
                score += evaluation.GetAbsPieceValue(board.position[move.startSquare]);
            }

            if (Move.IsSame(move, hashMove))
            {
                score += 100000000;
            }
            
            if (Move.IsSame(move, bestMoveLastIteration))
            {
                score += 200000000;
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