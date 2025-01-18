public static class PreComputedEvalData
{
    public static readonly int[] DistanceFromCenter;
    public static readonly int[,] DistanceFromSquare;

    public static readonly int[] RangeDistanceFromCenter;
    public static readonly int[,] RangeDistanceFromSquare;

    static PreComputedEvalData()
    {
        DistanceFromCenter = new int[64];
        for (int square = 0; square < 64; square++)
        {
            int file = square.File();
            int rank = square.Rank();

            if (rank >= 4)
            {
                if (file >= 4)
                {
                    DistanceFromCenter[square] = file - 4 + rank - 4;
                }
                else
                {
                    DistanceFromCenter[square] = 3 - file + rank - 4;
                }
            }
            else
            {
                if (file >= 4)
                {
                    DistanceFromCenter[square] = file - 4 + 3 - rank;
                }
                else
                {
                    DistanceFromCenter[square] = 3 - file + 3 - rank;
                }
            }
        }
    
        DistanceFromSquare = new int[64, 64];
        for (int startSquare = 0; startSquare < 64; startSquare++)
        {
            for (int targetSquare = 0; targetSquare < 64; targetSquare++)
            {
                int startFile = startSquare.File();
                int startRank = startSquare.Rank();
                int targetFile = targetSquare.File();
                int targetRank = targetSquare.Rank();

                DistanceFromSquare[startSquare, targetSquare] = Math.Abs(targetFile - startFile) + Math.Abs(targetRank - startRank);
            }
        }
    
        RangeDistanceFromCenter = new int[64];
        for (int square = 0; square < 64; square++)
        {
            int file = square.File();
            int rank = square.Rank();

            if (rank >= 4)
            {
                if (file >= 4)
                {
                    RangeDistanceFromCenter[square] = Math.Max(file - 4, rank - 4);
                }
                else
                {
                    RangeDistanceFromCenter[square] = Math.Max(3 - file, rank - 4);
                }
            }
            else
            {
                if (file >= 4)
                {
                    RangeDistanceFromCenter[square] = Math.Max(file - 4, 3 - rank);
                }
                else
                {
                    RangeDistanceFromCenter[square] = Math.Max(3 - file, 3 - rank);
                }
            }
        }

        RangeDistanceFromSquare = new int[64, 64];
        for (int startSquare = 0; startSquare < 64; startSquare++)
        {
            for (int targetSquare = 0; targetSquare < 64; targetSquare++)
            {
                int startFile = startSquare.File();
                int startRank = startSquare.Rank();
                int targetFile = targetSquare.File();
                int targetRank = targetSquare.Rank();

                RangeDistanceFromSquare[startSquare, targetSquare] = Math.Max(Math.Abs(targetFile - startFile), Math.Abs(targetRank - startRank));
            }
        }
    }
}