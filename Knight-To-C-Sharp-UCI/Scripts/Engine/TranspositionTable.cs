public class TranspositionTable
{
    // Credit: Sebastian Lague

    public const int lookupFailed = int.MinValue;

    // The value for this position is the exact evaluation
    public const int Exact = 0;

    // A move was found during the search that was too good, meaning the opponent will play a different move earlier on,
    // not allowing the position where this move was available to be reached. Because the search cuts off at
    // this point (beta cut-off), an even better move may exist. This means that the evaluation for the
    // position could be even higher, making the stored value the lower bound of the actual value.
    public const int LowerBound = 1; // Beta

    // No move during the search resulted in a position that was better than the current player could get from playing a
    // different move in an earlier position (i.e eval was <= alpha for all moves in the position).
    // Due to the way alpha-beta search works, the value we get here won't be the exact evaluation of the position,
    // but rather the upper bound of the evaluation. This means that the evaluation is, at most, equal to this value.
    public const int UpperBound = 2; // Alpha

    public Entry[] entries;

    public readonly ulong size;
    public bool enabled = true;
    Engine engine;
    Board board;
    Evaluation evaluation;

    public TranspositionTable (Engine _engine)
    {
        engine = _engine;
        board = engine.GetBoard();
        size = (ulong) (engine.GetSettings().TTSizeInMB * 1024 * 1024 / Entry.GetSize());
        evaluation = engine.GetEvaluation();

        entries = new Entry[size];
    }

    public void Clear ()
    {
        entries = new Entry[size];
    }

    public ulong Index
    {
        get
        {
            return board.ZobristKey % size;
        }
    }

    // There might be a collision (Zobrist key), I have to check if the move is legal
    public Move GetStoredMove ()
    {
        return entries[Index].key == board.ZobristKey ? entries[Index].move : Move.NullMove;
    }

    public int LookupEvaluation (int depth, int plyFromRoot, int alpha, int beta)
    {
        if (!enabled)
        {
            return lookupFailed;
        }

        Entry entry = entries[Index];

        if (entry.key == board.ZobristKey)
        {
            // Only use stored evaluation if it has been searched to at least the same depth as would be searched now
            if (entry.depth >= depth)
            {
                int correctedScore = CorrectRetrievedMateScore (entry.value, plyFromRoot);
                // We have stored the exact evaluation for this position, so return it
                if (entry.nodeType == Exact)
                {
                    return correctedScore;
                }
                // We have stored the upper bound of the eval for this position. If it's less than alpha then we don't need to
                // search the moves in this position as they won't interest us; otherwise we will have to search to find the exact value
                if (entry.nodeType == UpperBound && correctedScore <= alpha)
                {
                    return correctedScore;
                }
                // We have stored the lower bound of the eval for this position. Only return if it causes a beta cut-off.
                if (entry.nodeType == LowerBound && correctedScore >= beta)
                {
                    return correctedScore;
                }
            }
        }
        return lookupFailed;
    }

    public void StoreEvaluation (int depth, int numPlySearched, int eval, int evalType, Move move)
    {
        if (!enabled)
        {
            return;
        }

        // if (board.ZobristKey == 14266748245563660247)
        // {
        //     board.PrintSmallBoard();
        // }

        if (entries[Index].depth > depth)
        {
            return;
        }

        Entry entry = new Entry (board.ZobristKey, CorrectMateScoreForStorage (eval, numPlySearched), (byte) depth, (byte) evalType, move);
        entries[Index] = entry;
    }

    // Position-Based Mate Eval
    int CorrectMateScoreForStorage (int score, int numPlySearched)
    {
        if (evaluation.IsMateScore (score))
        {
            int sign = System.Math.Sign (score);
            return (score * sign + numPlySearched) * sign;
        }
        return score;
    }

    // Root-Based Mate Eval
    int CorrectRetrievedMateScore (int score, int numPlySearched)
    {
        if (evaluation.IsMateScore (score))
        {
            int sign = System.Math.Sign (score);
            return (score * sign - numPlySearched) * sign;
        }
        return score;
    }

    public void Print()
    {
        Console.WriteLine("###############");
        // for (int i = 0; i < (int) size; i++)
        // {
        //     if (entries[i].key != 0)
        //     {
        //         Entry e = entries[i];
        //         Console.WriteLine("key: " + e.key + " val: " + e.value + " move: " + Move.MoveString(e.move) + " index: " + i);
        //     }
        // }
        Entry e = entries[Index];
        Console.WriteLine("key: " + e.key + " val: " + e.value + " move: " + Move.MoveString(e.move) + " index: " + Index + " depth " + e.depth + " type " + e.nodeType);
        Console.WriteLine("###############");
    }

    public struct Entry // 16 bytes.
    {
        public readonly ulong key;
        public readonly int value;
        public readonly Move move;
        public readonly byte depth;
        public readonly byte nodeType;

        public Entry (ulong key, int value, byte depth, byte nodeType, Move move)
        {
            this.key = key;
            this.value = value;
            this.depth = depth; // depth is how many ply were searched *ahead* from this position
            this.nodeType = nodeType;
            this.move = move;
        }

        public static int GetSize ()
        {
            return System.Runtime.InteropServices.Marshal.SizeOf<Entry> ();
        }
    }
}