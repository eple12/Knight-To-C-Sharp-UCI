
public class Engine
{
    Board board;
    TranspositionTable tt;
    MoveOrder moveOrder;
    EngineSettings settings;
    Evaluation evaluation;

    Move bestMove;
    bool isSearching;
    bool cancellationRequested;

    public event Action OnSearchComplete;

    public Engine(Board _board, EngineSettings _settings)
    {
        board = _board;
        settings = _settings;

        evaluation = new Evaluation(this);
        tt = new TranspositionTable(this);

        moveOrder = new MoveOrder(this);
        
        isSearching = false;
        cancellationRequested = false;

        OnSearchComplete = () => {};
    }

    public void StartSearch(int maxDepth, Action? onSearchComplete = null)
    {
        List<Move> preSearchMoves = MoveGen.GenerateMoves(board);
        bestMove = preSearchMoves.Count == 0 ? Move.NullMove : preSearchMoves[0];

        isSearching = true;
        cancellationRequested = false;

        if (onSearchComplete != null)
        {
            OnSearchComplete += onSearchComplete;
        }

        // Return Null Move
        if (maxDepth <= 0)
        {
            EndSearch();
            return;
        }

        if (settings.useIterativeDeepening)
        {
            Task.Factory.StartNew(() => {
                // Iterative Deepening
                for (int depth = 1; depth <= maxDepth; depth++)
                {
                    int evalThisIteration = Search(depth, Infinity.negativeInfinity, Infinity.positiveInfinity, 0);

                    if (cancellationRequested)
                    {
                        break;
                    }

                    if (evaluation.IsMateScore(evalThisIteration))
                    {
                        break;
                    }
                }

                EndSearch();
            }, TaskCreationOptions.LongRunning);
        }
        else
        {
            Task.Factory.StartNew(() => {
                Search(maxDepth, Infinity.negativeInfinity, Infinity.positiveInfinity, 0);

                EndSearch();
            }, TaskCreationOptions.LongRunning);
        }
    }

    int Search(int depth, int alpha, int beta, int plyFromRoot)
    {
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
            return settings.useQSearch ? QuiescenceSearch(alpha, beta) : evaluation.Evaluate(board);
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

        moveOrder.GetOrderedList(legalMoves);

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
        int standPat = evaluation.Evaluate(board);

        if (standPat >= beta)
        {
            return beta;
        }
        if (alpha < standPat)
        {
            alpha = standPat;
        }

        List<Move> moves = MoveGen.GenerateMoves(board, true);
        moveOrder.GetOrderedList(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int eval = -QuiescenceSearch(-beta, -alpha);

            board.UnmakeMove(move);

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

    void EndSearch()
    {
        isSearching = false;
        cancellationRequested = false;
        
        OnSearchComplete?.Invoke();
        OnSearchComplete = () => {};
    }

    public void Update()
    {
        if (!isSearching)
        {
            
        }
    }

    public Move GetMove()
    {
        return bestMove;
    }

    public bool IsSearching()
    {
        return isSearching;
    }

    public TranspositionTable GetTT()
    {
        return tt;
    }

    public Board GetBoard()
    {
        return board;
    }
    public EngineSettings GetSettings()
    {
        return settings;
    }
    public Evaluation GetEvaluation()
    {
        return evaluation;
    }

    public void CancelSearch()
    {
        cancellationRequested = true;
    }








}