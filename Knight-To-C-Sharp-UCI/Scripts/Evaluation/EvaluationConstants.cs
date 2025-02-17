public static class EvaluationConstants
{
    public struct Score {
        int mg, eg;
        public Score(int mg, int eg) : this() {
            this.mg = mg;
            this.eg = eg;
        }

        public int this[int index] => index == 0 ? mg : eg;
    }
    static Score S(int mg, int eg) => new(mg, eg);

    // Checkmate
    public const int CheckmateEval = 99999;

    // Material
    public static readonly Score[] MaterialValues = { S(100, 100), S(320, 320), S(325, 325), S(500, 500), S(900, 900) };

    // Piece Mobility
    public static readonly Score PieceMobilityPerSquare = S(1, 1);

    // Outpost
    public static readonly Score OutpostBonus = S(20, 20);

    // Mop-Up
    public const int CloserToEnemyKing = 8;
    public const int EnemyKingCorner = 6;
    public const int EnemyKingFriendlyBishopSquare = 8;

    // Open File
    public static readonly Score RookOnOpenFileBonus = S(20, 20);
    public static readonly Score RookOnSemiOpenFileBonus = S(20, 20);

    // Pawn structure
    public static readonly Score[] PassedPawnBonus = 
    { S(0, 0), S(120, 120), S(80, 80), S(60, 60), S(40, 40), S(30, 30), S(15, 15), S(15, 15) };
    public static readonly Score[] IsolatedPawnPenaltyByCount = 
    { S(0, 0), S(10, 10), S(25, 25), S(50, 50), S(75, 75), S(75, 75), S(75, 75), S(75, 75), S(75, 75) };
    public static readonly Score SpaceAdvantagePerSquare = S(1, 1);

    // King Safety
    public static readonly Score DirectKingFrontPawnPenalty = S(50, 50);
    public static readonly Score DistantKingFrontPawnPenalty = S(30, 30);
    public static readonly Score DirectKingFrontPiecePenalty = S(30, 30);
    public static readonly Score DistantKingFrontPiecePenalty = S(20, 20);

    public static readonly int[] TotalKingShield = { 3 * DirectKingFrontPawnPenalty[0], 3 * DirectKingFrontPawnPenalty[1] };

    // King Open Files
    public static Score KingOpenPenalty = S(75, 20);
    public static Score KingAdjacentOpenPenalty = S(25, 5);

    // King Ring Attackers
    public static readonly Score KingRingQueen = S(50, 50);
    public static readonly Score KingRingRook = S(35, 35);
    public static readonly Score KingRingBishop = S(20, 20);
    public static readonly Score KingRingKnight = S(20, 20);
    public static readonly Score KingRingPawn = S(15, 15);
}