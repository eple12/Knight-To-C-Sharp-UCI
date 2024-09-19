
public static class PerftHelper
{
    static PerftPosition[] PerftPositions;

    static PerftHelper()
    {
        PerftPositions = [
            new PerftPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 
            [20, 400, 8902, 197281, 4865609, 119060324]),
            new PerftPosition("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -",
            [48, 2039, 97862, 4085603, 193690690, 8031647685]),
            new PerftPosition("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w -",
            [14, 191, 2812, 43238, 674624, 11030083, 178633661, 3009794393]),
            new PerftPosition("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1",
            [6, 264, 9467, 422333, 15833292, 706045033]),
            new PerftPosition("r2q1rk1/pP1p2pp/Q4n2/bbp1p3/Np6/1B3NBn/pPPP1PPP/R3K2R b KQ - 0 1",
            [6, 264, 9467, 422333, 15833292, 706045033]),
            new PerftPosition("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8",
            [44, 1486, 62379, 2103487, 89941194]),
            new PerftPosition("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10",
            [46, 2079, 89890, 3894594, 164075551, 6923051137])
        ];
    }

    public static void Go(Board board, bool qSearch = false)
    {
        Console.WriteLine("##############################");
        Console.WriteLine("Perft Tests\n");

        // Move Generation Speed Test: Depth 5
        foreach (PerftPosition position in PerftPositions)
        {
            Case(board, position.Get(5), qSearch: false);
        }
        // Case(board, PerftPositions[1].Get(5), print: true);
        // Case(board, new PerftCase("8/2p5/3p4/KP5r/1R2Pp1k/8/6P1/8 b - - 0 1", 1, 0));

        board.LoadPositionFromFen(Board.InitialFen);
        Console.WriteLine("##############################");
    }

    static void Case(Board board, PerftCase perftCase, bool print = false, bool qSearch = false)
    {
        string FEN = perftCase.fen;
        int depth = perftCase.depth;
        ulong expectation = perftCase.exp;
        
        Console.WriteLine($"Running case with FEN: \"{FEN}\" | EXPECTATION: {expectation}");
        board.LoadPositionFromFen(FEN);

        Test(board, depth, expectation, print, qSearch);
    }

    public static void Test(Board board, int depth, ulong expectation, bool print = false, bool qSearch = false)
    {
        System.Diagnostics.Stopwatch sw = new();
        sw.Start();

        ulong result = Calculate(board, depth, print: print, qSearch: qSearch);

        sw.Stop();
        double milliseconds = sw.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;
        Console.WriteLine($"\tResult: {(result == expectation ? "PASS" : "FAIL")} | Output: {result} | EXPECTATION {expectation} | Time: {milliseconds:F5}ms. ({milliseconds / 1000d:F5}sec.)\n");
    }

    public static ulong CalculateInfiniteQSearch(Board board, int plyFromRoot = 0, bool print = false)
    {
        ulong nodes = 0;
        Span<Move> legalMoves = stackalloc Move[128];
        board.MoveGen.GenerateMoves(ref legalMoves, genOnlyCaptures: true);

        if (legalMoves.Length == 0)
        {
            return 1;
        }

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);

            ulong n = CalculateInfiniteQSearch(board, plyFromRoot + 1);
            nodes += n;

            board.UnmakeMove(move);

            if (plyFromRoot == 0 && print)
            {
                Move.PrintMove(move);
                Console.WriteLine($"Nodes: {n}");
            }
        }

        return nodes;
    }

    public static ulong Calculate(Board board, int depth, int plyFromRoot = 0, bool print = false, bool qSearch = false)
    {
        if (depth == 0)
        {
            return 1;
        }

        ulong nodes = 0;
        Span<Move> legalMoves = stackalloc Move[256];
        board.MoveGen.GenerateMoves(ref legalMoves, genOnlyCaptures: qSearch);
        
        if (depth == 1)
        {
            if (plyFromRoot == 0)
            {
                Console.WriteLine($"total {legalMoves.Length}");
            }
            
            return (ulong) legalMoves.Length;
        }

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);

            ulong n = Calculate(board, depth - 1, plyFromRoot + 1, qSearch: qSearch);
            nodes += n;

            board.UnmakeMove(move);

            if (plyFromRoot == 0 && print)
            {
                Move.PrintMove(move);
                Console.WriteLine($"Nodes: {n}");
            }
        }

        return nodes;
    }
}

public struct PerftPosition
{
    string fen;
    ulong[] exp;

    public PerftPosition(string FEN, ulong[] expectations)
    {
        fen = FEN;
        exp = expectations;
    }

    public PerftCase Get(int depth)
    {
        int d = Math.Min(depth, exp.Length);
        d = Math.Max(d, 1);
        return new PerftCase(fen, d, exp[d - 1]);
    }
}

public struct PerftCase
{
    public string fen;
    public int depth;
    public ulong exp;

    public PerftCase(string FEN, int depth, ulong expectation)
    {
        fen = FEN;
        this.depth = depth;
        exp = expectation;
    }
}