public class EngineController
{
    EngineSettings settings;
    Engine engine;

    public EngineController(Board _board)
    {
        settings = new EngineSettings();
        engine = new Engine(_board, settings);
    }

    public void StartSearch(int depth, Action? onSearchComplete = null)
    {
        onSearchComplete += () => {
            Console.WriteLine("bestmove " + Move.MoveString(MainProcess.engine.GetMove()));
        };
        engine.StartSearch(depth, onSearchComplete);
    }
    public void StartTimedSearch(int depth, int timeMS, Action? onSearchComplete = null)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Task.Factory.StartNew(() => {Thread.Sleep(timeMS);}, cts.Token)
        .ContinueWith((t) => {
            CancelAndWait();
        });

        onSearchComplete += () => {
            cts.Cancel();
            cts.Dispose();
        };

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

    public void CancelSearch(Action? onSearchComplete = null)
    {
        if (IsSearching())
        {
            engine.CancelSearch(onSearchComplete);
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
        return engine.GetMove();
    }
    public bool IsSearching()
    {
        return engine.IsSearching();
    }
    public Engine GetEngine()
    {
        return engine;
    }



}