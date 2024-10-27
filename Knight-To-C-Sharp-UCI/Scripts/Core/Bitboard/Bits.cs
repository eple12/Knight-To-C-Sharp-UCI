public static class Bits
{
    public static ulong Full = ulong.MaxValue;

    // Board Representation Bitboards
    public static ulong Rank1 = 0xFF;
    public static ulong Rank8 = Rank1 << 56;
    public static ulong FileA = 0x0101010101010101;
    public static ulong FileH = FileA << 7;
    public static ulong NotFileA = ulong.MaxValue ^ FileA;
    public static ulong NotFileH = ulong.MaxValue ^ FileH;

    public static ulong[] RankMask;

    public static ulong[] FileMask;
    public static ulong[] AdjacentFilesMask;
    public static ulong[] TripleFileMask;

    public static ulong[] WhiteForwardMask;
    public static ulong[] BlackForwardMask;

    public static ulong[] WhitePassedPawnMask;
    public static ulong[] BlackPassedPawnMask;

    public static ulong LightSquares;
    public static ulong DarkSquares;

    public static ulong AllEdge;

    static Bits()
    {
        RankMask = new ulong[8];
        for (int i = 0; i < 8; i++)
        {
            RankMask[i] = Rank1 << (8 * i);
        }

        FileMask = new ulong[8];
        for (int i = 0; i < 8; i++)
        {
            FileMask[i] = FileA << i;
        }

        AdjacentFilesMask = new ulong[8];
        for (int i = 0; i < 8; i++)
        {
            AdjacentFilesMask[i] = (i > 0 ? FileMask[i - 1] : 0) | (i < 7 ? FileMask[i + 1] : 0);
        }

        TripleFileMask = new ulong[8];
        for (int i = 0; i < 8; i++)
        {
            TripleFileMask[i] = FileMask[i] | AdjacentFilesMask[i];
        }

        WhiteForwardMask = new ulong[8];
        BlackForwardMask = new ulong[8];
        for (int i = 0; i < 8; i++)
        {
            WhiteForwardMask[i] = Full << 8 * (i + 1);
            BlackForwardMask[i] = Full >> 8 * (8 - i);

            // Bitboard.Print(WhiteForwardMask[i]);
            // Bitboard.Print(BlackForwardMask[i]);
        }

        WhitePassedPawnMask = new ulong[64];
        BlackPassedPawnMask = new ulong[64];

        for (int square = 0; square < 64; square++)
        {
            int file = square % 8;
            int rank = square / 8;

            ulong adj = FileMask[file] | AdjacentFilesMask[file];
            ulong whiteForward = WhiteForwardMask[rank];
            ulong blackForward = BlackForwardMask[rank];

            WhitePassedPawnMask[square] = adj & whiteForward;
            BlackPassedPawnMask[square] = adj & blackForward;
        }

        LightSquares = 0;
        DarkSquares = 0;

        for (int i = 0; i < 64; i++)
        {
            int file = i % 8;
            int rank = i / 8;

            if (((file + rank) % 2) == 1)
            {
                LightSquares |= 1ul << i;
            }
            else
            {
                DarkSquares |= 1ul << i;
            }
        }
    
        AllEdge = FileMask[0] | FileMask[7] | RankMask[0] | RankMask[7];
    }
}