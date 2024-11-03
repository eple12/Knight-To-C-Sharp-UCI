public static class Square
{   
    // Files
    static readonly Dictionary<char, int> FileNumber = new Dictionary<char, int>()
    {
        {'a', 0}, {'b', 1}, {'c', 2}, {'d', 3}, {'e', 4}, {'f', 5}, {'g', 6}, {'h', 7}
    };
    static readonly string Files = "abcdefgh";
    public const int Invalid = -1;

    public static int Index(string name)
    {
        return FileNumber[name[0]] + ((int)Char.GetNumericValue(name[1]) - 1) * 8;
    }
    public static string Name(int square)
    {
        return Files[square % 8] + Convert.ToString((square / 8) + 1);
    }
    public static int Index(int file, int rank)
    {
        return file + rank * 8;
    }

    public static bool IsLightSquare(int square)
    {
        return ((1ul << square) & Bits.LightSquares) != 0;
    }

    public static int FlipIndex(int square)
    {
        return (square % 8) + (7 - square / 8) * 8;
    }

    public static bool IsValidSquare(int file, int rank)
    {
        if (file >= 0 && file < 8 && rank >= 0 && rank < 8)
        {
            return true;
        }
        return false;
    }
    public static bool IsValidSquareExceptOutline(int square)
    {
        int file = square % 8;
        int rank = square / 8;

        if (file >= 1 && file < 7 && rank >= 1 && rank < 7)
        {
            return true;
        }
        return false;
    }


    // En passant utility
    public static int EnpassantCaptureSquare(int enpFile, bool isWhiteTurn)
    {
        if (enpFile == 8)
        {
            return 64;
        }

        return enpFile + (!isWhiteTurn ? 16 : 40);
    }
    public static int EnpassantAvailablePawnIndex(int enpFile, bool isWhiteTurn)
    {
        if (enpFile == 8)
        {
            return 64;
        }

        return enpFile + (!isWhiteTurn ? 24 : 32);
    }
    public static bool IsValidEnpassantFile(int enpFile, Board board)
    {
        if (enpFile == 8)
        {
            return false;
        }

        // Get opponent's possible enpassant pawn square
        int enpSquare = EnpassantAvailablePawnIndex(enpFile, !board.Turn);

        // There's a pawn on the left
        if (enpFile > 0)
        {
            if (board.Squares[enpSquare - 1] == (Piece.Pawn | (!board.Turn ? Piece.White : Piece.Black)))
            {
                return true;
            }
        }
        // There's a pawn on the right
        if (enpFile < 7)
        {
            if (board.Squares[enpSquare + 1] == (Piece.Pawn | (!board.Turn ? Piece.White : Piece.Black)))
            {
                return true;
            }
        }

        // There is no pawn that can en-passant on this file
        return false;
    }
}