using System.IO;

public class BookParser
{
    readonly string SourcePath = "C:\\Users\\user\\Desktop\\WorkSpace\\Knight-To-C-Sharp-UCI\\Knight-To-C-Sharp-UCI\\Resource\\source\\Book.txt";
    readonly string TargetPath = "C:\\Users\\user\\Desktop\\WorkSpace\\Knight-To-C-Sharp-UCI\\Knight-To-C-Sharp-UCI\\Resource\\KeyBook.txt";

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
                if (line.Contains("pos"))
                {
                    string fen = line.Substring(4);
                    MainProcess.board.LoadPositionFromFen(fen);
                    ulong key = Zobrist.GetZobristKey(MainProcess.board);

                    File.AppendAllText(TargetPath, $"key {key}\n");
                }
                else if (!string.IsNullOrEmpty(line))
                {
                    File.AppendAllText(TargetPath, $"{line}\n");
                }

                Console.WriteLine(i + ". " + line);
            }
        }
        else
        {
            Console.WriteLine("Files missing!");
        }
    }
}

