using System.IO;

public class BookParser
{
    readonly string SourcePath = "C:\\Users\\user\\Desktop\\WorkSpace\\Knight-To-C-Sharp-UCI\\Knight-To-C-Sharp-UCI\\Resource\\Book\\games.txt";
    readonly string TargetPath = "C:\\Users\\user\\Desktop\\WorkSpace\\Knight-To-C-Sharp-UCI\\Knight-To-C-Sharp-UCI\\Resource\\Book\\book.txt";

    public BookParser()
    {

    }

    public void Parse()
    {
        if (File.Exists(SourcePath) && File.Exists(TargetPath))
        {
            int i = 0;
            foreach (string line in File.ReadLines(SourcePath))
            {
                i++;
                // if (line.Contains("pos"))
                // {
                //     string fen = line.Substring(4);
                //     MainProcess.board.LoadPositionFromFen(fen);
                //     ulong key = Zobrist.GetZobristKey(MainProcess.board);

                //     File.AppendAllText(TargetPath, $"key {key}\n");
                // }
                // else if (!string.IsNullOrEmpty(line))
                // {
                //     File.AppendAllText(TargetPath, $"{line}\n");
                // }
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
                        if (idx % 2 == 0)
                        {
                            // Move string
                            Move m = Move.FindMove(MainProcess.board.LegalMoves, token);
                            File.AppendAllText(TargetPath, $" {m.moveValue} ");
                        }
                        else
                        {
                            // num
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

