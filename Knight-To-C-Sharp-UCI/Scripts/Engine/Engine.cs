
public class Engine
{
    // Materials
    Board board;
    MoveGenerator MoveGen;
    TranspositionTable tt;
    MoveOrder moveOrder;
    EngineSettings settings;
    Evaluation evaluation;

    Move bestMove;
    Move bestMoveLastIteration;
    int bestEval;

    // Search Constants
    const int MaxExtension = 16;

    bool isSearching;
    bool cancellationRequested;

    public event Action OnSearchComplete;

    public Engine(Board _board, EngineSettings _settings)
    {
        board = _board;
        MoveGen = board.MoveGen;
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
        Span<Move> preSearchMoves = MoveGen.GenerateMoves();
        bestMove = preSearchMoves.Length == 0 ? Move.NullMove : preSearchMoves[0];

        bestMoveLastIteration = Move.NullMove;

        bestEval = 0;

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

        // Try to find this position in the Opening Book
        Move bookMove = Book.GetRandomMove(board);
        if (!Move.IsNull(bookMove))
        {
            bestMove = bookMove;
            EndSearch();
            return;
        }

        if (settings.useIterativeDeepening)
        {
            Task.Factory.StartNew(() => {
                IterativeDeepening(maxDepth);
            }, TaskCreationOptions.LongRunning);
        }
        else
        {
            Task.Factory.StartNew(() => {
                Search(maxDepth, Infinity.NegativeInfinity, Infinity.PositiveInfinity, 0);

                EndSearch();
            }, TaskCreationOptions.LongRunning);
        }
    }

    void IterativeDeepening(int maxDepth)
    {
        // Iterative Deepening
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            int evalThisIteration = Search(depth, Infinity.NegativeInfinity, Infinity.PositiveInfinity, 0);
            
            bestMoveLastIteration = bestMove;
            Console.WriteLine($"depth {depth} move {Move.MoveString(bestMove)} eval {evalThisIteration}");
            
            if (cancellationRequested)
            {
                break;
            }

            // if (drawExit)
            // {
            //     break;
            // }

            if (evaluation.IsMateScore(evalThisIteration))
            {
                break;
            }
        }

        EndSearch();
    }

    int Search(int depth, int alpha, int beta, int plyFromRoot, int numExtensions = 0)
    {
        if (cancellationRequested) // Return if the search is cancelled
        {
            return 0;
        }

        if (plyFromRoot > 1) // Return 0 if drawn
        {
            if (board.FiftyRuleHalfClock >= 100 || board.PositionHistory[board.ZobristKey] > 1)
            {
                return 0;
            }

            // Skip this position if a mating sequence has already been found earlier in the search, which would be shorter
            // than any mate we could find from here. This is done by observing that alpha can't possibly be worse
            // (and likewise beta can't  possibly be better) than being mated in the current position.
            alpha = Math.Max(alpha, -Evaluation.CheckmateEval + plyFromRoot);
            beta = Math.Min(beta, Evaluation.CheckmateEval - plyFromRoot);
            if (alpha >= beta)
            {
                return alpha;
            }
        }
        

        // Try looking up the current position in the transposition table.
        // If the same position has already been searched to at least an equal depth
        // to the search we're doing now,we can just use the recorded evaluation.
        int ttVal = tt.LookupEvaluation (depth, plyFromRoot, alpha, beta);
        if (ttVal != TranspositionTable.lookupFailed)
        {
            Move ttMove = tt.GetStoredMove();

            if (plyFromRoot == 0) // Use the move stored in TT
            {
                if (evaluation.IsMateScore(ttVal)) // If the tt evaluation is a checkmate value, check if it is not a draw
                {
                    if (board.FiftyRuleHalfClock >= 100 || board.PositionHistory[board.ZobristKey] > 1)
                    {
                        return 0;
                    }
                }

                if (ttMove.moveValue != Move.NullMove.moveValue)
                {
                    // Console.WriteLine("tt move " + Move.MoveString(ttMove) + " eval " + ttVal + " depth " + depth);
                    bestMove = ttMove;
                    bestEval = ttVal;
                }
            }

            return ttVal;
        }

        if (depth == 0) // Return QSearch Evaluation
        {
            return settings.useQSearch ? QuiescenceSearch(alpha, beta) : evaluation.Evaluate();
        }

        Span<Move> legalMoves = stackalloc Move[256];
        MoveGen.GenerateMoves(ref legalMoves, genOnlyCaptures: false);

        // Checkmate, Stalemate, Draws
        MateChecker.MateState mateState = MateChecker.GetPositionState(board, legalMoves, ExcludeRepetition: true, ExcludeFifty: true);
        if (mateState != MateChecker.MateState.None)
        {
            if (mateState == MateChecker.MateState.Checkmate)
            {
                return -Evaluation.CheckmateEval + plyFromRoot;
            }
            return 0;
        }

        // Order Moves
        if (plyFromRoot == 0)
        {
            moveOrder.GetOrderedList(legalMoves, bestMoveLastIteration);
        }
        else
        {
            moveOrder.GetOrderedList(legalMoves);
        }

        int evalType = TranspositionTable.UpperBound;

        Move bestMoveInThisPosition = legalMoves[0];
        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);

            int extension = 0;

            // if (numExtensions < MaxExtension)
            // {
            //     if (board.MoveGen.InCheck())
            //     {
            //         extension = 1;
            //     }
            // }
                
            // else if (Piece.GetType(board.Squares[legalMoves[i].targetSquare]) == Piece.Pawn)
            // {
            //     int targetSquareRank = legalMoves[i].targetSquare / 8;
            //     if (targetSquareRank == 6 || targetSquareRank == 1)
            //     {
            //         extension = 1;
            //     }
            // }

            int eval = -Search(depth - 1 + extension, -beta, -alpha, plyFromRoot + 1, numExtensions + extension);

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
                bestMoveInThisPosition = legalMoves[i];
                evalType = TranspositionTable.Exact;
                
                if (plyFromRoot == 0)
                {
                    // Console.WriteLine("found better move: " + Move.MoveString(legalMoves[i]) + " eval: " + eval + " bestEval: " + bestEval);
                    bestMove = legalMoves[i];
                    bestEval = eval;
                }
            }
        }
        
        tt.StoreEvaluation (depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);

        return alpha;
    }

    int QuiescenceSearch(int alpha, int beta)
    {
        int standPat = evaluation.Evaluate();

        if (standPat >= beta)
        {
            return beta;
        }
        if (alpha < standPat)
        {
            alpha = standPat;
        }

        Span<Move> moves = stackalloc Move[256];
        MoveGen.GenerateMoves(ref moves, genOnlyCaptures: true);
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
        Console.WriteLine($"debug info eval: {bestEval}");

        OnSearchComplete?.Invoke();
        OnSearchComplete = () => {};
        
        isSearching = false;
        cancellationRequested = false;
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

    public void CancelSearch(Action? onSearchComplete = null)
    {
        if (onSearchComplete != null)
        {
            OnSearchComplete += onSearchComplete;
        }
        cancellationRequested = true;
    }








}