public static class MainProcess
{
    public static Board board = new Board();
    public static Bot engine = new Bot(board);

    public static void Start()
    {
        PreCalculate();
    }

    public static int CommandUpdate()
    {
        return RecieveCommands();
    }

    public static int RecieveCommands()
    {
        string? command = Console.ReadLine();

        if (command == null)
        {
            return 0;
        }
        
        return Command.RecieveCommand(command);
    }

    static void PreCalculate()
    {
        Zobrist.GenerateZobristTable();
        // PreComputedData.Initialize();
        
        board.LoadPositionFromFen(Board.InitialFen);

        Book.GenerateTable();
        
    }
}