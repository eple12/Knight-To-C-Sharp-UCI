using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

public static class Command
{
    public static bool autoWhiteEngine = false;
    public static bool autoBlackEngine = false;

    public static int RecieveCommand(string command)
    {
        string[] commandList = command.Split(' ');
        string prefix = commandList[0];

        // Available Commands Pre-UCI Load
        switch (prefix)
        {
            case "quit":
                if (MainProcess.engine.IsSearching())
                {
                    MainProcess.engine.CancelSearch();
                }

                return 1;
            case "uci":
                Console.WriteLine("id name Knight-To-C-Sharp");
                Console.WriteLine("id author KMS (Eaten_Apple on lichess.org)");
                Console.WriteLine("uciok");
                break;
            case "ucinewgame":
                if (MainProcess.engine.IsSearching())
                {
                    MainProcess.engine.CancelSearch(() => RecieveCommand(command));
                    break;
                }

                MainProcess.board.LoadPositionFromFen(Board.initialFen);
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
                if (commandList.Length > 1)
                {
                    RecieveCustomCommand(command.Substring(4));
                }
                break;
            case "position":
                if (commandList.Length < 2)
                {
                    break;
                }

                if (MainProcess.engine.IsSearching())
                {
                    MainProcess.engine.CancelSearch(() => RecieveCommand(command));
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
                    MainProcess.board.LoadPositionFromFen(fen);
                }
                else if (commandList[1] == "startpos")
                {
                    MainProcess.board.LoadPositionFromFen(Board.initialFen);
                }
                
                if (commandList.Contains("moves"))
                {
                    int index = Array.IndexOf(commandList, "moves");
                    for (int i = index + 1; i < commandList.Length; i++)
                    {
                        MainProcess.board.MakeConsoleMove(commandList[i]);
                    }
                }
                break;
            case "go":
                if (MainProcess.engine.IsSearching())
                {
                    break;
                }
                if (commandList.Contains("depth"))
                {
                    if (Array.IndexOf(commandList, "depth") + 1 < commandList.Length)
                    {
                        int depth = Convert.ToInt32(commandList[Array.IndexOf(commandList, "depth") + 1]);
                        MainProcess.engine.StartSearch(depth, () => {
                            Console.WriteLine("bestmove " + Move.MoveString(MainProcess.engine.GetMove()));
                        });
                    }
                }
                break;
            case "stop":
                MainProcess.engine.CancelSearch();
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
                Console.WriteLine("debug cmd eval: " + MainProcess.engine.GetEngine().GetEvaluation().Evaluate(MainProcess.board));
                break;

            default:
                break;
        }

        return 0;
    }
}