using System.Numerics;

public class Bitboard
{
    public ulong[] Bitboards;

    public Bitboard()
    {
        Bitboards = new ulong[14];
    }

    public void Add(int pieceIndex, int square)
    {
        Bitboards[pieceIndex] = Add(Bitboards[pieceIndex], square);
        int allIndex = PieceIndex.IsWhite(pieceIndex) ? PieceIndex.WhiteAll : PieceIndex.BlackAll;
        Bitboards[allIndex] = Add(Bitboards[allIndex], square);
    }

    public void Remove(int pieceIndex, int square)
    {
        Bitboards[pieceIndex] = Remove(Bitboards[pieceIndex], square);
        int allIndex = PieceIndex.IsWhite(pieceIndex) ? PieceIndex.WhiteAll : PieceIndex.BlackAll;
        Bitboards[allIndex] = Remove(Bitboards[allIndex], square);
    }

    public void Print()
    {
        for (int i = 0; i < 14; i++)
        {
            Console.WriteLine("Bitboard: " + PieceIndex.ToString(i));
            Print(Bitboards[i]);
        }
    }

    public void Test(Board board)
    {
        ulong[] bitboards = new ulong[14];

        for (int i = 0; i < 64; i++)
        {
            int piece = board.Squares[i];

            if (piece != Piece.None)
            {
                int pieceIndex = Piece.GetPieceIndex(piece);
                bitboards[pieceIndex] = Add(bitboards[pieceIndex], i);

                int allIndex = PieceIndex.IsWhite(pieceIndex) ? PieceIndex.WhiteAll : PieceIndex.BlackAll;
                bitboards[allIndex] = Add(bitboards[allIndex], i);
            }
        }

        string s = "";
        for (int i = 0; i < 14; i++)
        {
            s += "Index: " + PieceIndex.ToString(i) + ", Result: ";
            s += (bitboards[i] == Bitboards[i]) ? "Pass" : "Fail";
            s += '\n';
        }

        Console.WriteLine(s);
    }

    public static bool Contains(ulong bitboard, int index)
    {
        return (bitboard & ((ulong) 1 << index)) != 0;
    }
    public static ulong Add(ulong bitboard, int square)
    {
        return bitboard | ((ulong) 1 << square);
    }
    public static ulong Remove(ulong bitboard, int square)
    {
        return bitboard & ~((ulong) 1 << square);
    }
    public static void Print(ulong bitboard)
    {
        string s = "";
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                if (Contains(bitboard, rank * 8 + file))
                {
                    s += "O ";
                }
                else
                {
                    s += ". ";
                }
            }
            s += '\n';
        }
        Console.WriteLine(s);
    }
    public static int Count(ulong bitboard)
    {
        return BitOperations.PopCount(bitboard);
    }
    public static int PopLSB(ref ulong bitboard)
    {
        int i = BitOperations.TrailingZeroCount(bitboard);
        bitboard &= bitboard - 1;
        return i;
    }
    public static ulong Shift(ulong bitboard, int shift)
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


}