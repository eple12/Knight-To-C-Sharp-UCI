

public class MagicHelper
{
    static readonly int[] RookDirections = {1, 8, -1, -8};
    static readonly int[] BishopDirections = {9, 7, -9, -7};


    public static ulong CreateMovementMask(int square, bool diagonal)
    {
        ulong mask = 0;
        int[] directions = diagonal ? BishopDirections : RookDirections;

        foreach (int offset in directions)
        {
            int numToEdge = PreComputedMoveGenData.NumSquaresToEdge[square, PreComputedMoveGenData.NumEdgeIndex(offset)];
            for (int dst = 1; dst <= numToEdge; dst++)
            {
                int thisSquare = square + dst * offset;
                // int nextSquare = square + (dst + 1) * offset;

                if (dst == numToEdge ? SquareUtils.IsValidSquareExceptOutline(thisSquare) : true)
                {
                    mask |= (ulong) 1 << thisSquare;
                }
                else
                {
                    continue;
                }
            }
        }

        return mask;
    }

    public static ulong[] CreateAllBlockerBitboards(ulong movementMask)
    {
        // Create a list of the indices of the bits that are set in the movement mask
        List<int> moveSquareIndices = new();
        for (int i = 0; i < 64; i++)
        {
            if (movementMask.Contains(i))
            {
                moveSquareIndices.Add(i);
            }
        }

        // Calculate total number of different bitboards (one for each possible arrangement of pieces)
        int numPatterns = 1 << moveSquareIndices.Count; // 2^n
        ulong[] blockerBitboards = new ulong[numPatterns];

        // Create all bitboards
        for (int patternIndex = 0; patternIndex < numPatterns; patternIndex++)
        {
            for (int bitIndex = 0; bitIndex < moveSquareIndices.Count; bitIndex++)
            {
                int bit = (patternIndex >> bitIndex) & 1;
                blockerBitboards[patternIndex] |= (ulong)bit << moveSquareIndices[bitIndex];
            }
        }

        return blockerBitboards;
    }
    public static ulong LegalMoveBitboardFromBlockers(int square, ulong blockerBitboard, bool diagonal)
    {
        ulong bitboard = 0;
        int[] directions = diagonal ? BishopDirections : RookDirections;
        
        foreach (int offset in directions)
        {
            int numToEdge = PreComputedMoveGenData.NumSquaresToEdge[square, PreComputedMoveGenData.NumEdgeIndex(offset)];

            for (int dst = 1; dst <= numToEdge; dst++)
            {
                int thisSquare = square + dst * offset;

                bitboard |= (ulong) 1 << thisSquare;
                if (blockerBitboard.Contains(thisSquare))
                {
                    break;
                }
            }
        }

        return bitboard;
    }

    public MagicHelper() {}
}