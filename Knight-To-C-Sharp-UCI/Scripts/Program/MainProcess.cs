public static class MainProcess
{
    public static Board board = new Board();
    public static EngineController engine = new EngineController(board);

    public static void Start()
    {
        PreCalculate();
    }

    public static int CommandUpdate()
    {
        int cmdResult = RecieveCommands();

        return cmdResult;
    }

    public static int RecieveCommands()
    {
        string? command = Console.ReadLine();

        if (command == null)
        {
            return 0;
        }

        int commandResult = Command.RecieveCommand(command);

        return commandResult;
    }

    static void PreCalculate()
    {
        Zobrist.GenerateZobristTable();
        PreComputedData.Initialize();
        
        board.LoadPositionFromFen(Board.initialFen);
    }
}