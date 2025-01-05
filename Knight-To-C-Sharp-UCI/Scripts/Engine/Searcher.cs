
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class Searcher
{
    // Engine Classes
    Board board;
    MoveGenerator MoveGen;
    TranspositionTable tt;
    MoveOrder moveOrder;
    // EngineSettings settings;
    Evaluation evaluation;
    Repetition repetition;

    // Search Info
    Move bestMove;
    Move bestMoveLastIteration;
    int bestEval;
    ulong numNodeSearched;
    SearchRequestInfo searchRequestInfo;
    Stopwatch searchTimeTimer;
    // PvLine BestPV;
    PvTable pvTable;

    // Search Flags
    bool searchRequested;
    bool isSearching;
    bool cancellationRequested;
    // bool searchedFirstLine;


    public event Action OnSearchComplete;

    public Searcher(Board _board)
    {
        board = _board;
        MoveGen = board.MoveGen;
        // settings = _settings;

        evaluation = new Evaluation(this);
        tt = new TranspositionTable(this);

        // RepetitionData = new();
        repetition = new(board);
        pvTable = new();

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

        repetition.Clear();

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

        // BestPV = new();
        // pvTable = new();
        pvTable.ClearAll();

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
            if (depth < Configuration.AspirationWindowMinDepth || lastSearchEval == Infinity.NegativeInfinity)
            {
                Search(depth, alpha, beta, 0);
            }
            else // Aspiration Windows Search, Inspired by Lynx-Bot (https://github.com/lynx-chess/Lynx)
            {
                // Aspiration Window
                int window = Configuration.AspirationWindowBase;
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

                    int eval = Search(depth - failHighReduction, alpha, beta, 0);

                    window += window >> 1; // Adds window / 2

                    if (alpha >= eval) // Fail Low
                    {
                        alpha = Math.Max(Infinity.NegativeInfinity, eval - window);
                        beta = (alpha + beta) >> 1; // (alpha + beta) / 2
                        failHighReduction = 0;
                        numFailLows++;
                    }
                    else if (beta <= eval)
                    {
                        beta = Math.Min(Infinity.PositiveInfinity, eval + window);
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
            string pvLine = pvTable.GetRootString();
            pvLine = string.IsNullOrEmpty(pvLine) ? bestMove.San : pvLine;
            // pvLine = bestMove.San + ' ';
            // if (BestPV.CMove <= 0)
            // {
            //     pvLine += bestMove.San + ' ';
            // }
            // else
            // {
            //     for (int i = 0; i < BestPV.CMove; i++)
            //     {
            //         pvLine += BestPV.ArgMoves[i].San + ' ';
            //     }
            // }

            Console.WriteLine($"info depth {depth} score {(!isMate ? $"cp {bestEval}" : $"mate {(bestEval > 0 ? (matePly + 1) / 2 : -matePly / 2)}")} nodes {numNodeSearched} nps {numNodeSearched * 1000 / (ulong) (searchTimeTimer.ElapsedMilliseconds != 0 ? searchTimeTimer.ElapsedMilliseconds : 1)} time {searchTimeTimer.ElapsedMilliseconds} pv {pvLine} multipv 1");

            pvTable.ClearAll();
            
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
    int Search(int depth, int alpha, int beta, int ply)
    {
        // bool debug = board.ZobristKey == 10809052421590594767;
        // if (board.ZobristKey == 3698178750396369094 && depth >= 4) {

        // }
        // if (board.ZobristKey == 10529439860043044384 && depth >= 2) {
            
        // }

        numNodeSearched++;

        // Reached the max depth
        if (ply >= Configuration.MaxDepth - 1) {
            return evaluation.Evaluate();
        }

        // PvLine line = new();

        if (cancellationRequested) // Return if the search is cancelled
        {
            // Console.WriteLine($"info string cancellation at ply {ply}");
            return 0;
        }

        bool isRoot = ply == 0;
        bool isPv = beta - alpha > 1;

        // Pv info
        int pvIndex = PvTable.Indexes[ply];
        int nextPvIndex = PvTable.Indexes[ply + 1];
        pvTable[pvIndex] = Move.NullMove;

        if (!isRoot) // Return 0 if drawn
        {
            if (board.FiftyRuleHalfClock >= 100 || repetition.IsThreeFold())
            {
                // Console.WriteLine($"info string draw at ply {ply}");
                pvTable.ClearFrom(nextPvIndex);
                return 0;
            }

            // Skip this position if a mating sequence has already been found earlier in the search, which would be shorter
            // than any mate we could find from here. This is done by observing that alpha can't possibly be worse
            // (and likewise beta can't  possibly be better) than being mated in the current position.
            // alpha = Math.Max(alpha, -Evaluation.CheckmateEval + ply);
            // beta = Math.Min(beta, Evaluation.CheckmateEval - ply);
            // if (alpha >= beta)
            // {
            //     pLine.CMove = 0;
            //     return alpha;
            // }
        }

        // Try looking up the current position in the transposition table.
        // If the same position has already been searched to at least an equal depth
        // to the search we're doing now,we can just use the recorded evaluation.
        Move ttMove = Move.NullMove;
        if (!isRoot)
        {
            int ttVal = tt.LookupEvaluation (depth, ply, alpha, beta);
            if (ttVal != TranspositionTable.lookupFailed)
            {
                ttMove = tt.GetStoredMove();
                // Console.WriteLine($"info string ttVal at ply {ply}");

                if (!isPv) {
                    return ttVal;
                }
            }
        }

        if (depth == 0) // Return QSearch Evaluation
        {
            // Console.WriteLine($"info string qSearch at ply {ply}");
            return QuiescenceSearch(alpha, beta);
        }

        // bool isPv = beta - alpha > 1;

        Span<Move> moves = stackalloc Move[256];
        MoveGen.GenerateMoves(ref moves, genOnlyCaptures: false);

        // Checkmate, Stalemate, Draws
        MateChecker.MateState mateState = MateChecker.GetPositionState(board, moves, ExcludeRepetition: true, ExcludeFifty: true);
        if (mateState != MateChecker.MateState.None)
        {
            pvTable.ClearFrom(nextPvIndex);
            // pLine.CMove = 0;

            if (mateState == MateChecker.MateState.Checkmate)
            {
                if (isPv) {
                    
                }
                return -Evaluation.CheckmateEval + ply;
            }
            
            return 0;
        }

        // Order Moves
        Move prevBestMove = isRoot ? bestMoveLastIteration : ttMove;

        SEE.SEEPinData pinData = new();
        pinData.Calculate(board);
        int[] moveScores = new int[moves.Length];
        moves = moveOrder.GetOrderedList(ref moves, prevBestMove, inQSearch: false, ply, pinData, moveScores);

        int evalType = TranspositionTable.Alpha;

        bool isInCheck = board.InCheck();
        if (isInCheck) {
            ++depth;
        }

        Move bestMoveInThisPosition = moves[0];

        // Moves Loop
        for (int i = 0; i < moves.Length; i++)
        {
            bool isCapture = board.Squares[moves[i].targetSquare] != Piece.None;

            board.MakeMove(moves[i], inSearch: true);

            repetition.Push();

            // Late Move Reduction
            int eval = 0;

            int reduction = 0;
            bool givesCheck = board.InCheck();

            // Late Move Reduction (LMR)
            if (
                depth >= Configuration.LMR_MinDepth && 
                i >= (isPv ? Configuration.LMR_MinFullSearchedMoves : Configuration.LMR_MinFullSearchedMoves - 1) &&
                !isCapture
            )
            {
                reduction = Configuration.LMR_Reductions[depth][i];
                
                if (isPv) {
                    --reduction;
                }

                if (givesCheck) {
                    --reduction;
                }

                reduction = Math.Clamp(reduction, 0, depth - 1);
            }

            // SEE Reduction
            if (
                !isInCheck &&
                moveScores[i] < MoveOrder.PromotionMoveScore &&
                moveScores[i] >= MoveOrder.BadCaptureBaseScore
            )
            {
                reduction += Configuration.SEE_BadCaptureReduction;
                reduction = Math.Clamp(reduction, 0, depth - 1);
            }

            eval = -Search(depth - 1 - reduction, -alpha - 1, -alpha, ply + 1);

            if (eval > alpha && reduction > 0) {
                eval = -Search(depth - 1, -alpha - 1, -alpha, ply + 1);
            }

            if (eval > alpha && eval < beta) {
                eval = -Search(depth - 1, -beta, -alpha, ply + 1);
            }

            repetition.Pop();

            board.UnmakeMove(moves[i]);
            
            // if (debug) {
            //     Console.WriteLine($"Debug position at depth {depth}: Move {Move.MoveString(moves[i])} => a {alpha} b {beta} eval {eval}");
            // }

            if (cancellationRequested)
            {
                return 0;
            }

            if (eval >= beta)
            {
                tt.StoreEvaluation (depth, ply, beta, TranspositionTable.Beta, moves[i]);

                // Killer Moves, History
                // If this move is not a capture
                // Does not store captures since they are ranked highly in Move Ordering anyway
                if (!isCapture)
                {
                    if (ply < MoveOrder.MaxKillerPly)
                    {
                        moveOrder.KillerMoves[ply].Add(moves[i]);
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

                // pLine.ArgMoves[0] = moves[i];
                // Array.Copy(line.ArgMoves, 0, pLine.ArgMoves, 1, line.CMove);
                // pLine.CMove = line.CMove + 1;
                if (isPv) {
                    pvTable[pvIndex] = moves[i];
                    pvTable.CopyFrom(pvIndex + 1, nextPvIndex, Configuration.MaxDepth - ply - 1);
                }
                
                if (isRoot)
                {
                    // Console.WriteLine($"Root Alpha Update: {moves[i].San}");
                    bestMove = moves[i];
                    bestEval = eval;
                }
            }
        }
        
        tt.StoreEvaluation (depth, ply, alpha, evalType, bestMoveInThisPosition);

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
        int[] moveScores = new int[moves.Length];
        moves = moveOrder.GetOrderedList(ref moves, Move.NullMove, inQSearch: true, 0, pinData, moveScores);

        for (int i = 0; i < moves.Length; i++)
        {
            // int moveScore = moveOrder.GetLastMoveScores()[i];

            board.MakeMove(moves[i], inSearch: true);

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
    // public EngineSettings GetSettings()
    // {
    //     return settings;
    // }
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