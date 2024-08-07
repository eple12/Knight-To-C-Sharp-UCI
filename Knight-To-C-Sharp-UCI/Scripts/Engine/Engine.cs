
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

public class Engine
{
    public Board board;
    public TranspositionTable tt;

    readonly int ttSize = EngineSettings.ttSize;

    public Move bestMove;
    bool isSearching;
    bool cancellationRequested;

    public Engine(Board _board)
    {
        board = _board;
        tt = new TranspositionTable(board, ttSize);
        isSearching = false;
        cancellationRequested = false;
    }

    public void StartSearch(int maxDepth)
    {
        isSearching = true;
        cancellationRequested = false;

        // Return Null Move
        if (maxDepth <= 0)
        {
            
        }
        else
        {
            if (EngineSettings.useIterativeDeepening)
            {
                // Iterative Deepening
                for (int depth = 1; depth <= maxDepth; depth++)
                {
                    int evalThisIteration = Search(depth, Infinity.negativeInfinity, Infinity.positiveInfinity, 0);

                    if (cancellationRequested)
                    {
                        break;
                    }

                    if (Evaluation.IsMateScore(evalThisIteration))
                    {
                        break;
                    }
                }
            }
            else
            {
                Search(maxDepth, Infinity.negativeInfinity, Infinity.positiveInfinity, 0);
            }
        }
        
        // EndSearch();
        isSearching = false;
        cancellationRequested = true;
    }

    int Search(int depth, int alpha, int beta, int plyFromRoot)
    {
        // Console.WriteLine("Search Head PlyFromRoot " + plyFromRoot);
        if (cancellationRequested)
        {
            return 0;
        }

        // Try looking up the current position in the transposition table.
        // If the same position has already been searched to at least an equal depth
        // to the search we're doing now,we can just use the recorded evaluation.
        if (plyFromRoot != 0)
        {
            int ttVal = tt.LookupEvaluation (depth, plyFromRoot, alpha, beta);
            if (ttVal != TranspositionTable.lookupFailed)
            {
                // The Transposition Table cannot store the repetition data, 
                // so whenever a position is repeated, the engine ends up in a threefold draw.
                // To prevent that, check if it's threefold once again!
                // For simplicity, just check if previous position is already reached
                if (board.positionHistory[board.currentZobristKey] > 1)
                {
                    return 0;
                }

                if (plyFromRoot == 0)
                {
                    Move ttMove = tt.GetStoredMove();
                    if (ttMove.moveValue != Move.NullMove.moveValue)
                    {
                        bestMove = ttMove;
                    }
                }

                return ttVal;
            }
        }

        if (depth == 0)
        {
            return EngineSettings.useQSearch ? QuiescenceSearch(alpha, beta) : Evaluation.Evaluate(board);
        }

        List<Move> legalMoves = MoveGen.GenerateMoves(board);

        MateChecker.MateState mateState = MateChecker.GetPositionState(board, legalMoves);
        if (mateState != MateChecker.MateState.None)
        {
            if (mateState == MateChecker.MateState.Checkmate)
            {
                return -Evaluation.checkmateEval + plyFromRoot;
            }

            return 0;
        }

        MoveOrder.GetOrderedList(legalMoves);

        int evalType = TranspositionTable.UpperBound;

        for (int i = 0; i < legalMoves.Count; i++)
        {
            // board.PrintSmallBoard();
            // Console.WriteLine("Search Body PlyFromRoot " + plyFromRoot + " Move " + Move.MoveString(legalMoves[i]));
            // Move.PrintMoveList(legalMoves);
            // board.PrintCastlingData();

            board.MakeMove(legalMoves[i]);

            int eval = -Search(depth - 1, -beta, -alpha, plyFromRoot + 1);

            board.UnmakeMove(legalMoves[i]);

            if (cancellationRequested)
            {
                return 0;
            }

            if (eval >= beta)
            {
                tt.StoreEvaluation (depth, plyFromRoot, beta, TranspositionTable.LowerBound, legalMoves[i]);
                return beta;
            }

            if (eval > alpha)
            {
                alpha = eval;
                evalType = TranspositionTable.Exact;
                
                if (plyFromRoot == 0)
                {
                    // Debugger.PrintPosition(board);
                    bestMove = legalMoves[i];
                    // Move.PrintMove(bestMove);
                }
            }

            tt.StoreEvaluation (depth, plyFromRoot, alpha, evalType, bestMove);
        }

        return alpha;
    }

    int QuiescenceSearch(int alpha, int beta)
    {
        // if (cancellationRequested)
        // {
        //     return alpha;
        // }

        // int ttVal = tt.LookupEvaluation (0, 0, alpha, beta);
        // if (ttVal != TranspositionTable.lookupFailed)
        // {
        //     return ttVal;
        // }

        int standPat = Evaluation.Evaluate(board);

        if (standPat >= beta)
        {
            return beta;
        }
        if (alpha < standPat)
        {
            alpha = standPat;
        }

        List<Move> moves = MoveGen.GenerateMoves(board, true);
        MoveOrder.GetOrderedList(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int eval = -QuiescenceSearch(-beta, -alpha);

            board.UnmakeMove(move);

            // if (cancellationRequested)
            // {
            //     return alpha;
            // }

            if (eval >= beta)
            {
                return beta;
            }
            if (eval > alpha)
            {
                alpha = eval;
            }
        }

        return alpha;
    }

    public Move GetMove()
    {
        return bestMove;
    }

    public void EndSearch()
    {
        isSearching = false;
        cancellationRequested = true;
    
        // AfterThreadedSearch();
        // EnginePlayer.OnSearchComplete();
    }

    public void TimeOut()
    {
        cancellationRequested = true;
    }

    public void BeforeThreadedSearch()
    {
        isSearching = true;
        cancellationRequested = false;
        bestMove = Move.NullMove;
    }

    public bool IsSearching()
    {
        return isSearching;
    }

    // void AfterThreadedSearch()
    // {
    //     ThreadingManager.EngineCancelled();
    // }
}