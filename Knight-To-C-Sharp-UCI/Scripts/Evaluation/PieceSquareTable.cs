public static class PieceSquareTable
{
    public static readonly int[] Pawn = {
		 0,   0,   0,   0,   0,   0,   0,   0,
		50,  50,  50,  50,  50,  50,  50,  50,
		10,  10,  20,  30,  30,  20,  10,  10,
		 5,   5,  10,  25,  25,  10,   5,   5,
		 0,   0,   0,  20,  20,   0,   0,   0,
		 5,  -5, -10,   0,   0, -10,  -5,   5,
		 5,  10,  10, -20, -20,  10,  10,   5,
		 0,   0,   0,   0,   0,   0,   0,   0
	};
    public static readonly int[] Knight = {
		-50,-40,-30,-30,-30,-30,-40,-50,
		-40,-20,  0,  0,  0,  0,-20,-40,
		-30,  0, 10, 15, 15, 10,  0,-30,
		-30,  5, 15, 20, 20, 15,  5,-30,
		-30,  0, 15, 20, 20, 15,  0,-30,
		-30,  5, 10, 15, 15, 10,  5,-30,
		-40,-20,  0,  5,  5,  0,-20,-40,
		-50,-40,-30,-30,-30,-30,-40,-50,
	};
    public static readonly int[] Bishop =  {
		-20,-10,-10,-10,-10,-10,-10,-20,
		-10,  0,  0,  0,  0,  0,  0,-10,
		-10,  0,  5, 10, 10,  5,  0,-10,
		-10,  5,  5, 10, 10,  5,  5,-10,
		-10,  0, 10, 10, 10, 10,  0,-10,
		-10, 10, 10, 10, 10, 10, 10,-10,
		-10,  5,  0,  0,  0,  0,  5,-10,
		-20,-10,-10,-10,-10,-10,-10,-20,
	};
	public static readonly int[] Rook =  {
         0,  0,  0,  0,  0,  0,  0,  0,
        15, 20, 20, 20, 20, 20, 20, 15,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -15, 0,  0,  0,  0,  0,  0, -15,
        -20,-20,  0, 15, 15,  0,-20,-20
    };
    public static readonly int[] Queen =  {
		-20,-10,-10, -5, -5,-10,-10,-20,
		-10,  0,  0,  0,  0,  0,  0,-10,
		-10,  0,  5,  5,  5,  5,  0,-10,
		-5,  0,  5,  5,  5,  5,  0, -5,
		0,  0,  5,  5,  5,  5,  0, -5,
		-10,  5,  5,  5,  5,  5,  0,-10,
		-10,  0,  5,  0,  0,  0,  0,-10,
		-20,-10,-10, -5, -5,-10,-10,-20
	};
	public static readonly int[] King = 
    {
        -80, -70, -70, -70, -70, -70, -70, -80, 
        -60, -60, -60, -60, -60, -60, -60, -60, 
        -40, -50, -50, -60, -60, -50, -50, -40, 
        -30, -40, -40, -50, -50, -40, -40, -30, 
        -20, -30, -30, -40, -40, -30, -30, -20, 
        -10, -20, -20, -20, -20, -20, -20, -10, 
         10,  10,  -5,  -5,  -5,  -5,  10,  10, 
         10,  15,  10,  -5,   0,  -5,  15,  10
    };
    
    // Endgame Tables
    public static readonly int[] PawnEnd = {
		 0,   0,   0,   0,   0,   0,   0,   0,
		80,  80,  80,  80,  80,  80,  80,  80,
		50,  50,  50,  50,  50,  50,  50,  50,
		30,  30,  30,  30,  30,  30,  30,  30,
		20,  20,  20,  20,  20,  20,  20,  20,
		10,  10,  10,  10,  10,  10,  10,  10,
		10,  10,  10,  10,  10,  10,  10,  10,
		 0,   0,   0,   0,   0,   0,   0,   0
	};
    public static readonly int[] KingEnd = { 
        -20, -10, -10, -10, -10, -10, -10, -20, 
         -5,   0,   5,   5,   5,   5,   0,  -5, 
        -10,  -5,  20,  30,  30,  20,  -5, -10, 
        -15, -10,  35,  45,  45,  35, -10, -15, 
        -20, -15,  30,  40,  40,  30, -15, -20, 
        -25, -20,  20,  25,  25,  20, -20, -25, 
        -30, -25,   0,   0,   0,   0, -25, -30, 
        -50, -30, -30, -30, -30, -30, -30, -50 
    };
	
	// Read the flipped square since the table is written in black's perspective
	[Inline]
    public static int Read(int[] table, Square square, bool white)
    {
        return table[white ? square.FlipRank() : square];
    }

	public static int ReadTableFromPiece(Piece piece, Square square, bool color, Board board) {
		int type = piece.Type();
		
		if (type == PieceUtils.Knight) {
			return Read(Knight, square, color);
		}
		
		if (type == PieceUtils.Bishop) {
			return Read(Bishop, square, color);
		}

		if (type == PieceUtils.Rook) {
			return Read(Rook, square, color);
		}

		if (type == PieceUtils.Queen) {
			return Read(Queen, square, color);
		}

		if (type == PieceUtils.Pawn) {
			double endgameWeight = Evaluation.GetEndgameWeight(board, color);
			return (int) (Read(Pawn, square, color) * (1 - endgameWeight) + Read(PawnEnd, square, color) * endgameWeight);
		}

		if (type == PieceUtils.King) {
			double endgameWeight = Evaluation.GetEndgameWeight(board, color);
			return (int) (Read(King, square, color) * (1 - endgameWeight) + Read(KingEnd, square, color) * endgameWeight);
		}

		return 0;
	}
}