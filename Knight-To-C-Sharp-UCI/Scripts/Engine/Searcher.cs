
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
    const int AspirationWindowMinDepth = 8;
    const int AspirationWindowBase = 20;
    const int MaxExtension = 16;
    const int Reduction = 1;

    // Search Flags
    bool searchRequested;
    bool isSearching;
    bool cancellationRequested;
    // bool searchedFirstLine;


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
            Console.WriteLine("WARNING: The current search is not complete. Search request failed.");
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
        moveOrder.ClearKillers();

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
    }

    void IterativeDeepening(int maxDepth)
    {
        int alpha = Infinity.NegativeInfinity;
        int beta = Infinity.PositiveInfinity;

        int lastSearchEval = Infinity.NegativeInfinity;

        // Iterative Deepening
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            if (depth < AspirationWindowMinDepth || lastSearchEval == Infinity.NegativeInfinity)
            {
                Search(depth, alpha, beta, 0, ref BestPV);
            }
            else // Aspiration Windows Search, Inspired by Lynx-Bot (https://github.com/lynx-chess/Lynx)
            {
                // Aspiration Window
                int window = AspirationWindowBase;
                // Temporary reduction for fail-highs
                int failHighReduction = 0;
                
                int numFailHighs = 0;
                int numFailLows = 0;

                alpha = Math.Max(Infinity.NegativeInfinity, lastSearchEval - window);
                beta = Math.Min(Infinity.PositiveInfinity, lastSearchEval + window);

                while (true) // Gradient Widening
                {
                    if (cancellationRequested)
                    {
                        break;
                    }

                    Search(depth - failHighReduction, alpha, beta, 0, ref BestPV);

                    window += window >> 1; // Adds window / 2

                    if (alpha >= bestEval) // Fail Low
                    {
                        alpha = Math.Max(Infinity.NegativeInfinity, bestEval - window);
                        beta = (alpha + beta) >> 1; // (alpha + beta) / 2
                        failHighReduction = 0;
                        numFailLows++;
                    }
                    else if (beta <= bestEval)
                    {
                        beta = Math.Min(Infinity.PositiveInfinity, bestEval + window);
                        ++failHighReduction;
                        numFailHighs++;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // Console.WriteLine($"info string Aspiration Windows at depth {depth} fail-low {numFailLows} fail-high {numFailHighs}");
            }

            // if (cancellationRequested)
            // {
            //     bestEval = lastSearchEval;
            //     bestMove = bestMoveLastIteration;
            //     break;
            // }

            lastSearchEval = bestEval;

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

            if (evaluation.IsMateScore(bestEval) && (evaluation.MateInPly(bestEval) <= depth))
            {
                break;
            }
        }

        EndSearch();
    }

    // Main NegaMax Search Function
    int Search(int depth, int alpha, int beta, int plyFromRoot, ref PvLine pLine)
    {
        numNodeSearched++;
        PvLine line = new();

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
                if (ttMove.moveValue != Move.NullMove.moveValue)
                {
                    bestMove = ttMove;
                    bestEval = ttVal;
                }
            }

            return ttVal;
        }

        if (depth == 0) // Return QSearch Evaluation
        {
            pLine.CMove = 0;
            return QuiescenceSearch(alpha, beta);
        }

        // bool isPv = beta - alpha > 1;

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
        Move prevBestMove = plyFromRoot == 0 ? (Move.IsNull(bestMoveLastIteration) ? tt.GetStoredMove() : bestMoveLastIteration) : tt.GetStoredMove();

        SEE.SEEPinData pinData = new();
        pinData.Calculate(board);
        moves = moveOrder.GetOrderedList(ref moves, prevBestMove, inQSearch: false, plyFromRoot, pinData);

        int evalType = TranspositionTable.UpperBound;

        Move bestMoveInThisPosition = moves[0];

        // Moves Loop
        for (int i = 0; i < moves.Length; i++)
        {
            bool isCapture = board.Squares[moves[i].targetSquare] != Piece.None;

            board.MakeMove(moves[i]);

            // Search Extensions
            int extension = 0;
            // if (numExtensions < MaxExtension)

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

            // Late Move Reduction
            // Reverted changes due to bugs
            // bool needFullSearch = true;
            int eval = 0;

            int reduction = 0;

            if (extension == 0 && depth >= 3 && i >= 3)
            {
                // int reduction = 0;

                int moveScore = moveOrder.GetLastMoveScores()[i];
                if (!isCapture)
                {
                    reduction = 1;
                }
                else if (moveScore >= MoveOrder.BadCaptureBaseScore && moveScore < MoveOrder.PromotionMoveScore)
                {
                    reduction = 1;
                }
                
                // eval = -Search(depth - 1 - reduction, -alpha - 1, -alpha, plyFromRoot + 1, ref line);
                // needFullSearch = eval > alpha;
            }
            // if (needFullSearch)
            // {
            //     eval = -Search(depth - 1 + extension, -beta, -alpha, plyFromRoot + 1, ref line);
            // }

            eval = -Search(depth - 1 - reduction, -alpha - 1, -alpha, plyFromRoot + 1, ref line);

            if (eval > alpha && reduction > 0) {
                eval = -Search(depth - 1, -alpha - 1, -alpha, plyFromRoot + 1, ref line);
            }

            if (eval > alpha && eval < beta) {
                eval = -Search(depth - 1, -beta, -alpha, plyFromRoot + 1, ref line);
            }

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
                if (!isCapture)
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
        numNodeSearched++;
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

        SEE.SEEPinData pinData = new();
        pinData.Calculate(board);
        moves = moveOrder.GetOrderedList(ref moves, Move.NullMove, inQSearch: true, 0, pinData);

        for (int i = 0; i < moves.Length; i++)
        {
            int moveScore = moveOrder.GetLastMoveScores()[i];

            // QSearch SEE Pruning
            // if (moveScore < MoveOrder.PromotionMoveScore && moveScore >= MoveOrder.BadCaptureBaseScore)
            // {
            //     continue;
            // }

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