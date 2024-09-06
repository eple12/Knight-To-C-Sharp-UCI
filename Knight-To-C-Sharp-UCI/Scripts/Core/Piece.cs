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


    public static Dictionary<char, int> charToPiece = new Dictionary<char, int>()
    {
        {'P', White | Pawn}, {'N', White | Knight}, {'B', White | Bishop}, 
        {'R', White | Rook}, {'Q', White | Queen}, {'K', White | King}, 

        {'p', Black | Pawn}, {'n', Black | Knight}, {'b', Black | Bishop}, 
        {'r', Black | Rook}, {'q', Black | Queen}, {'k', Black | King}
    };

    public static string PieceIndexToChar = "PNBRQKpnbrqkWB ";
    
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

    public static int GetPieceIndex(int piece)
    {
        if (piece == None)
        {
            return PieceIndex.Invalid;
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

    public static char PieceToChar(int piece)
    {
        return PieceIndexToChar[GetPieceIndex(piece)];
    }
}

public struct PieceIndex{
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