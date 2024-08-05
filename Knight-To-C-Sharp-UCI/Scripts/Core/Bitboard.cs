
using System;
using System.Collections.Generic;

public struct BitboardIndex{
        public const int WhitePawn = 0; 
        public const int WhiteKnight = 1; 
        public const int WhiteBishop = 2; 
        public const int WhiteRook = 3;
        public const int WhiteQueen = 4;
        public const int WhiteKing = 5;
        public const int BlackPawn = 6;
        public const int BlackKnight = 7; 
        public const int BlackBishop = 8; 
        public const int BlackRook = 9;
        public const int BlackQueen = 10;
        public const int BlackKing = 11;
        public const int WhiteAll = 12;
        public const int BlackAll = 13;

        public const int Invalid = 14;
    }

public static class Bitboard
{

    public static void SetBitboardPieceIndex(ulong[] bitboard, int piece, int index)
    {
        if (piece == Piece.None)
        {
            return;
        }
        bitboard[Piece.GetBitboardIndex(piece)] |= (ulong) 1 << index;

        
        if (Piece.IsWhitePiece(piece))
        {
            bitboard[BitboardIndex.WhiteAll] |= (ulong)1 << index;
        }
        else
        {
            bitboard[BitboardIndex.BlackAll] |= (ulong)1 << index;
        }
    }

    public static void SetBitboardIndexToZero(ulong[] bitboard, int piece, int index)
    {
        if (piece == Piece.None)
        {
            return;
        }
        bitboard[Piece.GetBitboardIndex(piece)] &= ~((ulong) 1 << index);

        if (Piece.IsWhitePiece(piece))
        {
            bitboard[BitboardIndex.WhiteAll] &= ~((ulong) 1 << index);
        }
        else
        {
            bitboard[BitboardIndex.BlackAll] &= ~((ulong) 1 << index);
        }
    }

    public static void SetSingleBitboardIndexToZero(ref ulong bitboard, int index)
    {
        bitboard &= ~((ulong) 1 << index);
    }

    public static bool IsAllBitboardEmpty(ulong[] bitboard, int index)
    {
        return ((bitboard[BitboardIndex.WhiteAll] | bitboard[BitboardIndex.BlackAll]) 
                & ((ulong) 1 << index)) == 0;
    }

    public static bool Contains(ulong bitboard, int index)
    {
        return (bitboard & ((ulong) 1 << index)) != 0;
    }

    public static string BitsString(ulong bitboard)
    {
        ulong mask = (ulong)1;
        string bitsString = "";
        for (int i = 0; i < 64; i++)
        {
            bitsString += (bitboard & mask) != 0 ? "1" : "0";
            mask <<= 1;
            
        }
        return bitsString;
    }

    public static int TrailingZeroCount(ulong n)
    {
        ulong mask = 1;

        for (int i = 0; i < 64; i++, mask <<= 1)
        {
            if ((n & mask) != 0)
            {
                return i;
            }
        }
        return 64;
    }

    public static List<int> BitIndexList(ulong n)
    {
        ulong mask = 1;
        List<int> ints = new List<int>();

        for (int i = 0; i < 64; i++, mask <<= 1)
        {
            if ((n & mask) != 0)
            {
                ints.Add(i);
            }
        }
        return ints;
    }

    public static int GetPieceBitboardIndexAtSquare(in ulong[] bitboards, int index)
    {
        ulong indexMask = (ulong) 1 << index;

        if ((bitboards[BitboardIndex.WhiteAll] & indexMask) != 0)
        {
            for (int i = 0; i < 6; i++)
            {
                if ((bitboards[i] & indexMask) != 0)
                {
                    return i;
                }
            }
        }
        else if ((bitboards[BitboardIndex.BlackAll] & indexMask) != 0)
        {
            for (int i = 6; i < 12; i++)
            {
                if ((bitboards[i] & indexMask) != 0)
                {
                    return i;
                }
            }
        }
        return BitboardIndex.Invalid; // Returns invalid BitboardIndex
    }



}