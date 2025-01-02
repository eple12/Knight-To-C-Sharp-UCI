public static class Configuration
{
    public const int TTSizeInMB = 64;
    public const int MaxDepth = 100;

    // Aspiration Windows
    public const int AspirationWindowMinDepth = 8;
    public const int AspirationWindowBase = 20;

    // Late Move Reduction
    public const int LMR_MinFullSearchedMoves = 3;
    public const int LMR_MinDepth = 3;
    public const double LMR_Divisor = 3.49;
    public const double LMR_Base = 0.75;

    // SEE Reduction
    public const int SEE_BadCaptureReduction = 2;

    public const int MaxLegalMovesCount = 256;
    
    public static readonly int[][] LMR_Reductions = new int[MaxDepth][];

    static Configuration() {
        for (int searchDepth = 1; searchDepth < MaxDepth; ++searchDepth) {
            LMR_Reductions[searchDepth] = new int[MaxLegalMovesCount];
            
            // movesSearchedCount > 0 or we wouldn't be applying LMR
            for (int movesSearchedCount = 1; movesSearchedCount < MaxLegalMovesCount; ++movesSearchedCount)
            {
                LMR_Reductions[searchDepth][movesSearchedCount] = Convert.ToInt32(Math.Round(
                    LMR_Base + (Math.Log(movesSearchedCount) * Math.Log(searchDepth) / LMR_Divisor)
                ));
            }
        }
    }

}