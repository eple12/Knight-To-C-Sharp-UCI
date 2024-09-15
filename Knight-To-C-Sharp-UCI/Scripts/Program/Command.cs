
public static class Command
{
    const int MaxThinkTime = 3000 * 1000;
    const int MinThinkTime = 50;

    public static int RecieveCommand(string command)
    {
        string[] tokens = command.Split(' ');
        string prefix = tokens[0];

        // Available Commands Pre-UCI Load
        switch (prefix)
        {
            case "quit":
            {
                MainProcess.engine.CancelAndWait();
            }
            return 1;
            case "uci":
            {
                Console.WriteLine("id name Knight-To-C-Sharp");
                Console.WriteLine("id author KMS (Eaten_Apple on lichess.org)");
                Console.WriteLine("uciok");
            }
            break;
            case "ucinewgame":
            {
                if (MainProcess.engine.IsSearching())
                {
                    MainProcess.engine.CancelAndWait();
                }

                MainProcess.board.LoadPositionFromFen(Board.InitialFen);
            }
            break;
            case "d":
            {
                MainProcess.board.PrintBoardAndMoves();
            }
            break;
            
            default:
                break;
        }

        if (!MainProcess.board.Loaded)
        {
            return 0;
        }

        // Available Commands After Loading UCI-New Game
        switch (prefix)
        {
            case "cmd":
            {
                if (tokens.Length > 1)
                {
                    RecieveCustomCommand(command.Substring(4));
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
                MainProcess.engine.CancelAndWait();
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
        string prefix = tokens[0];

        switch (prefix)
        {
            case "move":
            {
                if (tokens.Length <= 1)
                {
                    break;
                }
                MainProcess.board.MakeConsoleMove(tokens[1]);
            }
            break;
            case "eval":
            {
                Console.WriteLine("debug cmd eval: " + MainProcess.engine.GetEngine().GetEvaluation().Evaluate());
            }
            break;
            case "endweight":
            {
                Console.WriteLine("debug cmd EndgameWeight: " + 
                MainProcess.engine.GetEngine().GetEvaluation().GetEndgameWeight());
            }
            break;
            case "zobrist":
            {
                Console.WriteLine("debug cmd zobrist: " + MainProcess.board.ZobristKey);
            }
            break;
            case "parsebook":
            {
                BookParser parser = new BookParser();
                parser.Parse();
            }
            break;
            case "dir":
            {
                Console.WriteLine("Current Directory: " + Environment.CurrentDirectory);
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
                Console.WriteLine(Move.MoveString(Book.GetRandomMove(MainProcess.board)));
            }
            break;
            case "book":
            {
                PrintBookMoves(MainProcess.board.ZobristKey);
            }
            break;
            case "bitboard":
            {
                if (tokens.Length > 1)
                {
                    if (tokens[1] == "print")
                    {
                        MainProcess.board.BitboardSet.Print();
                    }
                    else if (tokens[1] == "test")
                    {
                        MainProcess.board.BitboardSet.Test(MainProcess.board);
                    }
                }
            }
            break;
            case "enginemove":
            {
                MainProcess.engine.StartTimedSearch(100, 1000, () => {
                    MainProcess.board.MakeMove(MainProcess.engine.GetMove());
                    MainProcess.board.LegalMoves = MainProcess.board.MoveGen.GenerateMoves();
                    MainProcess.board.PrintBoardAndMoves();
                });
            }
            break;
            case "tt":
            {
                Console.WriteLine("key: " + MainProcess.board.ZobristKey + " tt: " + MainProcess.engine.GetEngine().GetTT().LookupEvaluation(0, 0, 0, 0) + " move: " + Move.MoveString(MainProcess.engine.GetEngine().GetTT().GetStoredMove()) + " Index: " + MainProcess.engine.GetEngine().GetTT().Index);
            }
            break;
            case "ttprint":
            {
                MainProcess.engine.GetEngine().GetTT().Print();
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
                            Console.WriteLine($"Rook, Bishop on square {Square.Name(i)}");
                            Bitboard.Print(Magic.RookMasks[i]);
                            Bitboard.Print(Magic.BishopMasks[i]);
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
                            GeneralTimeTest(() => MainProcess.board.MoveGen.GenerateMoves(), testName: "Legal Move Generation", cases: 10);
                        }
                        break;
                        default:
                            break;
                    }
                }
            }
            break;
            
            default:
                break;
        }

        return 0;
    }

    // Temporary Tests
    static void Temp()
    {
        // Console.WriteLine(PreComputedData.numSquaresToEdge[Square.Index("a1"), 5]);
        // foreach (ulong bitboard in MagicHelper.CreateAllBlockerBitboards(Magic.RookMasks[0]))
        // {
        //     Bitboard.Print(bitboard);
        // }
        Bitboard.Print(Magic.GetBishopAttacks(Square.Index("d4"), 1ul << Square.Index("b2") | 1ul << Square.Index("c3") | 1ul << Square.Index("b6")));
    }
    
    // Time Tests
    static void GeneralTimeTest(Action? action = null, string testName = "UnNamed", int cases = 1, int suiteIteration = 10)
    {
        Console.WriteLine("###############");
        Console.WriteLine($"Time Test: {testName}\n");

        for (int i = 0; i < cases; i++)
        {
            Console.WriteLine($"Case {i + 1}");
            string s = "\tMS | ";
            double[] msarr = new double[suiteIteration];
            for (int suite = 0; suite < suiteIteration; suite++)
            {
                System.Diagnostics.Stopwatch sw = new ();
                sw.Start();

                action?.Invoke();

                sw.Stop();
                double milliseconds = sw.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;
                s += $"{milliseconds:F5}" + "ms. ";
                msarr[suite] = milliseconds; 
            }

            Console.WriteLine(s);
            Console.WriteLine($"\tAVG | {msarr.Average():F5}");
        }

        Console.WriteLine("###############");
    }

    static void ProcessPositionCommand(string command, string[] tokens)
    {
        if (tokens.Length < 3)
        {
            return;
        }

        if (MainProcess.engine.IsSearching())
        {
            MainProcess.engine.CancelAndWait();
        }

        bool containsMoves = tokens.Contains("moves");
        string subCommand = tokens[1];
        
        if (subCommand == "fen")
        {
            string fen;
            if (containsMoves)
            {
                fen = command.Substring(13, command.IndexOf("moves") - 14);
            }
            else
            {
                fen = command.Substring(13);
            }
            MainProcess.board.LoadPositionFromFen(fen);
        }
        else if (subCommand == "startpos")
        {
            MainProcess.board.LoadPositionFromFen(Board.InitialFen);
        }
        
        if (containsMoves)
        {
            int index = Array.IndexOf(tokens, "moves");
            for (int i = index + 1; i < tokens.Length; i++)
            {
                MainProcess.board.MakeConsoleMove(tokens[i]);
            }
        }
    }
    static void ProcessGoCommand(string[] tokens)
    {
        if (MainProcess.engine.IsSearching())
        {
            MainProcess.engine.CancelAndWait();
        }

        int tokenIndex = 0;

        // Search Launch Info
        int depth = MainProcess.engine.GetEngine().GetSettings().unlimitedMaxDepth;
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

            if (subCommand == "depth")
            {
                depth = GetIntegerAfterLabel(subCommand, tokens);
                tokenIndex++;
                gotThinkTime = true;
                thinkTime = MaxThinkTime;
            }
            else if (subCommand == "infinite")
            {
                depth = MainProcess.engine.GetEngine().GetSettings().unlimitedMaxDepth;
                gotThinkTime = true;
                infinite = true;
            }
            else if (subCommand == "movetime")
            {
                thinkTime = GetIntegerAfterLabel(subCommand, tokens);
                
                depth = MainProcess.engine.GetEngine().GetSettings().unlimitedMaxDepth;
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
            thinkTime = MainProcess.engine.DecideThinkTime(wtime, btime, winc, binc, MaxThinkTime, MinThinkTime);
        }

        Console.WriteLine($"debug searchtime {thinkTime}");
        
        if (infinite)
        {
            MainProcess.engine.StartSearch(depth);
        }
        else
        {
            MainProcess.engine.StartTimedSearch(depth, thinkTime);
        }
    }
    static int GetIntegerAfterLabel(string label, string[] tokens)
    {
        int result = 0;

        if (Array.IndexOf(tokens, label) + 1 < tokens.Length)
        {
            int.TryParse(tokens[Array.IndexOf(tokens, label) + 1], out result);
        }

        return result;
    }
    
    static void PrintBookMoves(ulong key)
    {
        MainProcess.board.PrintLargeBoard();
        Console.WriteLine("Zobrist Key: " + MainProcess.board.ZobristKey);

        BookPosition bp = Book.TryToGetBookPosition(key);
        if (bp.IsEmpty())
        {
            return;
        }
        for (int i = 0; i < bp.Moves.Count; i++)
        {
            Console.WriteLine(Move.MoveString(bp.Moves[i]) + ' ' + bp.Num[i]);
        }
    }
}