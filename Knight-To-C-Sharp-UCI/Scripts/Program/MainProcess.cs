public static class MainProcess
{
    public static Board board;
    public static Engine engine;

    public static void Start()
    {
        PreCalculate();

        
    }

    public static int Update()
    {
        int cmdResult = RecieveCommands();


        return cmdResult;
    }

    public static void Test()
    {
        engine.StartSearch(6);
        Move.PrintMove(engine.GetMove());
    }

    static int RecieveCommands()
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
        board = new Board();
        engine = new Engine(board);

        MoveOrder.Initialize(engine);
        Zobrist.GenerateZobristTable();
        PreComputedData.Initialize();
    }
}