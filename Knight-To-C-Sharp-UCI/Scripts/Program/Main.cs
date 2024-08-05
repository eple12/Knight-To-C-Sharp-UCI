public static class MainProcess
{
    public static Board board;

    public static void Start()
    {
        PreCalculate();

        board = new Board();
        board.LoadPositionFromFen(board.loadFen);
        board.PrintLargeBoard();
        board.PrintSmallBoard();
    }

    public static int Update()
    {
        int cmdResult = RecieveCommands();


        return cmdResult;
    }

    static int RecieveCommands()
    {
        string command = "";
        command = Console.ReadLine();

        if (command == "quit" || command == "stop")
        {
            return 1;
        }

        return 0;
    }

    static void PreCalculate()
    {
        PreComputedData.Initialize();
    }
}