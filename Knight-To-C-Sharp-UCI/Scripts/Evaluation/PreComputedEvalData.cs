public static class PreComputedEvalData
{
    public static readonly int[] DistanceFromCenter;
    public static readonly int[,] DistanceFromSquare;

    static PreComputedEvalData()
    {
        DistanceFromCenter = new int[64];
        for (int square = 0; square < 64; square++)
        {
            int file = square % 8;
            int rank = square / 8;

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
                int startFile = startSquare % 8;
                int startRank = startSquare / 8;
                int targetFile = targetSquare % 8;
                int targetRank = targetSquare / 8;

                DistanceFromSquare[startSquare, targetSquare] = Math.Abs(targetFile - startFile) + Math.Abs(targetRank - startRank);
            }
        }
    }
}