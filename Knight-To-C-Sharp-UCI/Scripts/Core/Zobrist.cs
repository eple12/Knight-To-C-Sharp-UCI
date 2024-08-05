
using System;

public static class Zobrist
{
    static readonly int seed = 29426028;
    static System.Random rng = new System.Random(seed);

    public static readonly ulong[,] pieceArray = new ulong[12, 64];
    public static readonly ulong[] castlingArray = new ulong[16]; // No, K, Q, KQ (4 Possible states for each side) -> 4 * 2
    public static readonly ulong[] enpassantArray = new ulong[9]; // index 8 => No ENP
    public static readonly ulong sideToMove = NextUlong(rng);

    public static void GenerateZobristTable() // CALLED INITIALLY;
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

    public static ulong GetZobristKey(Board board) // Called Only Once (after LoadPositionFromFen())
    {
        // var sw = System.Diagnostics.Stopwatch.StartNew();

        ulong zobristKey = 0;

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            // Returns invalid BitboardIndex if failed to find piece
            // int piece = Bitboard.GetPieceBitboardIndexAtSquare(bitboards, squareIndex);
            int pieceBitboardIndex = Piece.GetBitboardIndex(board.position[squareIndex]);

            if (pieceBitboardIndex != BitboardIndex.Invalid)
            {
                zobristKey ^= pieceArray[pieceBitboardIndex, squareIndex];
            }
        }

        // Debug.Log(IsValidEnp(bitboards, !isWhiteTurn) ? (enpassantSquareIndex % 8) : 8);
        // Debug.Log(IsValidEnp(board, true));
        // Debug.Log(enpassantSquareIndex);
        zobristKey ^= enpassantArray[board.enpassantFile];

        if (board.isWhiteTurn) 
        {
            zobristKey ^= sideToMove;
        }

        // PrintCastleData();

        zobristKey ^= castlingArray[board.castlingData];
        
        // Debug.Log((double) sw.ElapsedTicks / 10000);
        return zobristKey;
    }

    static ulong NextUlong(System.Random rng)
    {
        byte[] buffer = new byte[8];
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }

    // public static bool IsValidEnp(Board board, bool reverseTurn = false)
    // {
    //     if (board.enpassantFile == 8)
    //     {
    //         return false;
    //     }
    //     if (reverseTurn ? !board.isWhiteTurn : board.isWhiteTurn)
    //     {
    //         if (board.enpassantSquareIndex % 8 < 7 && 
    //         board.position[board.enpassantSquareIndex + 9] == (Piece.Black | Piece.Pawn))
    //         {
    //             return true;
    //         }
    //         if (board.enpassantSquareIndex % 8 > 0 && 
    //         board.position[board.enpassantSquareIndex + 7] == (Piece.Black | Piece.Pawn))
    //         {
    //             return true;
    //         }
    //     }
    //     else
    //     {
    //         if (board.enpassantSquareIndex % 8 < 7 && 
    //         board.position[board.enpassantSquareIndex - 7] == (Piece.White | Piece.Pawn))
    //         {
    //             return true;
    //         }
    //         if (board.enpassantSquareIndex % 8 > 0 && 
    //         board.position[board.enpassantSquareIndex - 9] == (Piece.White | Piece.Pawn))
    //         {
    //             return true;
    //         }
    //     }

    //     return false;
    // }


}