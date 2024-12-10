using System.Runtime.CompilerServices;

public class MoveOrder
{
    Board board;
    int[] moveScores;

    public const int HashMoveScore = 2_097_152;
    public const int QueenPromotionCaptureBaseScore = GoodCaptureBaseScore + PromotionMoveScore;
    public const int GoodCaptureBaseScore = 1_048_576;
    public const int KillerMoveValue = 524_288;
    public const int PromotionMoveScore = 32_768;
    public const int BadCaptureBaseScore = 16_384;
    // Negative value to make sure history moves doesn't reach other important moves
    public const int BaseMoveScore = int.MinValue / 2;

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

    public Span<Move> GetOrderedList(ref Span<Move> moves, Move lastIteration, bool inQSearch, int ply, SEE.SEEPinData pinData)
    {
        // moveScores = new int[moves.Length];
        
        GetScores(moves, lastIteration, inQSearch, ply, pinData);

        // SortMoves(moves);
        Quicksort(ref moves, moveScores, 0, moves.Length - 1);

        return moves;
    }

    void GetScores(Span<Move> moves, Move lastIteration, bool inQSearch, int ply, SEE.SEEPinData pinData)
    {
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            moveScores[i] = ScoreMove(move, lastIteration, inQSearch, ply, pinData);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int ScoreMove(Move move, Move hash, bool inQSearch, int ply, SEE.SEEPinData pinData)
    {
        if (Move.IsSame(move, hash))
        {
            return HashMoveScore;
        }
        int startSquare = move.startSquare;
        int targetSquare = move.targetSquare;

        int movePiece = board.Squares[startSquare];
        int capturePiece = board.Squares[targetSquare];
        int capturePieceType = Piece.GetType(capturePiece);
        int capturePieceValue = Evaluation.GetAbsPieceValue(capturePieceType);
        bool isCapture = capturePieceType != Piece.None;
        int flag = move.flag;

        if (flag == MoveFlag.PromoteToQueen)
        {
            if (isCapture)
            {
                return QueenPromotionCaptureBaseScore + capturePieceValue;
            }

            return PromotionMoveScore + (SEE.HasPositiveScore(board, move, pinData) ? GoodCaptureBaseScore : BadCaptureBaseScore);
        }

        if (isCapture)
        {
            int baseCapture = (flag == MoveFlag.EnpassantCapture || MoveFlag.IsPromotion(flag) || SEE.IsGoodCapture(board, move, pinData)) ? GoodCaptureBaseScore : BadCaptureBaseScore;

            return baseCapture + MostValueableVictimLeastValuableAttacker[Piece.GetPieceIndex(movePiece)][Piece.GetPieceIndex(capturePieceType)];
        }

        if (MoveFlag.IsPromotion(flag))
        {
            return PromotionMoveScore;
        }

        if (!inQSearch)
        {
            bool isKiller = ply < MaxKillerPly && KillerMoves[ply].Match(move);
            return BaseMoveScore + (isKiller ? KillerMoveValue : 0) + History[board.Turn ? 0 : 1, startSquare, targetSquare];
        }

        return BaseMoveScore;
    }

    public static void Quicksort(ref Span<Move> values, int[] scores, int low, int high)
    {
        if (low < high)
        {
            int pivotIndex = Partition(values, scores, low, high);
            Quicksort(ref values, scores, low, pivotIndex - 1);
            Quicksort(ref values, scores, pivotIndex + 1, high);
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

    public int[] GetLastMoveScores()
    {
        return moveScores;
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


    /// <summary>
    /// MVV LVA [attacker,victim] 12x11
    /// Original based on
    /// https://github.com/maksimKorzh/chess_programming/blob/master/src/bbc/move_ordering_intro/bbc.c#L2406
    ///             (Victims)   Pawn Knight Bishop  Rook   Queen  King
    /// (Attackers)
    ///       Pawn              105    205    305    405    505    0
    ///     Knight              104    204    304    404    504    0
    ///     Bishop              103    203    303    403    503    0
    ///       Rook              102    202    302    402    502    0
    ///      Queen              101    201    301    401    501    0
    ///       King              100    200    300    400    500    0
    /// </summary>
    public static readonly int[][] MostValueableVictimLeastValuableAttacker =
    [         //    P     N     B     R      Q  K    p    n      b    r      q          k
        /* P */ [   0,    0,    0,    0,     0, 0,  1500, 4000, 4500, 5500, 11500 ], // 0],
        /* N */ [   0,    0,    0,    0,     0, 0,  1400, 3900, 4400, 5400, 11400 ], // 0],
        /* B */ [   0,    0,    0,    0,     0, 0,  1300, 3800, 4300, 5300, 11300 ], // 0],
        /* R */ [   0,    0,    0,    0,     0, 0,  1200, 3700, 4200, 5200, 11200 ], // 0],
        /* Q */ [   0,    0,    0,    0,     0, 0,  1100, 3600, 4100, 5100, 11100 ], // 0],
        /* K */ [   0,    0,    0,    0,     0, 0,  1000, 3500, 4001, 5000, 11000 ], // 0],
        /* p */ [1500, 4000, 4500, 5500, 11500, 0,     0,    0,    0,    0,     0 ], // 0],
        /* n */ [1400, 3900, 4400, 5400, 11400, 0,     0,    0,    0,    0,     0 ], // 0],
        /* b */ [1300, 3800, 4300, 5300, 11300, 0,     0,    0,    0,    0,     0 ], // 0],
        /* r */ [1200, 3700, 4200, 5200, 11200, 0,     0,    0,    0,    0,     0 ], // 0],
        /* q */ [1100, 3600, 4100, 5100, 11100, 0,     0,    0,    0,    0,     0 ], // 0],
        /* k */ [1000, 3500, 4001, 5000, 11000, 0,     0,    0,    0,    0,     0 ], // 0]
    ];

}