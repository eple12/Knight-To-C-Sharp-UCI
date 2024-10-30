public class MoveOrder
{
    Board board;

    int[] moveScores;

    const int BiasMultiplier = 1000000;
    const int HashMoveScore = 100 * BiasMultiplier;
    const int WinningCapture = 8 * BiasMultiplier;
    const int LosingCapture = 2 * BiasMultiplier;
    const int Promotion = 6 * BiasMultiplier;
    const int KillerBias = 4 * BiasMultiplier;
    const int RegularBias = 0;

    public int[,,] History;
    public Killers[] KillerMoves;
    public const int MaxKillerPly = 32;

    public MoveOrder(Searcher engine)
    {
        board = engine.GetBoard();

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