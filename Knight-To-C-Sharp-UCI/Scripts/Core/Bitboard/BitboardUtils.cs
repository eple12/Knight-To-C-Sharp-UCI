using System.Numerics;

public class BitboardSet
{
    ulong[] bitboards;

    public BitboardSet()
    {
        bitboards = new ulong[14];
    }

    public void Add(int pieceIndex, int square)
    {
        bitboards[pieceIndex] = bitboards[pieceIndex].Add(square);
        int allIndex = PieceIndex.IsWhite(pieceIndex) ? PieceIndex.WhiteAll : PieceIndex.BlackAll;
        bitboards[allIndex] = bitboards[allIndex].Add(square);
    }

    public Bitboard this[int index] {
        get {
            return bitboards[index];
        }
    }

    public void Remove(int pieceIndex, int square)
    {
        bitboards[pieceIndex] = bitboards[pieceIndex].Remove(square);
        int allIndex = PieceIndex.IsWhite(pieceIndex) ? PieceIndex.WhiteAll : PieceIndex.BlackAll;
        bitboards[allIndex] = bitboards[allIndex].Remove(square);
    }

    public void Print()
    {
        for (int i = 0; i < 14; i++)
        {
            Console.WriteLine("Bitboard: " + PieceIndex.ToString(i));
            bitboards[i].Print();
        }
    }

    public void Test(Board board)
    {
        ulong[] tempBitboards = new ulong[14];

        for (int i = 0; i < 64; i++)
        {
            int piece = board.Squares[i];

            if (piece != PieceUtils.None)
            {
                int pieceIndex = PieceUtils.GetPieceIndex(piece);
                tempBitboards[pieceIndex] = tempBitboards[pieceIndex].Add(i);

                int allIndex = PieceIndex.IsWhite(pieceIndex) ? PieceIndex.WhiteAll : PieceIndex.BlackAll;
                tempBitboards[allIndex] = tempBitboards[allIndex].Add(i);
            }
        }

        string s = "";
        for (int i = 0; i < 14; i++)
        {
            s += "Index: " + PieceIndex.ToString(i) + ", Result: ";
            s += (tempBitboards[i] == bitboards[i]) ? "Pass" : "Fail";
            s += '\n';
        }

        Console.WriteLine(s);
    }
}

public static class BitboardUtils {
    [Inline]
    public static bool Contains(this Bitboard bitboard, int index)
    {
        return (bitboard & ((ulong) 1 << index)) != 0;
    }
    [Inline]
    public static ulong Add(this Bitboard bitboard, int square)
    {
        return bitboard | ((ulong) 1 << square);
    }
    [Inline]
    public static ulong Remove(this Bitboard bitboard, int square)
    {
        return bitboard & ~((ulong) 1 << square);
    }
    [Inline]
    public static int Count(this Bitboard bitboard)
    {
        return BitOperations.PopCount(bitboard);
    }
    [Inline]
    public static int PopLSB(ref Bitboard bitboard)
    {
        int i = BitOperations.TrailingZeroCount(bitboard);
        bitboard &= bitboard - 1;
        return i;
    }
    [Inline]
    public static ulong Shift(this Bitboard bitboard, int shift)
    {
        if (shift >= 0)
        {
            return bitboard << shift;
        }
        else
        {
            return bitboard >> -shift;
        }
    }
    [Inline]
    public static ulong Make(params int[] values)
    {
        ulong board = 0;

        foreach (int value in values) {
            board |= 1ul << value;
        }

        return board;
    }
    [Inline]
    public static bool MoreThanOne(this Bitboard bitboard)
    {
        return (bitboard & (bitboard - 1)) != 0;
    }

    public static string GetString(this Bitboard bitboard)
    {
        return string.Join('\n', Enumerable.Range(0, 8).Reverse().Select(rank =>
            string.Join(" ", Enumerable.Range(0, 8).Select(file =>
                Contains(bitboard, rank * 8 + file) ? "O" : ".")
            )
        ));
    }
    public static void Print(this Bitboard bitboard)
    {
        Console.WriteLine(GetString(bitboard));
    }
}