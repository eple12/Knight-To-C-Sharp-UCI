using PS = ProgramSettings;

public static class BookParser
{
    static string SourcePath => Path.Combine(PS.Directory, PS.BookSource);
    static string TargetPath => Path.Combine(PS.Directory, PS.BookPath);

    static BookParser()
    {
        
    }

    public static void Parse()
    {
        if (File.Exists(SourcePath) && File.Exists(TargetPath))
        {
            int i = 0;
            foreach (string line in File.ReadLines(SourcePath))
            {
                i++;

                if (!string.IsNullOrEmpty(line))
                {
                    int movesIndex = line.IndexOf("moves");
                    string fen = line.Substring(0, movesIndex - 1);
                    MainProcess.board.LoadPositionFromFen(fen);

                    File.AppendAllText(TargetPath, $"{MainProcess.board.ZobristKey}");

                    string[] tokens = line.Split(' ');
                    int tokenMoveIndex = Array.IndexOf(tokens, "moves");
                    for (int idx = 0; idx < tokens.Length - tokenMoveIndex - 1; idx++)
                    {
                        string token = tokens[idx + tokenMoveIndex + 1];

                        if (idx % 2 == 0) // Move string
                        {
                            Move m = MainProcess.board.LegalMoves.FindMove(token);
                            File.AppendAllText(TargetPath, $" {m.moveValue} ");
                        }
                        else // Weight of this move
                        {
                            File.AppendAllText(TargetPath, token);
                        }
                    }
                    File.AppendAllText(TargetPath, "\n");
                }

                MainProcess.board.LoadPositionFromFen(Board.InitialFen);
                Console.WriteLine(i + ". " + line);
            }
        }
        else
        {
            Console.WriteLine("Files missing!");
        }
    }
}

