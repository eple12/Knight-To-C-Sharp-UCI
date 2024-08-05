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



        return 0;
    }

    static void PreCalculate()
    {
        PreComputedData.Initialize();
    }
}