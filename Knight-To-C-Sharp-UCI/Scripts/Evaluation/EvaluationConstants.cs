public static class EvaluationConstants
{
    public struct Score {
        int mg, eg;
        public Score(int mg, int eg) : this() {
            this.mg = mg;
            this.eg = eg;
        }

        // phase => 0 ~ 256
        public readonly int this[int phase] {
            get {
                if (phase == 0) {
                    return mg;
                }
                else if (phase == 256) {
                    return eg;
                }

                return (mg * (256 - phase) + eg * phase) >> 8;
            }
        }
    }
    static Score S(int mg, int eg) => new(mg, eg);

    // Checkmate
    public const int CheckmateEval = 99999;

    // Material
    public static readonly Score[] MaterialValues = { S(101, 149), S(472, 557), S(441, 516), S(522, 904), S(1433, 1819) };

    // Piece Mobility
    public static readonly Score PieceMobilityPerSquare = S(8, 6);

    // Outpost
    public static readonly Score OutpostBonus = S(23, 29);

    // Mop-Up
    public static readonly Score CloserToEnemyKing = S(1, -4);
    public static readonly Score EnemyKingCorner = S(-13, 14);
    public static readonly Score EnemyKingFriendlyBishopSquare = S(0, 0);

    // Open File
    public static readonly Score RookOnOpenFileBonus = S(39, 7);
    public static readonly Score RookOnSemiOpenFileBonus = S(16, 6);

    // Pawn structure
    public static readonly Score[] PassedPawnBonus = 
    { S(0, 0), S(99, 256), S(80, 194), S(19, 101), S(-24, 64), S(-19, 22), S(-12, 14), S(15, 15) };
    public static readonly Score[] IsolatedPawnPenaltyByCount = 
    { S(-15, -18), S(2, 4), S(24, 26), S(64, 44), S(85, 72), S(77, 122), S(118, 157), S(132, 197), S(75, 74) };

    // King Safety
    public static readonly Score[] VirtualKingMobilityBonus = {
        S(61, -132), S(115, -111), S(110, -97), S(98, -98), S(83, -82), S(73, -73), S(66, -65), S(57, -63), S(52, -49), S(29, -30), 
        S(8, -19), S(-16, -5), S(-44, 5), S(-81, 17), S(-110, 26), S(-145, 35), S(-161, 39), S(-193, 46), S(-187, 46), S(-187, 44), 
        S(-230, 52), S(-230, 47), S(-233, 59), S(-221, 55), S(-245, 66), S(-190, 61), S(-125, 53)
    };

    public static readonly Score KingOpenFilePenalty = S(-68, 33);
    public static readonly Score KingSemiOpenFilePenalty = S(-18, 42);

    public static readonly Score KingShieldBonus = S(14, 10);
}