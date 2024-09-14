
using System.Collections.Concurrent;

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
                MainProcess.engine.CancelAndWait();
                return 1;
            case "uci":
                Console.WriteLine("id name Knight-To-C-Sharp");
                Console.WriteLine("id author KMS (Eaten_Apple on lichess.org)");
                Console.WriteLine("uciok");
                break;
            case "ucinewgame":
                if (MainProcess.engine.IsSearching())
                {
                    MainProcess.engine.CancelAndWait();
                }

                MainProcess.board.LoadPositionFromFen(Board.InitialFen);
                break;
            case "d":
                MainProcess.board.PrintBoardAndMoves();
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
                if (tokens.Length > 1)
                {
                    RecieveCustomCommand(command.Substring(4));
                }
                break;
            case "position":
                ProcessPositionCommand(command, tokens);
                break;
            case "go":
                ProcessGoCommand(tokens);
                break;
            case "stop":
                MainProcess.engine.CancelAndWait();
                break;
            case "isready":
                Console.WriteLine("readyok");
                break;
                        
            default:
                break;
        }

        return 0;
    }

    public static int RecieveCustomCommand(string command)
    {
        string[] commandList = command.Split(' ');
        string prefix = commandList[0];

        switch (prefix)
        {
            case "move":
                if (commandList.Length <= 1)
                {
                    break;
                }
                MainProcess.board.MakeConsoleMove(commandList[1]);
                break;
            case "eval":
                Console.WriteLine("debug cmd eval: " + MainProcess.engine.GetEngine().GetEvaluation().Evaluate());
                break;
            case "endweight":
                Console.WriteLine("debug cmd EndgameWeight: " + 
                MainProcess.engine.GetEngine().GetEvaluation().GetEndgameWeight());
                break;
            case "zobrist":
                Console.WriteLine("debug cmd zobrist: " + MainProcess.board.ZobristKey);
                break;
            case "parsebook":
                BookParser parser = new BookParser();
                parser.Parse();
                break;
            case "dir":
                Console.WriteLine("Current Directory: " + Environment.CurrentDirectory);
                break;
            case "keybook":
                if (commandList.Length > 1)
                {
                    ulong key = ulong.Parse(commandList[1]);
                    PrintBookMoves(key);
                }
                break;
            case "booktest":
                Console.WriteLine(Move.MoveString(Book.GetRandomMove(MainProcess.board)));
                break;
            case "book":
                PrintBookMoves(MainProcess.board.ZobristKey);
                break;
            case "bitboard":
                if (commandList.Length > 1)
                {
                    if (commandList[1] == "print")
                    {
                        MainProcess.board.BitboardSet.Print();
                    }
                    else if (commandList[1] == "test")
                    {
                        MainProcess.board.BitboardSet.Test(MainProcess.board);
                    }
                }

                break;
            case "enginemove":
                MainProcess.engine.StartTimedSearch(100, 1000, () => {
                    MainProcess.board.MakeMove(MainProcess.engine.GetMove());
                    MainProcess.board.LegalMoves = MainProcess.board.MoveGen.GenerateMoves();
                    MainProcess.board.PrintBoardAndMoves();
                });
                break;
            case "tt":
                Console.WriteLine("key: " + MainProcess.board.ZobristKey + " tt: " + MainProcess.engine.GetEngine().GetTT().LookupEvaluation(0, 0, 0, 0) + " move: " + Move.MoveString(MainProcess.engine.GetEngine().GetTT().GetStoredMove()) + " Index: " + MainProcess.engine.GetEngine().GetTT().Index);
                break;
            case "ttprint":
                MainProcess.engine.GetEngine().GetTT().Print();
                break;
            
            default:
                break;
        }

        return 0;
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
            // Think Time
            int myTime = MainProcess.board.Turn ? wtime : btime;
            int myInc = MainProcess.board.Turn ? winc : binc;
            // Get a fraction of remaining time to use for current move
            double thinkTimeDouble = myTime / 30.0;
            // Clamp think time if a maximum limit is imposed
            thinkTimeDouble = Math.Min(MaxThinkTime, thinkTimeDouble);
            // Add increment
            if (myTime > myInc * 2)
            {
                thinkTimeDouble += myInc * 0.6;
            }

            double minThinkTime = Math.Min(MinThinkTime, myTime * 0.25);
            thinkTimeDouble = Math.Ceiling(Math.Max(minThinkTime, thinkTimeDouble));

            thinkTime = (int) thinkTimeDouble;
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