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
    public static readonly Score[] MaterialValues = { S(100, 100), S(320, 320), S(325, 325), S(500, 500), S(900, 900) };

    // Piece Mobility
    public static readonly Score PieceMobilityPerSquare = S(1, 1);

    // Outpost
    public static readonly Score OutpostBonus = S(20, 20);

    // Mop-Up
    public static readonly Score CloserToEnemyKing = S(0, 8);
    public static readonly Score EnemyKingCorner = S(0, 6);
    public static readonly Score EnemyKingFriendlyBishopSquare = S(0, 8);

    // Open File
    public static readonly Score RookOnOpenFileBonus = S(20, 20);
    public static readonly Score RookOnSemiOpenFileBonus = S(20, 20);

    // Pawn structure
    public static readonly Score[] PassedPawnBonus = 
    { S(0, 0), S(120, 120), S(80, 80), S(60, 60), S(40, 40), S(30, 30), S(15, 15), S(15, 15) };
    public static readonly Score[] IsolatedPawnPenaltyByCount = 
    { S(0, 0), S(10, 10), S(25, 25), S(50, 50), S(75, 75), S(75, 75), S(75, 75), S(75, 75), S(75, 75) };
    // public static readonly Score SpaceAdvantagePerSquare = S(1, 1);

    // King Safety
    public static readonly Score DirectKingFrontPawnPenalty = S(50, 50);
    public static readonly Score DistantKingFrontPawnPenalty = S(30, 30);
    public static readonly Score DirectKingFrontPiecePenalty = S(30, 30);
    public static readonly Score DistantKingFrontPiecePenalty = S(20, 20);

    public static readonly Score TotalKingShield = S(DirectKingFrontPawnPenalty[0] * 3, DirectKingFrontPawnPenalty[256] * 3);

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