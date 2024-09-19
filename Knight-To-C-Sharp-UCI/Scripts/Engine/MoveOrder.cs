public class MoveOrder
{
    Board board;
    TranspositionTable tt;

    int[] moveScores;

    // readonly int captureValueMultiplier = 10;
    const int BiasMultiplier = 1000000;
    const int HashMoveScore = 100 * BiasMultiplier;
    const int WinningCapture = 8 * BiasMultiplier;
    const int LosingCapture = 2 * BiasMultiplier;
    const int Promotion = 6 * BiasMultiplier;
    const int KillerBias = 4 * BiasMultiplier;
    const int RegularBias = 0;
    const int PawnAttackMultipler = 2;

    public int[,,] History;
    public Killers[] KillerMoves;
    public const int MaxKillerPly = 32;

    public MoveOrder(Engine engine)
    {
        board = engine.GetBoard();
        tt = engine.GetTT();

        History = new int[2, 64, 64];
        KillerMoves = new Killers[MaxKillerPly];

        moveScores = new int[MoveGenerator.MaxMoves];
    }

    public void ClearHistory()
    {
        History = new int[2, 64, 64];
    }
    public void ClearKillers()
    {
        KillerMoves = new Killers[MaxKillerPly];
    }

    public Span<Move> GetOrderedList(Span<Move> moves, Move lastIteration, bool inQSearch, int ply)
    {
        moveScores = new int[moves.Length];
        
        GetScores(moves, lastIteration, inQSearch, ply);

        // SortMoves(moves);
        Quicksort(moves, moveScores, 0, moves.Length - 1);

        return moves;
    }

    void GetScores(Span<Move> moves, Move lastIteration, bool inQSearch, int ply)
    {
        // Move hashMove = tt.GetStoredMove();

        // for (int i = 0; i < moves.Length; i++)
        // {
        //     Move move = moves[i];
        //     int score = 0;

        //     int movingPiece = board.Squares[move.startSquare];
        //     int capturedPiece = board.Squares[move.targetSquare];

        //     // Capture
        //     if (capturedPiece != Piece.None)
        //     {
        //         // score += captureValueMultiplier * Evaluation.GetAbsPieceValue(capturedPiece) - Evaluation.GetAbsPieceValue(movingPiece);
        //         int delta = Evaluation.GetAbsPieceValue(movingPiece) - Evaluation.GetAbsPieceValue(capturedPiece);
        //         bool opponentCanRecapture = Bitboard.Contains(board.MoveGen.OpponentAttackMap(), move.targetSquare);

        //         if (opponentCanRecapture)
        //         {
        //             score += ((delta >= 0) ? WinningCapture : LosingCapture) + delta;
        //         }
        //         else
        //         {
        //             score += WinningCapture + delta;
        //         }
        //     }
        //     // Killer Moves & History Moves
        //     else
        //     {
        //         bool isKiller = !inQSearch && ply < MaxKillerPly && KillerMoves[ply].Match(move);
        //         score += isKiller ? KillerBias : RegularBias;
        //         score += History[board.Turn ? 0 : 1, move.startSquare, move.targetSquare];
        //     }
        //     // Promotion
        //     if (Piece.GetType(movingPiece) == Piece.Pawn)
        //     {
        //         if (MoveFlag.IsPromotion(move.flag))
        //         {
        //             score += Promotion + MoveFlag.GetPromotionPieceValue(move.flag);
        //         }
        //     }
        //     else
        //     {
        //         // Moving to a square attacked by an enemy pawn
        //         if (Bitboard.Contains(board.MoveGen.PawnAttackMap(), move.targetSquare))
        //         {
        //             score -= PawnAttackMultipler * Evaluation.GetAbsPieceValue(movingPiece);
        //         }
        //         else if (Bitboard.Contains(board.MoveGen.OpponentAttackMap(), move.targetSquare))
        //         {
        //             score -= Evaluation.GetAbsPieceValue(movingPiece);
        //         }
        //     }

        //     if (Move.IsSame(move, hashMove))
        //     {
        //         score += HashMoveScore;
        //     }
        //     else if (Move.IsSame(move, lastIteration))
        //     {
        //         score += HashMoveScore;
        //     }

        //     moveScores[i] = score;
        // }
        // ulong oppPieces = board.BitboardSet.Bitboards[board.Turn ? PieceIndex.BlackAll : PieceIndex.WhiteAll];
        // ulong[] pawnAttacks = board.IsWhiteToMove ? BitBoardUtility.WhitePawnAttacks : BitBoardUtility.BlackPawnAttacks;
        //bool danger = board.queens[1 - board.MoveColourIndex].Count > 0 || board.rooks[1 - board.MoveColourIndex].Count > 1;

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            if (Move.IsSame(move, lastIteration))
            {
                moveScores[i] = HashMoveScore;
                continue;
            }
            int score = 0;
            int startSquare = move.startSquare;
            int targetSquare = move.targetSquare;

            int movePiece = board.Squares[startSquare];
            int movePieceType = Piece.GetType(movePiece);
            int capturePieceType = Piece.GetType(board.Squares[targetSquare]);
            bool isCapture = capturePieceType != Piece.None;
            int flag = moves[i].flag;
            int pieceValue = Evaluation.GetAbsPieceValue(movePieceType);

            if (isCapture)
            {
                // Order moves to try capturing the most valuable opponent piece with least valuable of own pieces first
                int captureMaterialDelta = Evaluation.GetAbsPieceValue(capturePieceType) - pieceValue;
                bool opponentCanRecapture = Bitboard.Contains(board.MoveGen.OpponentAttackMap(), targetSquare);
                if (opponentCanRecapture)
                {
                    score += (captureMaterialDelta >= 0 ? WinningCapture : LosingCapture) + captureMaterialDelta;
                }
                else
                {
                    score += WinningCapture + captureMaterialDelta;
                }
            }

            if (movePieceType == Piece.Pawn)
            {
                if (flag == MoveFlag.PromoteToQueen && !isCapture)
                {
                    score += Promotion;
                }
            }
            else if (movePieceType == Piece.King)
            {

            }
            else
            {
                // int toScore = PieceSquareTable.Read(movePiece, targetSquare);
                // int fromScore = PieceSquareTable.Read(movePiece, startSquare);
                // score += toScore - fromScore;

                if (Bitboard.Contains(board.MoveGen.PawnAttackMap(), targetSquare))
                {
                    score -= 50;
                }
                else if (Bitboard.Contains(board.MoveGen.AttackMapNoPawn(), targetSquare))
                {
                    score -= 25;
                }

            }

            if (!isCapture)
            {
                //score += regularBias;
                bool isKiller = !inQSearch && ply < MaxKillerPly && KillerMoves[ply].Match(move);
                score += isKiller ? KillerBias : RegularBias;
                score += History[board.Turn ? 0 : 1, startSquare, targetSquare];
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
    public static void Quicksort(Span<Move> values, int[] scores, int low, int high)
    {
        if (low < high)
        {
            int pivotIndex = Partition(values, scores, low, high);
            Quicksort(values, scores, low, pivotIndex - 1);
            Quicksort(values, scores, pivotIndex + 1, high);
        }
    }
    static int Partition(Span<Move> values, int[] scores, int low, int high)
    {
        int pivotScore = scores[high];
        int i = low - 1;

        for (int j = low; j <= high - 1; j++)
        {
            if (scores[j] > pivotScore)
            {
                i++;
                (values[i], values[j]) = (values[j], values[i]);
                (scores[i], scores[j]) = (scores[j], scores[i]);
            }
        }
        (values[i + 1], values[high]) = (values[high], values[i + 1]);
        (scores[i + 1], scores[high]) = (scores[high], scores[i + 1]);

        return i + 1;
    }


    public struct Killers
	{
		public Move moveA;
		public Move moveB;

		public void Add(Move move)
		{
			if (!Move.IsSame(move, moveA))
			{
				moveB = moveA;
				moveA = move;
			}
		}

		public bool Match(Move move) => Move.IsSame(move, moveA) || Move.IsSame(move, moveB);

	}
}