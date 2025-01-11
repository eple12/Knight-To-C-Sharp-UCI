public static class MainProcess
{
    public static Board board = new();
    public static Bot engine = new(board);

    static MainProcess() {
        Zobrist.GenerateZobristTable();
        
        board.LoadInitialPosition();

        Book.GenerateTable();
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
}