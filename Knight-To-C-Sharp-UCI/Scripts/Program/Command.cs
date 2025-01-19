public static class Command
{
    // References
    static Board board => MainProcess.board;
    static Bot engine => MainProcess.engine;
    static TranspositionTable tt => engine.GetSearcher().GetTT();

    static int MaxThinkTime => Configuration.MaxThinkTime;
    static int MinThinkTime => Configuration.MinThinkTime;

    public static int RecieveCommand(string command)
    {
        string[] tokens = command.Split(' ');
        string prefix = tokens.FirstOrDefault(string.Empty);

        // Available Commands Pre-UCI Load
        switch (prefix)
        {
            case "quit":
            {
                engine.CancelAndWait();
                return 1;
            }
            case "uci":
            {
                Console.WriteLine("id name Knight-To-C-Sharp");
                Console.WriteLine("id author KMS (Eaten_Apple on lichess.org)");
                Console.WriteLine("uciok");
            }
            break;
            case "ucinewgame":
            {
                if (engine.IsSearching())
                {
                    engine.CancelAndWait();
                }

                board.LoadInitialPosition();
            }
            break;
            case "d":
            {
                board.PrintBoardAndMoves();
            }
            break;
            case "cmd":
            {
                if (tokens.Length > 1)
                {
                    RecieveCustomCommand(command[4..]);
                }
            }
            break;
            case "position":
            {
                ProcessPositionCommand(command, tokens);
            }
            break;
            case "go":
            {
                ProcessGoCommand(tokens);
            }
            break;
            case "stop":
            {
                engine.CancelAndWait();
            }
            break;
            case "isready":
            {
                Console.WriteLine("readyok");
            }
            break;

            default:
                break;
        }

        return 0;
    }

    public static int RecieveCustomCommand(string command)
    {
        string[] tokens = command.Split(' ');
        string prefix = tokens.FirstOrDefault(string.Empty);

        CancelSearch();

        switch (prefix)
        {
            case "move":
            {
                if (tokens.Length <= 1)
                {
                    break;
                }

                board.MakeConsoleMove(tokens[1]);
            }
            break;
            case "eval":
            {
                Console.WriteLine($"debug cmd eval: {engine.GetSearcher().GetEvaluation().Evaluate(verbose: true)}");
            }
            break;
            case "zobrist":
            {
                Console.WriteLine($"debug cmd zobrist: {board.ZobristKey}");
            }
            break;
            case "parsebook":
            {
                BookParser.Parse();
            }
            break;
            case "dir":
            {
                Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
            }
            break;
            case "keybook":
            {
                if (tokens.Length > 1)
                {
                    ulong key = ulong.Parse(tokens[1]);
                    PrintBookMoves(key);
                }
            }
            break;
            case "booktest":
            {
                Book.GetRandomMove(board).Print();
            }
            break;
            case "book":
            {
                PrintBookMoves(board.ZobristKey);
            }
            break;
            case "bitboard":
            {
                if (tokens.Length > 1)
                {
                    if (tokens[1] == "print")
                    {
                        board.BBSet.Print();
                    }
                    else if (tokens[1] == "test")
                    {
                        board.BBSet.Test(board);
                    }
                }
            }
            break;
            case "enginemove":
            {
                if (tokens.Length > 1)
                {
                    int time = int.Parse(tokens[1]);
                    
                    engine.StartTimedSearch(0, time, () => {
                        board.MakeMove(engine.GetMove());
                        board.UpdateLegalMoves();
                        board.PrintBoardAndMoves();
                    });
                }
            }
            break;
            case "tt":
            {
                Console.WriteLine($"key: {board.ZobristKey} tt: {tt.LookupEvaluation(0, 0, 0, 0)} move: {tt.GetStoredMove().San} Index: {tt.Index}");
            }
            break;
            case "ttprint":
            {
                tt.Print();
            }
            break;
            case "magic":
            {
                string subCommand = tokens[1];
                switch (subCommand)
                {
                    case "movement":
                        for (int i = 0; i < 64; i++)
                        {
                            Console.WriteLine($"Rook, Bishop on square {SquareUtils.Name(i)}");
                            Magic.RookMasks[i].Print();
                            Magic.BishopMasks[i].Print();
                        }
                        break;


                    default:
                        break;
                }
            }
            break;
            case "temp":
            {
                Temp();
            }
            break;
            case "timetest":
            {
                if (tokens.Length > 1)
                {
                    string subCommand = tokens[1];
                    switch (subCommand)
                    {
                        case "movegen":
                        {
                            GeneralTimeTest(() => board.MoveGen.GenerateMoves(), testName: "Legal Move Generation", cases: 10);
                        }
                        break;
                        case "perft":
                        {
                            GeneralTimeTest(Perft, testName: "Perft", cases: 3, suiteIteration: 1);
                        }
                        break;
                        case "see":
                        {
                            GeneralTimeTest(Test.SEEPositive, testName: "SEE Positive", cases: 3, suiteIteration: 5);
                        }
                        break;
                        default:
                            break;
                    }
                }
            }
            break;
            case "perft":
            {
                Perft();
            }
            break;
            case "test":
            {
                Test.CurrentTest();
            }
            break;

            default:
                break;
        }

        return 0;
    }

    // Command Funcitons
    static void Perft()
    {
        PerftHelper.Go(board);
    }

    // Temporary Tests
    static void Temp()
    {

    }
    
    // Time Tests
    static void GeneralTimeTest(Action? action = null, string testName = "UnNamed", int cases = 1, int suiteIteration = 10)
    {
        Console.WriteLine("###############");
        Console.WriteLine($"Time Test: {testName}\n");

        for (int i = 0; i < cases; i++)
        {
            Console.WriteLine($"Case {i + 1}");

            double[] msArr = new double[suiteIteration];
            for (int suite = 0; suite < suiteIteration; suite++)
            {
                System.Diagnostics.Stopwatch sw = new ();

                sw.Start();
                action?.Invoke();
                sw.Stop();

                msArr[suite] = sw.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond; 
            }

            Console.WriteLine($"\tMS | {string.Join(" ", msArr.Select(ms => $"{ms:F5}ms."))}");
            Console.WriteLine($"\tAVG | {msArr.Average():F5}");
        }

        Console.WriteLine("###############");
    }

    static void ProcessPositionCommand(string command, string[] tokens)
    {
        CancelSearch();

        bool containsMoves = tokens.Contains("moves");
        string subCommand = tokens[1..].FirstOrDefault(string.Empty);
        
        if (subCommand == "fen")
        {
            if (tokens.Length < 3)
            {
                return;
            }

            string fen;
            if (containsMoves)
            {
                fen = command.Substring(13, command.IndexOf("moves") - 14);
            }
            else
            {
                fen = command.Substring(13);
            }

            board.LoadPositionFromFen(fen);
        }
        else if (subCommand == "startpos")
        {
            board.LoadPositionFromFen(Board.InitialFen);
        }
        
        if (containsMoves)
        {
            int index = Array.IndexOf(tokens, "moves");
            for (int i = index + 1; i < tokens.Length; i++)
            {
                board.MakeConsoleMove(tokens[i]);
            }
        }
    }
    static void ProcessGoCommand(string[] tokens)
    {
        CancelSearch();

        int tokenIndex = 0;

        // Search Launch Info
        int depth = Configuration.MaxDepth;
        int wtime = Infinity.PositiveInfinity;
        int btime = Infinity.PositiveInfinity;
        int winc = 0;
        int binc = 0;

        bool infinite = false;
        bool gotThinkTime = false;
        int thinkTime = MinThinkTime;
        
        // Sub Commands
        while (true)
        {
            string subCommand = tokens[tokenIndex];

            if (subCommand == "perft")
            {
                int d = GetIntegerAfterLabel(subCommand, tokens);
                PerftHelper.Test(board, d, 0, true);
                return;
            }
            else if (subCommand == "depth")
            {
                depth = GetIntegerAfterLabel(subCommand, tokens);
                tokenIndex++;
                gotThinkTime = true;
                thinkTime = MaxThinkTime;
            }
            else if (subCommand == "infinite")
            {
                depth = Configuration.MaxDepth;
                gotThinkTime = true;
                infinite = true;
            }
            else if (subCommand == "movetime")
            {
                thinkTime = GetIntegerAfterLabel(subCommand, tokens);
                
                depth = Configuration.MaxDepth;
                tokenIndex++;
                gotThinkTime = true;
            }
            else if (subCommand == "wtime")
            {
                wtime = GetIntegerAfterLabel(subCommand, tokens);
                tokenIndex++;
            }
            else if (subCommand == "btime")
            {
                btime = GetIntegerAfterLabel(subCommand, tokens);
                tokenIndex++;
            }
            else if (subCommand == "winc")
            {
                winc = GetIntegerAfterLabel(subCommand, tokens);
                tokenIndex++;
            }
            else if (subCommand == "binc")
            {
                binc = GetIntegerAfterLabel(subCommand, tokens);
                tokenIndex++;
            }

            tokenIndex++;

            if (tokenIndex >= tokens.Length)
            {
                break;
            }
        }

        // Choose Think Time
        if (!gotThinkTime)
        {
            thinkTime = engine.DecideThinkTime(wtime, btime, winc, binc, MaxThinkTime, MinThinkTime);
        }

        Console.WriteLine($"info string searchtime {(!infinite ? thinkTime : "infinite")}");
        
        if (infinite)
        {
            engine.StartTimedSearch(depth, -1);
        }
        else
        {
            engine.StartTimedSearch(depth, thinkTime);
        }
    }
    static int GetIntegerAfterLabel(string label, string[] tokens)
    {
        int result = 0;

        if (Array.IndexOf(tokens, label) + 1 < tokens.Length)
        {
            if (!int.TryParse(tokens[Array.IndexOf(tokens, label) + 1], out result))
            {
                result = 0;
            }
        }

        return result;
    }
    
    static void PrintBookMoves(ulong key)
    {
        board.PrintLargeBoard();
        Console.WriteLine($"Zobrist Key: {board.ZobristKey}");

        BookPosition bp = Book.TryToGetBookPosition(key);
        if (bp.IsEmpty())
        {
            return;
        }
        for (int i = 0; i < bp.Moves.Count; i++)
        {
            Console.WriteLine($"{bp.Moves[i].San} {bp.Num[i]}");
        }
    }

    [Inline]
    static void CancelSearch() {
        if (engine.IsSearching()) {
            engine.CancelAndWait();
        }
    }
}