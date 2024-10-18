public class Bot
{
    EngineSettings settings;
    Searcher engine;
    Board board;

    public Bot(Board _board)
    {
        settings = new EngineSettings();
        engine = new Searcher(_board, settings);
        board = _board;
    }

    public void StartSearch(int depth, Action? onSearchComplete = null)
    {
        if (TryToGetBookMove(onSearchComplete))
        {
            return;
        }

        // Best move report
        onSearchComplete += () => {
            ReportBestMove(GetMove());
        };
        engine.RequestSearch(depth, onSearchComplete);
    }
    public void StartTimedSearch(int depth, int timeMS, Action? onSearchComplete = null)
    {
        // Console.WriteLine("starttimed");
        // if (TryToGetBookMove(onSearchComplete))
        // {
        //     return;
        // }

        if (timeMS > 0)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Task.Delay(timeMS, cts.Token)
            .ContinueWith((t) => {
                CancelAndWait();
            });

            onSearchComplete += () => {
                cts.Cancel();
                cts.Dispose();
            };
        }

        if (depth <= 0)
        {
            depth = settings.unlimitedMaxDepth;
        }

        StartSearch(depth, onSearchComplete);
    }
    public int DecideThinkTime(int wtime, int btime, int winc, int binc, int max, int min)
    {
        // Think Time
        int myTime = MainProcess.board.Turn ? wtime : btime;
        int myInc = MainProcess.board.Turn ? winc : binc;
        // Get a fraction of remaining time to use for current move
        double thinkTimeDouble = myTime / 30.0;
        // Clamp think time if a maximum limit is imposed
        thinkTimeDouble = Math.Min(max, thinkTimeDouble);
        // Add increment
        if (myTime > myInc * 2)
        {
            thinkTimeDouble += myInc * 0.6;
        }

        double minThinkTime = Math.Min(min, myTime * 0.25);
        thinkTimeDouble = Math.Ceiling(Math.Max(minThinkTime, thinkTimeDouble));

        return (int) thinkTimeDouble;
    }
    
    // Returns if it found a book move or not
    bool TryToGetBookMove(Action? onSearchComplete)
    {
        // Console.WriteLine("book try");

        // Try to find this position in the Opening Book
        Move bookMove = Book.GetRandomMove(board);

        if (!Move.IsNull(bookMove))
        {
            engine.SetBookMove(bookMove);
            ReportBestMove(bookMove);
            
            onSearchComplete?.Invoke();

            return true;
        }

        return false;
    }

    public void CancelSearch(Action? onSearchComplete = null)
    {
        if (IsSearching())
        {
            engine.CancelSearch(onSearchComplete);
        }
    }
    public void CancelAndWait()
    {
        // Console.WriteLine("Cancel and Wait");

        if (!IsSearching())
        {
            return;
        }

        CancelSearch();
        while (IsSearching())
        {

        }
    }

    public Move GetMove()
    {
        return engine.GetMove();
    }
    public bool IsSearching()
    {
        return engine.IsSearching();
    }
    public Searcher GetEngine()
    {
        return engine;
    }

    public void ReportBestMove(Move move)
    {
        Console.WriteLine("bestmove " + Move.MoveString(move));
    }

}