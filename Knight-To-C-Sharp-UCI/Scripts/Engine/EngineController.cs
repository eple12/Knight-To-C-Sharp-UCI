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