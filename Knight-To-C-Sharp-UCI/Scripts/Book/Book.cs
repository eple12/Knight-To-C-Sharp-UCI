using System.IO;

public static class Book
{
    public static readonly string DIR = "C:\\Users\\user\\Desktop\\WorkSpace\\Knight-To-C-Sharp-UCI\\Knight-To-C-Sharp-UCI";
    public static readonly string PATH = "/Resource/KeyBook.txt";
    public static Dictionary<ulong, BookPosition> OpeningBook = new Dictionary<ulong, BookPosition>();

    static Random RatioRandom = new Random();

    public static void GenerateTable()
    {
        // System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        // sw.Start();

        ulong key = 0;
        List<string> moves = new List<string>();
        List<int> nums = new List<int>();

        foreach (string line in File.ReadLines(DIR + PATH))
        {
            string[] split = line.Split(' ');
            if (split[0] == "key")
            {
                if (moves.Count > 0)
                {
                    OpeningBook.Add(key, new BookPosition(moves, nums));
                }
                moves = new List<string>();
                nums = new List<int>();
                key = ulong.Parse(split[1]);
            }
            else
            {
                moves.Add(split[0]);
                nums.Add(int.Parse(split[1]));
            }
        }

        // sw.Stop();
        // Console.WriteLine(sw.ElapsedMilliseconds);
    }

    public static BookPosition TryToGetBookPosition(ulong key)
    {
        if (OpeningBook.ContainsKey(key))
        {
            return OpeningBook[key];
        }
        else
        {
            return new BookPosition();
        }
    }

    public static Move GetRandomMove(Board board)
    {
        if (board.LegalMoves.Count < 1)
        {
            return Move.NullMove;
        }

        if (OpeningBook.ContainsKey(board.ZobristKey))
        {
            BookPosition bookPosition = OpeningBook[board.ZobristKey];
            string moveString = GetRandomRatio(bookPosition.Moves, bookPosition.Num);
            
            Move move = Move.FindMove(board.LegalMoves, moveString);

            return move;
        }
        else
        {
            return Move.NullMove;
        }
    }

    public static string GetRandomRatio(List<string> options, List<int> ratios)
    {
        if (ratios.Count != options.Count)
        {
            return Move.NullMoveString;
        }

        int total = 0;
        foreach (int ratio in ratios)
        {
            total += ratio;
        }
        
        // Random value between 0 and total value
        int randomNumber = RatioRandom.Next(0, total);

        int cumulativeSum = 0;
        for (int i = 0; i < ratios.Count; i++)
        {
            cumulativeSum += ratios[i];
            if (randomNumber < cumulativeSum)
            {
                return options[i];
            }
        }

        // Failsafe
        return options[options.Count - 1];
    }
}

public struct BookPosition
{
    public List<string> Moves;
    public List<int> Num;

    public BookPosition()
    {
        Moves = new List<string>();
        Num = new List<int>();
    }

    public BookPosition(List<string> moves, List<int> nums)
    {
        Moves = moves;
        Num = nums;
    }

    public bool IsEmpty()
    {
        return Moves.Count < 1;
    }
}