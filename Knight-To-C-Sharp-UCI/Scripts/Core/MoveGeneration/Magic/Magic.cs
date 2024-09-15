

public static class Magic
{
    public static ulong[] RookMasks;
    public static ulong[] BishopMasks;

    // [Square] [Key]
    public static ulong[][] RookAttacks;
    public static ulong[][] BishopAttacks;

    public static ulong GetSliderAttacks(int square, ulong blockers, bool diagonal)
    {
        return diagonal ? GetBishopAttacks(square, blockers) : GetRookAttacks(square, blockers);
    }

    public static ulong GetRookAttacks(int square, ulong blockers)
    {
        ulong key = ((blockers & RookMasks[square]) * PreComputedMagic.RookMagics[square]) >> PreComputedMagic.RookShifts[square];
        return RookAttacks[square][key];
    }

    public static ulong GetBishopAttacks(int square, ulong blockers)
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

    static ulong[] CreateTable(int square, bool diagonal, ulong magic, int leftShift)
    {
        int numBits = 64 - leftShift;
        int lookupSize = 1 << numBits;
        ulong[] table = new ulong[lookupSize];

        ulong movementMask = MagicHelper.CreateMovementMask(square, diagonal);
        ulong[] blockerPatterns = MagicHelper.CreateAllBlockerBitboards(movementMask);

        foreach (ulong pattern in blockerPatterns)
        {
            ulong index = (pattern * magic) >> leftShift;
            ulong moves = MagicHelper.LegalMoveBitboardFromBlockers(square, pattern, diagonal);
            table[index] = moves;
        }

        return table;
    }


}