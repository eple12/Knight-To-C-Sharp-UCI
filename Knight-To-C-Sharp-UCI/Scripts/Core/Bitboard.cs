
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



    // public static string BitsString(ulong bitboard)
    // {
    //     ulong mask = (ulong)1;
    //     string bitsString = "";
    //     for (int i = 0; i < 64; i++)
    //     {
    //         bitsString += (bitboard & mask) != 0 ? "1" : "0";
    //         mask <<= 1;
            
    //     }
    //     return bitsString;
    // }

    // public static int TrailingZeroCount(ulong n)
    // {
    //     ulong mask = 1;

    //     for (int i = 0; i < 64; i++, mask <<= 1)
    //     {
    //         if ((n & mask) != 0)
    //         {
    //             return i;
    //         }
    //     }
    //     return 64;
    // }

    // public static List<int> BitIndexList(ulong n)
    // {
    //     ulong mask = 1;
    //     List<int> ints = new List<int>();

    //     for (int i = 0; i < 64; i++, mask <<= 1)
    //     {
    //         if ((n & mask) != 0)
    //         {
    //             ints.Add(i);
    //         }
    //     }
    //     return ints;
    // }

    // public static int GetPieceBitboardIndexAtSquare(in ulong[] bitboards, int index)
    // {
    //     ulong indexMask = (ulong) 1 << index;

    //     if ((bitboards[PieceIndex.WhiteAll] & indexMask) != 0)
    //     {
    //         for (int i = 0; i < 6; i++)
    //         {
    //             if ((bitboards[i] & indexMask) != 0)
    //             {
    //                 return i;
    //             }
    //         }
    //     }
    //     else if ((bitboards[PieceIndex.BlackAll] & indexMask) != 0)
    //     {
    //         for (int i = 6; i < 12; i++)
    //         {
    //             if ((bitboards[i] & indexMask) != 0)
    //             {
    //                 return i;
    //             }
    //         }
    //     }
    //     return PieceIndex.Invalid; // Returns invalid BitboardIndex
    // }
// public static void SetBitboardPieceIndex(ulong[] bitboard, int piece, int index)
    // {
    //     if (piece == Piece.None)
    //     {
    //         return;
    //     }
    //     bitboard[Piece.GetPieceIndex(piece)] |= (ulong) 1 << index;

        
    //     if (Piece.IsWhitePiece(piece))
    //     {
    //         bitboard[PieceIndex.WhiteAll] |= (ulong)1 << index;
    //     }
    //     else
    //     {
    //         bitboard[PieceIndex.BlackAll] |= (ulong)1 << index;
    //     }
    // }

    // public static void SetBitboardIndexToZero(ulong[] bitboard, int piece, int index)
    // {
    //     if (piece == Piece.None)
    //     {
    //         return;
    //     }
    //     bitboard[Piece.GetPieceIndex(piece)] &= ~((ulong) 1 << index);

    //     if (Piece.IsWhitePiece(piece))
    //     {
    //         bitboard[PieceIndex.WhiteAll] &= ~((ulong) 1 << index);
    //     }
    //     else
    //     {
    //         bitboard[PieceIndex.BlackAll] &= ~((ulong) 1 << index);
    //     }
    // }

    // public static void SetSingleBitboardIndexToZero(ref ulong bitboard, int index)
    // {
    //     bitboard &= ~((ulong) 1 << index);
    // }

    // public static bool IsAllBitboardEmpty(ulong[] bitboard, int index)
    // {
    //     return ((bitboard[PieceIndex.WhiteAll] | bitboard[PieceIndex.BlackAll]) 
    //             & ((ulong) 1 << index)) == 0;
    // }



}