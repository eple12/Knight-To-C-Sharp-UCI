using System;

public static class Piece
{
    // Piece type
    public static readonly int None = 0;
    public static readonly int Pawn = 1;
    public static readonly int Knight = 2;
    public static readonly int Bishop = 3;
    public static readonly int Rook = 4;
    public static readonly int Queen = 5;
    public static readonly int King = 6;


    // Piece color
    public static readonly int White = 8;
    public static readonly int Black = 16;

    // Masks
    static readonly int typeMask = 0b00111;
    static readonly int colorMask = 0b11000;
    
    public static bool IsColor(int piece, int color)
    {
        return (piece & colorMask) == color;
    }

    public static bool IsWhitePiece(int piece)
    {
        return IsColor(piece, White);
    }

    public static int GetType(int piece)
    {
        return piece & typeMask;
    }

    public static int GetBitboardIndex(int piece)
    {
        if (piece == None)
        {
            return BitboardIndex.Invalid;
        }
        return GetType(piece) - 1 + (IsWhitePiece(piece) ? 0 : 6);
    }

    public static bool IsDiagonalPiece(int piece)
    {
        int type = GetType(piece);
        return type == Bishop || type == Queen;
    }

    public static bool IsStraightPiece(int piece)
    {
        int type = GetType(piece);
        return type == Rook || type == Queen;
    }
}
