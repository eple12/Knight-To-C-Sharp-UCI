
using System.Diagnostics;

public class Searcher
{
    // Engine Classes
    Board board;
    MoveGenerator MoveGen;
    TranspositionTable tt;
    MoveOrder moveOrder;
    EngineSettings settings;
    Evaluation evaluation;

    // Search Info
    Move bestMove;
    Move bestMoveLastIteration;
    int bestEval;
    ulong numNodeSearched;
    SearchRequestInfo searchRequestInfo;
    Stopwatch searchTimeTimer;
    PvLine BestPV;

    // Search Constants
    const int MaxExtension = 16;
    const int Reduction = 1;

    // Search Flags
    bool searchRequested;
    bool isSearching;
    bool cancellationRequested;

    // int numQs;

    public event Action OnSearchComplete;

    public Searcher(Board _board, EngineSettings _settings)
    {
        board = _board;
        MoveGen = board.MoveGen;
        settings = _settings;

        evaluation = new Evaluation(this);
        tt = new TranspositionTable(this);

        moveOrder = new MoveOrder(this);
        
        searchRequested = false;
        isSearching = false;
        cancellationRequested = false;

        OnSearchComplete = () => {};


        Task.Factory.StartNew(SearchThread, TaskCreationOptions.LongRunning);

        searchTimeTimer = new();
    }

    // Set the best move to the book move that has been already found
    public void SetBookMove(Move bookMove)
    {
        bestMove = bookMove;
    }
    
    public void RequestSearch(int maxDepth, Action? onSearchComplete = null)
    {
        if (isSearching)
        {
            Console.WriteLine("The current search is not complete. Search request failed.");
            return;
        }
        
        searchRequestInfo = new SearchRequestInfo(maxDepth, onSearchComplete);
        searchRequested = true;
    }

    void StartSearch(int maxDepth, Action? onSearchComplete = null)
    {
        searchRequested = false;

        Span<Move> preSearchMoves = MoveGen.GenerateMoves();
        bestMove = preSearchMoves.Length == 0 ? Move.NullMove : preSearchMoves[0];

        bestMoveLastIteration = Move.NullMove;
        moveOrder.ClearHistory();

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

        BestPV = new();

        searchTimeTimer.Restart();
        numNodeSearched = 0;

        IterativeDeepening(maxDepth);
        // if (settings.useIterativeDeepening)
        // {
        //     IterativeDeepening(maxDepth);
        // }
        // else
        // {
        //     Search(maxDepth, Infinity.NegativeInfinity, Infinity.PositiveInfinity, 0);

        //     EndSearch();
        // }
    }

    void IterativeDeepening(int maxDepth)
    {
        // Iterative Deepening
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            Search(depth, Infinity.NegativeInfinity, Infinity.PositiveInfinity, 0, 0, ref BestPV);
            
            bestMoveLastIteration = bestMove;

            bool isMate = evaluation.IsMateScore(bestEval);
            int matePly = isMate ? evaluation.MateInPly(bestEval) : 0;

            // PV Line
            string pvLine = string.Empty;
            if (BestPV.CMove <= 0)
            {
                pvLine += Move.MoveString(bestMove) + ' ';
            }
            else
            {
                for (int i = 0; i < BestPV.CMove; i++)
                {
                    pvLine += Move.MoveString(BestPV.ArgMoves[i]) + ' ';
                }
            }

            Console.WriteLine($"info depth {depth} score {(!isMate ? $"cp {bestEval}" : $"mate {(bestEval > 0 ? (matePly + 1) / 2 : -matePly / 2)}")} nodes {numNodeSearched} nps {numNodeSearched * 1000 / (ulong) (searchTimeTimer.ElapsedMilliseconds != 0 ? searchTimeTimer.ElapsedMilliseconds : 1)} time {searchTimeTimer.ElapsedMilliseconds} pv {pvLine}multipv 1");
            
            if (cancellationRequested)
            {
                break;
            }

            // if (drawExit)
            // {
            //     break;
            // }

            if (evaluation.IsMateScore(bestEval) && (evaluation.MateInPly(bestEval) <= depth))
            {
                break;
            }
        }

        EndSearch();
    }

    int Search(int depth, int alpha, int beta, int plyFromRoot, int numExtensions, ref PvLine pLine)
    {
        numNodeSearched++;
        PvLine line = new();

        // Console.WriteLine($"Search Head Depth {depth}");
        if (cancellationRequested) // Return if the search is cancelled
        {
            pLine.CMove = 0;
            return 0;
        }

        if (plyFromRoot > 0) // Return 0 if drawn
        {
            if (board.FiftyRuleHalfClock >= 100 || board.PositionHistory[board.ZobristKey] > 1)
            {
                pLine.CMove = 0;
                return 0;
            }

            // Skip this position if a mating sequence has already been found earlier in the search, which would be shorter
            // than any mate we could find from here. This is done by observing that alpha can't possibly be worse
            // (and likewise beta can't  possibly be better) than being mated in the current position.
            alpha = Math.Max(alpha, -Evaluation.CheckmateEval + plyFromRoot);
            beta = Math.Min(beta, Evaluation.CheckmateEval - plyFromRoot);
            if (alpha >= beta)
            {
                pLine.CMove = 0;
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
            pLine.CMove = 0;

            if (plyFromRoot == 0) // Use the move stored in TT
            {
                // if (evaluation.IsMateScore(ttVal)) // If the tt evaluation is a checkmate value, check if it is not a draw
                // {
                //     if (board.FiftyRuleHalfClock >= 100 || board.RepetitionVerify.Contains(board.ZobristKey))
                //     {
                //         return 0;
                //     }
                // }

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
            // return evaluation.Evaluate();
            // board.PrintSmallBoard();
            pLine.CMove = 0;
            return QuiescenceSearch(alpha, beta);
        }

        Span<Move> moves = stackalloc Move[256];
        MoveGen.GenerateMoves(ref moves, genOnlyCaptures: false);

        // Checkmate, Stalemate, Draws
        MateChecker.MateState mateState = MateChecker.GetPositionState(board, moves, ExcludeRepetition: true, ExcludeFifty: true);
        if (mateState != MateChecker.MateState.None)
        {
            pLine.CMove = 0;
            if (mateState == MateChecker.MateState.Checkmate)
            {
                return -Evaluation.CheckmateEval + plyFromRoot;
            }
            return 0;
        }

        // Order Moves
        Move prevBestMove = plyFromRoot == 0 ? bestMoveLastIteration : tt.GetStoredMove();
        moveOrder.GetOrderedList(moves, bestMoveLastIteration, inQSearch: false, plyFromRoot);

        int evalType = TranspositionTable.UpperBound;

        Move bestMoveInThisPosition = moves[0];
        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);

            int extension = 0;

            if (numExtensions < MaxExtension)
            {
                if (board.InCheck())
                {
                    extension = 1;
                }
                else
                {
                    int targetSquare = moves[i].targetSquare;
                    if (Piece.GetType(board.Squares[targetSquare]) == Piece.Pawn && (targetSquare / 8 == 1 || targetSquare / 8 == 6))
                    {
                        extension = 1;
                    }
                }
            }

            bool needFullSearch = true;
            int eval = 0;

            // Console.WriteLine($"current Depth {depth} ext {extension} totalExt {numExtensions}");

            if (extension == 0 && depth >= 3 && i >= 3 && board.Squares[moves[i].targetSquare] != Piece.None)
            {
                eval = -Search(depth - 1 - Reduction, -alpha - 1, -alpha, plyFromRoot + 1, numExtensions, ref line);
                needFullSearch = eval > alpha;
            }
            if (needFullSearch)
            {
                eval = -Search(depth - 1 + extension, -beta, -alpha, plyFromRoot + 1, numExtensions + extension, ref line);
            }
            // int eval = -Search(depth - 1 + extension, -beta, -alpha, plyFromRoot + 1, numExtensions + extension);

            board.UnmakeMove(moves[i]);

            if (cancellationRequested)
            {
                return 0;
            }

            if (eval >= beta)
            {
                tt.StoreEvaluation (depth, plyFromRoot, beta, TranspositionTable.LowerBound, moves[i]);

                // Killer Moves, History
                // If this move is not a capture
                // Does not store captures since they are ranked highly in Move Ordering anyway
                if (board.Squares[moves[i].targetSquare] == Piece.None)
                {
                    if (plyFromRoot < MoveOrder.MaxKillerPly)
                    {
                        moveOrder.KillerMoves[plyFromRoot].Add(moves[i]);
                    }
                    int historyScore = depth * depth;
                    moveOrder.History[board.Turn ? 0 : 1, moves[i].startSquare, moves[i].targetSquare] += historyScore;
                }

                return beta;
            }

            if (eval > alpha)
            {
                alpha = eval;
                bestMoveInThisPosition = moves[i];
                evalType = TranspositionTable.Exact;

                pLine.ArgMoves[0] = moves[i];
                Array.Copy(line.ArgMoves, 0, pLine.ArgMoves, 1, line.CMove);
                pLine.CMove = line.CMove + 1;
                
                if (plyFromRoot == 0)
                {
                    // Console.WriteLine("found better move: " + Move.MoveString(legalMoves[i]) + " eval: " + eval + " bestEval: " + bestEval);
                    bestMove = moves[i];
                    bestEval = eval;
                }
            }
        }
        
        tt.StoreEvaluation (depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);

        return alpha;
    }

    int QuiescenceSearch(int alpha, int beta)
    {
        // numQs++;
        int eval = evaluation.Evaluate();

        if (eval >= beta)
        {
            return beta;
        }
        if (alpha < eval)
        {
            alpha = eval;
        }

        Span<Move> moves = stackalloc Move[128];
        MoveGen.GenerateMoves(ref moves, genOnlyCaptures: true);
        moveOrder.GetOrderedList(moves, Move.NullMove, inQSearch: true, 0);
        // Console.WriteLine(moves.Length);

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);

            eval = -QuiescenceSearch(-beta, -alpha);

            board.UnmakeMove(moves[i]);

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
        searchTimeTimer.Stop();
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






    void SearchThread()
    {
        while (true)
        {
            if (searchRequested)
            {
                StartSearch(searchRequestInfo.MaxDepth, searchRequestInfo.OnSearchComplete);
            }
        }
    }

    struct PvLine
    {
        public const int MaxPvMoves = 100;

        public int CMove;
        public Move[] ArgMoves;

        public PvLine()
        {
            CMove = 0;
            ArgMoves = new Move[MaxPvMoves];
        }
    }

    struct SearchRequestInfo
    {
        public int MaxDepth;
        public Action? OnSearchComplete;

        public SearchRequestInfo(int maxDepth, Action? onSearchComplete)
        {
            MaxDepth = maxDepth;
            OnSearchComplete = onSearchComplete;
        }
    }

}