public static class Command
{

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
                MainProcess.Test();
                break;



            default:
                break;
        }

        return 0;
    }
}