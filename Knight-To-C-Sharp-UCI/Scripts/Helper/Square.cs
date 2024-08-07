public static class Square
{
    private static Dictionary<char, int> fileToIndexTable = new Dictionary<char, int>()
    {
        {'a', 0}, {'b', 1}, {'c', 2}, {'d', 3}, {'e', 4}, {'f', 5}, {'g', 6}, {'h', 7}
    };
    
    private static string indexToFile = "abcdefgh";

    public static int SquareNameToIndex(string name)
    {
        return fileToIndexTable[name[0]] + ((int)Char.GetNumericValue(name[1]) - 1) * 8;
    }

    public static string SquareIndexToName(int square)
    {
        return indexToFile[square % 8] + Convert.ToString((square / 8) + 1);
    }

    public static int FileRankToSquareIndex(int file, int rank)
    {
        return file + rank * 8;
    }

    public static int EnpassantFileToCaptureSquare(int enpFile, bool isWhiteTurn)
    {
        if (enpFile == 8)
        {
            return 64;
        }

        return enpFile + (!isWhiteTurn ? 16 : 40);
    }

    public static int EnpassantFileToPawnSquare(int enpFile, bool isWhiteTurn)
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

        int enpSquare = EnpassantFileToPawnSquare(enpFile, !board.isWhiteTurn);

        if (enpFile > 0)
        {
            if (board.position[enpSquare - 1] == (Piece.Pawn | (!board.isWhiteTurn ? Piece.White : Piece.Black)))
            {
                return true;
            }
        }
        
        if (enpFile < 7)
        {
            if (board.position[enpSquare + 1] == (Piece.Pawn | (!board.isWhiteTurn ? Piece.White : Piece.Black)))
            {
                return true;
            }
        }

        return false;
    }
}