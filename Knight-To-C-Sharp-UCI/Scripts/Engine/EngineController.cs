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
        engine.StartSearch(depth, onSearchComplete);
    }

    public void CancelSearch()
    {
        engine.CancelSearch();
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