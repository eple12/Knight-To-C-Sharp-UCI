
public static class PreComputedMoveGenData // Pre-Computed Data to speed move generation up
{
    public static int[,] NumSquaresToEdge = new int[64, 8];
    public static Dictionary<int, int> OffsetToNumEdgeIndex = new Dictionary<int, int> {
        {1, 0}, {8, 1}, {-1, 2}, {-8, 3}, {9, 4}, {7, 5}, {-9, 6}, {-7, 7}
    };
    public static int[] Directions = {1, 8, -1, -8, 9, 7, -9, -7};

    // Pre-Computed Bitboard (ulong)
    public static ulong[] KnightMap = new ulong[64];
    public static ulong[] KingMap = new ulong[64];
    public static ulong[] whitePawnAttackMap = new ulong[64];
    public static ulong[] blackPawnAttackMap = new ulong[64];

    // Pre-Computed Squares (Index)
    public static List<int>[] knightSquares = new List<int>[64];

    // Pre-Computed Direction Lookup Table
    // Index : directionLookup[targetSquare - startSquare + 63]
    // Values with index of impossible pin ray such as 126 are invalid (targetSquare = 63, startSquare = 0 -> THIS CANNOT BE A PIN RAY)
    public static int[] DirectionLookup = new int[127];

    // Direction-Ray Masks
    // [Direction Index , From Square] => Ray Mask from Square in Direction
    // Does not include the square
    public static ulong[,] DirRayMask = new ulong[8, 64];

    // Aligned Rays
    // [ SquareA , SquareB ] => SquareA to SquareB Ray
    public static ulong[,] AlignMask = new ulong[64, 64];

    // Move Generation Bitboards
    public static ulong WhiteKingSideCastlingMask = (ulong) 0b11 << 5;
    public static ulong WhiteQueenSideCastlingMask = (ulong) 0b11 << 2;
    public static ulong WhiteQueenSideCastlingBlockMask = (ulong) 0b111 << 1;
    public static ulong BlackKingSideCastlingMask = (ulong) 0b11 << 61;
    public static ulong BlackQueenSideCastlingMask = (ulong) 0b11 << 58;
    public static ulong BlackQueenSideCastlingBlockMask = (ulong) 0b111 << 57;


    static PreComputedMoveGenData()
    {
        GenerateNumSquaresToEdge();
        GenerateMaps();
        GenerateDirectionLookup();
        GenerateDirRayMask();
        GenerateAlignMask();
    }

    public static void GenerateNumSquaresToEdge()
    {
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                int numR = 7 - file;
                int numU = 7 - rank;
                int numL = file;
                int numD = rank;

                int squareIndex = 8 * rank + file;

                NumSquaresToEdge[squareIndex, 0] = numR;
                NumSquaresToEdge[squareIndex, 1] = numU;
                NumSquaresToEdge[squareIndex, 2] = numL;
                NumSquaresToEdge[squareIndex, 3] = numD;

                NumSquaresToEdge[squareIndex, 4] = Math.Min(numR, numU);
                
                NumSquaresToEdge[squareIndex, 5] = Math.Min(numL, numU);
                
                NumSquaresToEdge[squareIndex, 6] = Math.Min(numL, numD);
                
                NumSquaresToEdge[squareIndex, 7] = Math.Min(numD, numR);
            }
        }
    }
    public static int NumEdgeIndex(int offset)
    {
        return OffsetToNumEdgeIndex[offset];
    }

    public static void GenerateMaps()
    {
        GenerateKnightMap();
        GenerateKingMap();
        GeneratePawnAttackMap();
    }

    static void GenerateKnightMap()
    {
        for (int square = 0; square < 64; square++)
        {
            knightSquares[square] = new List<int>();

            int file = square % 8;
            int rank = square / 8;

            if (file < 6 && rank < 7)
            {
                KnightMap[square] |= (ulong) 1 << square + 10;
                knightSquares[square].Add(square + 10);
            }
            if (file < 7 && rank < 6)
            {
                KnightMap[square] |= (ulong) 1 << square + 17;
                knightSquares[square].Add(square + 17);
            }
            if (file > 0 && rank < 6)
            {
                KnightMap[square] |= (ulong) 1 << square + 15;
                knightSquares[square].Add(square + 15);
            }
            if (file > 1 && rank < 7)
            {
                KnightMap[square] |= (ulong) 1 << square + 6;
                knightSquares[square].Add(square + 6);
            }

            if (file > 1 && rank > 0)
            {
                KnightMap[square] |= (ulong) 1 << square - 10;
                knightSquares[square].Add(square - 10);
            }
            if (file > 0 && rank > 1)
            {
                KnightMap[square] |= (ulong) 1 << square - 17;
                knightSquares[square].Add(square - 17);
            }
            if (file < 7 && rank > 1)
            {
                KnightMap[square] |= (ulong) 1 << square - 15;
                knightSquares[square].Add(square - 15);
            }
            if (file < 6 && rank > 0)
            {
                KnightMap[square] |= (ulong) 1 << square - 6;
                knightSquares[square].Add(square - 6);
            }
        }
    }

    static void GenerateKingMap()
    {
        for (int square = 0; square < 64; square++)
        {
            int file = square % 8;
            int rank = square / 8;

            if (file < 7)
            {
                KingMap[square] |= (ulong) 1 << square + 1;
            }
            if (file > 0)
            {
                KingMap[square] |= (ulong) 1 << square - 1;
            }
            if (rank < 7)
            {
                if (file < 7)
                {
                    KingMap[square] |= (ulong) 1 << square + 9;
                }
                
                KingMap[square] |= (ulong) 1 << square + 8;
                
                if (file > 0)
                {
                    KingMap[square] |= (ulong) 1 << square + 7;
                }
            }
            if (rank > 0)
            {
                if (file < 7)
                {
                    KingMap[square] |= (ulong) 1 << square - 7;
                }

                KingMap[square] |= (ulong) 1 << square - 8;

                if (file > 0)
                {
                    KingMap[square] |= (ulong) 1 << square - 9;
                }
            }
        }
    }

    static void GeneratePawnAttackMap()
    {
        for (int square = 0; square < 64; square++)
        {
            int file = square % 8;
            int rank = square / 8;

            if (rank < 7)
            {
                if (file < 7)
                {
                    whitePawnAttackMap[square] |= (ulong) 1 << (square + 9);
                }
                if (file > 0)
                {
                    whitePawnAttackMap[square] |= (ulong) 1 << (square + 7);
                }
            }
            if (rank > 0)
            {
                if (file < 7)
                {
                    blackPawnAttackMap[square] |= (ulong) 1 << (square - 7);
                }
                if (file > 0)
                {
                    blackPawnAttackMap[square] |= (ulong) 1 << (square - 9);
                }
            }
        }
    }

    static void GenerateDirectionLookup()
    {
        for (int i = 0; i < 127; i++)
        {
            int offset = i - 63;
            int absOffset = Math.Abs(offset);
            int absDir = 1;

            if (absOffset % 9 == 0)
            {
                absDir = 9;
            }
            else if (absOffset % 8 == 0)
            {
                absDir = 8;
            }
            else if (absOffset % 7 == 0)
            {
                absDir = 7;
            }

            DirectionLookup[i] = absDir * Math.Sign(offset);
        }
    }
    static void GenerateDirRayMask()
    {
        for (int dirIndex = 0; dirIndex < 8; dirIndex++)
        {
            for (int square = 0; square < 64; square++)
            {
                int dirOffset = Directions[dirIndex];
                int numEdge = NumSquaresToEdge[square, dirIndex];
                ulong mask = 0;
                
                for (int dst = 1; dst <= numEdge; dst++)
                {
                    int thisSquare = square + dst * dirOffset;

                    mask |= (ulong) 1 << thisSquare;
                }

                DirRayMask[dirIndex, square] = mask;
            }
        }
    }
    static void GenerateAlignMask()
    {
        for (int squareA = 0; squareA < 64; squareA++)
        {
            for (int squareB = 0; squareB < 64; squareB++)
            {
                int aFile = squareA % 8;
                int aRank = squareA / 8;

                int deltaFile = (squareB % 8) - aFile;
                int deltaRank = (squareB / 8) - aRank;

                int signFile = Math.Sign(deltaFile);
                int signRank = Math.Sign(deltaRank);

                for (int dst = -7; dst <= 7; dst++)
                {
                    int thisFile = aFile + (signFile * dst);
                    int thisRank = aRank + (signRank * dst);

                    if (Square.IsValidSquare(thisFile, thisRank))
                    {
                        AlignMask[squareA, squareB] |= (ulong) 1 << (thisFile + 8 * thisRank);
                    }
                }
            }
        }
    }


}
