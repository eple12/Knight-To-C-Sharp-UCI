using PS = ProgramSettings;

public static class Book
{
    static readonly string BookPath = Path.Combine(PS.Directory, PS.BookPath);

    public static Dictionary<ulong, BookPosition> OpeningBook = new();

    static Random RatioRandom = new();

    public static void GenerateTable()
    {
        foreach (string line in File.ReadLines(BookPath))
        {
            List<Move> moves = new List<Move>();
            List<int> nums = new List<int>();
            string[] split = line.Split(' ');
            ulong key = ulong.Parse(split[0]);

            for (int i = 1; i < split.Length; i++)
            {
                if (i % 2 == 1)
                {
                    moves.Add(new Move(ushort.Parse(split[i])));
                }
                else
                {
                    nums.Add(int.Parse(split[i]));
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
            return GetRandomRatio(bookPosition.Moves, bookPosition.Num);
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

        int total = ratios.Sum();

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
        return options[^1];
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