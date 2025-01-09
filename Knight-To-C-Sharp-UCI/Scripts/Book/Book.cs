using System.IO;

public static class Book
{
    public static readonly string DIR = "C:\\Users\\user\\Desktop\\WorkSpace\\Knight-To-C-Sharp-UCI\\Knight-To-C-Sharp-UCI";
    public static readonly string PATH = "\\Resource\\Book\\book.txt";
    public static Dictionary<ulong, BookPosition> OpeningBook = new Dictionary<ulong, BookPosition>();

    static Random RatioRandom = new Random();

    public static void GenerateTable()
    {
        foreach (string line in File.ReadLines(DIR + PATH))
        {
            List<Move> moves = new List<Move>();
            List<int> nums = new List<int>();
            string[] split = line.Split(' ');
            ulong key = ulong.Parse(split[0]);
            
            for (int i = 0; i < split.Length - 1; i++)
            {
                if (i % 2 == 0)
                {
                    moves.Add(new Move(ushort.Parse(split[i + 1])));
                }
                else
                {
                    nums.Add(int.Parse(split[i + 1]));
                }
            }

            OpeningBook[key] = new BookPosition(moves, nums);
        }
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
        if (board.LegalMoves.Length < 1)
        {
            return Move.NullMove;
        }

        if (OpeningBook.ContainsKey(board.ZobristKey))
        {
            BookPosition bookPosition = OpeningBook[board.ZobristKey];
            Move m = GetRandomRatio(bookPosition.Moves, bookPosition.Num);
            
            return m;
        }
        else
        {
            return Move.NullMove;
        }
    }

    public static Move GetRandomRatio(List<Move> options, List<int> ratios)
    {
        if (ratios.Count != options.Count)
        {
            return Move.NullMove;
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
    public List<Move> Moves;
    public List<int> Num;

    public BookPosition()
    {
        Moves = new List<Move>();
        Num = new List<int>();
    }

    public BookPosition(List<Move> moves, List<int> nums)
    {
        Moves = moves;
        Num = nums;
    }

    public bool IsEmpty()
    {
        return Moves.Count < 1;
    }
}