public static class Command
{
    public static bool autoWhiteEngine = false;
    public static bool autoBlackEngine = false;

    public static void Update()
    {
        if (autoWhiteEngine && MainProcess.board.isWhiteTurn)
        {
            MakeEngineMove();
        }
        if (autoBlackEngine && !MainProcess.board.isWhiteTurn)
        {
            MakeEngineMove();
        }
    }

    public static int RecieveCommand(string command)
    {
        string[] commandList = command.Split(' ');
        string prefix = commandList[0];

        switch (prefix)
        {
            case "quit":
                return 1;
            case "cmd":
                if (commandList.Length > 1)
                {
                    RecieveCustomCommand(command.Substring(4));
                }
                break;
            case "uci":
                Console.WriteLine("id name Knight-To-C-Sharp");
                Console.WriteLine("id author KMS (Eaten_Apple on lichess.org)");
                Console.WriteLine("uciok");
                break;
            case "ucinewgame":
                MainProcess.board.LoadPositionFromFen(Board.initialFen);
                break;
            case "position":
                if (commandList.Length < 2)
                {
                    break;
                }
                if (commandList[1] == "fen")
                {
                    string fen;
                    if (command.Contains("moves"))
                    {
                        fen = command.Substring(13, command.IndexOf("move") - 14);
                    }
                    else
                    {
                        fen = command.Substring(13);
                    }
                    Console.WriteLine(fen);
                    MainProcess.board.LoadPositionFromFen(fen);
                }
                else if (commandList[1] == "startpos")
                {
                    MainProcess.board.LoadPositionFromFen(Board.initialFen);
                }
                MainProcess.board.PrintBoardAndMoves();
                break;



            default:
                break;
        }

        return 0;
    }

    public static void Test()
    {
        MainProcess.engine.StartSearch(6);
        Move.PrintMove(MainProcess.engine.GetMove());
    }
    public static void ToggleAutoWhite()
    {
        if (autoWhiteEngine)
        {
            Console.WriteLine("Toggled off");
        }
        else
        {
            Console.WriteLine("Toggled on");
        }
        autoWhiteEngine = !autoWhiteEngine;
    }
    public static void ToggleAutoBlack()
    {
        if (autoBlackEngine)
        {
            Console.WriteLine("Toggled off");
        }
        else
        {
            Console.WriteLine("Toggled on");
        }
        autoBlackEngine = !autoBlackEngine;
    }
    public static void MakeEngineMove()
    {
        MainProcess.engine.StartSearch(EngineSettings.searchDepth);
        MainProcess.board.MakeConsoleMove(MainProcess.engine.GetMove());
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
            case "test":
                Test();
                break;
            case "autoengine":
                if (commandList.Length <= 1)
                {
                    Console.WriteLine("Auto Engine // White: " + autoWhiteEngine + " Black: " + autoBlackEngine);
                    break;
                }
                if (commandList[1] == "white")
                {
                    ToggleAutoWhite();
                }
                else if (commandList[1] == "black")
                {
                    ToggleAutoBlack();
                }
                break;


            default:
                break;
        }

        return 0;
    }
}