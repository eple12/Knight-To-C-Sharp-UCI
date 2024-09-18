public class MoveOrder
{
    Board board;
    TranspositionTable tt;

    int[] moveScores;
    Move bestMoveLastIteration;

    // readonly int captureValueMultiplier = 10;
    const int BiasMultiplier = 1000000;
    const int WinningCapture = 8 * BiasMultiplier;
    const int LosingCapture = 2 * BiasMultiplier;

    const int Promotion = 6 * BiasMultiplier;

    const int PawnAttackMultipler = 2;

    public MoveOrder(Engine engine)
    {
        board = engine.GetBoard();
        tt = engine.GetTT();

        moveScores = new int[MoveGenerator.MaxMoves];
    }

    public Span<Move> GetOrderedList(Span<Move> legalMoves, Move lastIteration)
    {
        bestMoveLastIteration = lastIteration;

        return Calculate(legalMoves);
    }

    public Span<Move> GetOrderedList(Span<Move> legalMoves)
    {
        bestMoveLastIteration = Move.NullMove;

        return Calculate(legalMoves);
    }

    Span<Move> Calculate(Span<Move> moves)
    {
        moveScores = new int[MoveGenerator.MaxMoves];
        
        GetScores(moves);

        SortMoves(moves);

        return moves;
    }

    void GetScores(Span<Move> moves)
    {
        Move hashMove = tt.GetStoredMove();

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            int score = 0;

            int movingPiece = board.Squares[move.startSquare];
            int capturedPiece = board.Squares[move.targetSquare];

            // Capture
            if (capturedPiece != Piece.None)
            {
                // score += captureValueMultiplier * Evaluation.GetAbsPieceValue(capturedPiece) - Evaluation.GetAbsPieceValue(movingPiece);
                int delta = Evaluation.GetAbsPieceValue(movingPiece) - Evaluation.GetAbsPieceValue(capturedPiece);
                bool opponentCanRecapture = Bitboard.Contains(board.MoveGen.OpponentAttackMap(), move.targetSquare);

                if (opponentCanRecapture)
                {
                    score += ((delta >= 0) ? WinningCapture : LosingCapture) + delta;
                }
                else
                {
                    score += WinningCapture + delta;
                }
            }
            
            if (Piece.GetType(movingPiece) == Piece.Pawn)
            {
                if (MoveFlag.IsPromotion(move.flag))
                {
                    score += Promotion + MoveFlag.GetPromotionPieceValue(move.flag);
                }
            }
            else
            {
                // Moving to a square attacked by an enemy pawn
                if (Bitboard.Contains(board.MoveGen.PawnAttackMap(), move.targetSquare))
                {
                    score -= PawnAttackMultipler * Evaluation.GetAbsPieceValue(movingPiece);
                }
                else if (Bitboard.Contains(board.MoveGen.OpponentAttackMap(), move.targetSquare))
                {
                    score -= Evaluation.GetAbsPieceValue(movingPiece);
                }
            }

            if (Move.IsSame(move, hashMove))
            {
                score += Infinity.PositiveInfinity;
            }
            else if (Move.IsSame(move, bestMoveLastIteration))
            {
                score += Infinity.PositiveInfinity;
            }

            moveScores[i] = score;
        }
    }

    void SortMoves(Span<Move> moves)
    {
        // Bubble Sort
        for (int i = 0; i < moves.Length - 1; i++)
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