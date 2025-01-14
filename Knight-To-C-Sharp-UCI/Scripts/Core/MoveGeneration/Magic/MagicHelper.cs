public class MagicHelper
{
    static readonly int[] RookDirections = { 1, 8, -1, -8 };
    static readonly int[] BishopDirections = { 9, 7, -9, -7 };

    public static Bitboard CreateMovementMask(Square square, bool diagonal)
    {
        Bitboard mask = 0;
        int[] directions = diagonal ? BishopDirections : RookDirections;

        foreach (int offset in directions)
        {
            int numToEdge = PreComputedMoveGenData.NumSquaresToEdge[square, PreComputedMoveGenData.NumEdgeIndex(offset)];
            for (int dst = 1; dst <= numToEdge; dst++)
            {
                Square thisSquare = square + dst * offset;

                if (dst == numToEdge ? SquareUtils.IsValidSquareExceptOutline(thisSquare) : true)
                {
                    mask = mask.Add(thisSquare);
                }
                else
                {
                    continue;
                }
            }
        }

        return mask;
    }

    public static Bitboard[] CreateAllBlockerBitboards(Bitboard movementMask)
    {
        // Create a list of the indices of the bits that are set in the movement mask
        List<Square> moveSquareIndices = new();
        for (int i = 0; i < 64; i++)
        {
            if (movementMask.Contains(i))
            {
                moveSquareIndices.Add(i);
            }
        }

        // Calculate total number of different bitboards (one for each possible arrangement of pieces)
        int numPatterns = 1 << moveSquareIndices.Count; // 2^n
        Bitboard[] blockerBitboards = new Bitboard[numPatterns];

        // Create all bitboards
        for (int patternIndex = 0; patternIndex < numPatterns; patternIndex++)
        {
            for (int bitIndex = 0; bitIndex < moveSquareIndices.Count; bitIndex++)
            {
                int bit = (patternIndex >> bitIndex) & 1;
                blockerBitboards[patternIndex] |= (ulong) bit << moveSquareIndices[bitIndex];
            }
        }

        return blockerBitboards;
    }
    public static Bitboard LegalMoveBitboardFromBlockers(Square square, Bitboard blockerBitboard, bool diagonal)
    {
        Bitboard bitboard = 0;
        int[] directions = diagonal ? BishopDirections : RookDirections;
        
        foreach (int offset in directions)
        {
            int numToEdge = PreComputedMoveGenData.NumSquaresToEdge[square, PreComputedMoveGenData.NumEdgeIndex(offset)];

            for (int dst = 1; dst <= numToEdge; dst++)
            {
                Square thisSquare = square + dst * offset;

                bitboard = bitboard.Add(thisSquare);
                if (blockerBitboard.Contains(thisSquare))
                {
                    break;
                }
            }
        }

        return bitboard;
    }

    public MagicHelper()
    {

    }
}