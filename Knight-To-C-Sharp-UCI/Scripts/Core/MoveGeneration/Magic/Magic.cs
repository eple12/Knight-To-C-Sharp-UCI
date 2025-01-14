

public static class Magic
{
    public static Bitboard[] RookMasks;
    public static Bitboard[] BishopMasks;

    // [Square] [Key]
    public static Bitboard[][] RookAttacks;
    public static Bitboard[][] BishopAttacks;

    [Inline]
    public static ulong GetSliderAttacks(Square square, Bitboard blockers, bool diagonal)
    {
        return diagonal ? GetBishopAttacks(square, blockers) : GetRookAttacks(square, blockers);
    }

    [Inline]
    public static ulong GetRookAttacks(Square square, Bitboard blockers)
    {
        ulong key = ((blockers & RookMasks[square]) * PreComputedMagic.RookMagics[square]) >> PreComputedMagic.RookShifts[square];
        return RookAttacks[square][key];
    }

    [Inline]
    public static ulong GetBishopAttacks(Square square, Bitboard blockers)
    {
        ulong key = ((blockers & BishopMasks[square]) * PreComputedMagic.BishopMagics[square]) >> PreComputedMagic.BishopShifts[square];
        return BishopAttacks[square][key];
    }

    static Magic()
    {
        RookMasks = new ulong[64];
        BishopMasks = new ulong[64];

        GenerateSlidingMasks();

        RookAttacks = new ulong[64][];
        BishopAttacks = new ulong[64][];

        for (int i = 0; i < 64; i++)
        {
            RookAttacks[i] = CreateTable(i, false, PreComputedMagic.RookMagics[i], PreComputedMagic.RookShifts[i]);
            BishopAttacks[i] = CreateTable(i, true, PreComputedMagic.BishopMagics[i], PreComputedMagic.BishopShifts[i]);
        }
    }

    static void GenerateSlidingMasks()
    {
        for (int i = 0; i < 64; i++)
        {
            RookMasks[i] = MagicHelper.CreateMovementMask(i, false);
            BishopMasks[i] = MagicHelper.CreateMovementMask(i, true);
        }
    }

    static Bitboard[] CreateTable(Square square, bool diagonal, ulong magic, int leftShift)
    {
        int numBits = 64 - leftShift;
        int lookupSize = 1 << numBits;
        Bitboard[] table = new ulong[lookupSize];

        ulong movementMask = MagicHelper.CreateMovementMask(square, diagonal);
        Bitboard[] blockerPatterns = MagicHelper.CreateAllBlockerBitboards(movementMask);

        foreach (Bitboard pattern in blockerPatterns)
        {
            ulong index = (pattern * magic) >> leftShift;
            Bitboard moves = MagicHelper.LegalMoveBitboardFromBlockers(square, pattern, diagonal);
            table[index] = moves;
        }

        return table;
    }
}