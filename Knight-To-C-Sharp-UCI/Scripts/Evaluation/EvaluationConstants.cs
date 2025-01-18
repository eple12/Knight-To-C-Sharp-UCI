public static class EvaluationConstants
{
    // Checkmate
    public const int CheckmateEval = 99999;

    // Pawn structure
    public static readonly int[] PassedPawnBonus = { 0, 120, 80, 60, 40, 30, 15, 15 };
    public static readonly int[] IsolatedPawnPenaltyByCount = { 0, 10, 25, 50, 75, 75, 75, 75, 75 };

    // King Safety
    public const int DirectKingFrontPawnPenalty = 50;
    public const int DistantKingFrontPawnPenalty = 30;
    public const int DirectKingFrontPiecePenalty = 30;
    public const int DistantKingFrontPiecePenalty = 20;

    // King Open Files
    public const int KingOpenPenalty = 75;
    public const int KingAdjacentOpenPenalty = 25;

    // King Safety Weight
    public const int KingSafetyQueenWeight = 150;
    public const int KingSafetyRookWeight = 100;
    public const int KingSafetyMinorWeight = 50;
    public const int KingSafetyMaxQueens = 1;
    public const int KingSafetyMaxRooks = 2;
    public const int KingSafetyMaxMinors = 3;
    public const int KingSafetyTotalWeight = KingSafetyMaxQueens * KingSafetyQueenWeight + KingSafetyMaxRooks * KingSafetyRookWeight + KingSafetyMaxMinors * KingSafetyMinorWeight;

    // Open File
    public const int OpenFileBonus = 20;
    public const int SemiOpenFileBonus = 20;

    // Material Values
    public const int PawnValue = 100;
    public const int KnightValue = 320;
    public const int BishopValue = 325;
    public const int RookValue = 500;
    public const int QueenValue = 900;
}