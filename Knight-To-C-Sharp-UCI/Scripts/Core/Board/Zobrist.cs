public static class Zobrist
{
    const int seed = ProgramSettings.ZobristSeed;
    static Random rng = new Random(seed);

    public static readonly ulong[,] pieceArray = new ulong[12, 64];
    public static readonly ulong[] castlingArray = new ulong[16]; // ( - / K / Q / KQ ) for each side, 4^2 possibility
    public static readonly ulong[] enpassantArray = new ulong[9]; // Index 8: En-Passant not available
    public static readonly ulong sideToMove = NextUlong(rng);

    public static void GenerateZobristTable() // Called Initially
    {   
        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            for (int pieceIndex = 0; pieceIndex < 12; pieceIndex++)
            {
                pieceArray[pieceIndex, squareIndex] = NextUlong(rng);
            }
        }

        for (int i = 0; i < castlingArray.Length; i++)
        {
            castlingArray[i] = NextUlong(rng);
        }

        for (int i = 0; i < enpassantArray.Length; i++)
        {
            enpassantArray[i] = NextUlong(rng);
        }
    }

    public static ulong GetZobristKey(Board board) // Called after loading a position
    {
        ulong zobristKey = 0;

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            // Could be invalid
            int pieceBitboardIndex = PieceUtils.GetPieceIndex(board.Squares[squareIndex]);

            if (pieceBitboardIndex != PieceIndex.Invalid)
            {
                zobristKey ^= pieceArray[pieceBitboardIndex, squareIndex];
            }
        }
        zobristKey ^= enpassantArray[board.EnpassantFile];

        if (board.Turn) 
        {
            zobristKey ^= sideToMove;
        }
        
        zobristKey ^= castlingArray[board.CastlingData];

        return zobristKey;
    }

    static ulong NextUlong(Random rng)
    {
        byte[] buffer = new byte[8];
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }
}