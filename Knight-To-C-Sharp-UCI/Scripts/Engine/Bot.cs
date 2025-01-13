public class Bot
{
    // References
    Searcher searcher;
    Board board;

    public Bot(Board _board)
    {
        searcher = new Searcher(_board);
        board = _board;
    }

    public void StartSearch(int depth, Action? onSearchComplete = null)
    {
        if (TryGetBookMove(onSearchComplete))
        {
            return;
        }

        // Best move report
        onSearchComplete += () => {
            ReportBestMove(GetMove());
        };
        searcher.RequestSearch(depth, onSearchComplete);
    }

    public void StartTimedSearch(int depth, int timeMS, Action? onSearchComplete = null)
    {
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
            depth = Configuration.MaxDepth;
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
    
    // Returns true if a book move was found
    bool TryGetBookMove(Action? onSearchComplete)
    {
        // Try to find this position in the Opening Book
        Move bookMove = Book.GetRandomMove(board);

        if (!bookMove.IsNull())
        {
            searcher.SetBookMove(bookMove);
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
            searcher.CancelSearch(onSearchComplete);
        }
    }
    
    public void CancelAndWait()
    {
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
        return searcher.GetMove();
    }
    public bool IsSearching()
    {
        return searcher.IsSearching();
    }
    public Searcher GetSearcher()
    {
        return searcher;
    }

    public void ReportBestMove(Move move)
    {
        Console.WriteLine($"bestmove {move.San}");
    }
}