public static class PieceUtils
{
    // Piece type
    public const int None = 0;
    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int Queen = 5;
    public const int King = 6;

    // Piece color
    public const int White = 8;
    public const int Black = 16;

    // Masks
    const int typeMask = 0b00111;
    const int colorMask = 0b11000;

    public static Dictionary<char, Piece> charToPiece = new()
    {
        {'P', White | Pawn}, {'N', White | Knight}, {'B', White | Bishop}, 
        {'R', White | Rook}, {'Q', White | Queen}, {'K', White | King}, 

        {'p', Black | Pawn}, {'n', Black | Knight}, {'b', Black | Bishop}, 
        {'r', Black | Rook}, {'q', Black | Queen}, {'k', Black | King}
    };

    public static string PieceIndexToChar = "PNBRQKpnbrqkWB ";

    [Inline]
    public static int Color(this Piece piece) {
        return piece & colorMask;
    }
    [Inline]
    public static bool IsWhite(this Piece piece) {
        return IsWhitePiece(piece);
    }
    [Inline]
    public static int Type(this Piece piece) {
        return GetType(piece);
    }
    
    [Inline]
    public static bool IsColor(Piece piece, int color)
    {
        return (piece & colorMask) == color;
    }

    [Inline]
    public static bool IsWhitePiece(Piece piece)
    {
        return IsColor(piece, White);
    }

    [Inline]
    public static int GetType(Piece piece)
    {
        return piece & typeMask;
    }

    [Inline]
    public static PieceIndexer GetPieceIndex(Piece piece)
    {
        if (piece == None)
        {
            return PieceIndex.Invalid;
        }
        
        return GetType(piece) - 1 + (IsWhitePiece(piece) ? 0 : 6);
    }

    [Inline]
    public static bool IsDiagonalPiece(Piece piece)
    {
        int type = GetType(piece);
        return type == Bishop || type == Queen;
    }

    [Inline]
    public static bool IsStraightPiece(Piece piece)
    {
        int type = GetType(piece);
        return type == Rook || type == Queen;
    }

    [Inline]
    public static char PieceToChar(Piece piece)
    {
        return PieceIndexToChar[GetPieceIndex(piece)];
    }
}

public struct PieceIndex
{
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

    public const int White = 0;
    public const int Black = 6;
    public const int Pawn = 0;
    public const int Knight = 1;
    public const int Bishop = 2;
    public const int Rook = 3;
    public const int Queen = 4;
    public const int King = 5;

    [Inline]
    public static int MakePawn(bool white)
    {
        return white ? WhitePawn : BlackPawn;
    }
    [Inline]
    public static int MakeKnight(bool white)
    {
        return white ? WhiteKnight : BlackKnight;
    }
    [Inline]
    public static int MakeBishop(bool white)
    {
        return white ? WhiteBishop : BlackBishop;
    }
    [Inline]
    public static int MakeRook(bool white)
    {
        return white ? WhiteRook : BlackRook;
    }
    [Inline]
    public static int MakeQueen(bool white)
    {
        return white ? WhiteQueen : BlackQueen;
    }
    [Inline]
    public static int MakeKing(bool white)
    {
        return white ? WhiteKing : BlackKing;
    }
    [Inline]
    public static int MakeAll(bool white)
    {
        return white ? WhiteAll : BlackAll;
    }
}

public static class PieceIndexerUtils {
    static readonly string[] names = { 
        "WhitePawn", "WhiteKnight", "WhiteBishop", "WhiteRook", "WhiteQueen", "WhiteKing",
        "BlackPawn", "BlackKnight", "BlackBishop", "BlackRook", "BlackQueen", "BlackKing", 
        "WhiteAll", "BlackAll"
    };

    [Inline]
    public static string ToString(this PieceIndexer index) {
        if (index >= PieceIndex.WhitePawn && index <= PieceIndex.BlackAll)
        {
            return names[index];
        }
        else
        {
            return "Invalid";
        }
    }

    [Inline]
    public static bool IsWhiteIndex(this PieceIndexer index)
    {
        return index < PieceIndex.Black;
    }
}