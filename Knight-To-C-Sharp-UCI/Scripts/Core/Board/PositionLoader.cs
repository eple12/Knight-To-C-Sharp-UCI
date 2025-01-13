public class PositionLoader {
    Board board;
    Square[] Squares => board.Squares;
    PieceList[] PieceSquares => board.PieceSquares;
    BitboardSet BBSet => board.BBSet;

    public PositionLoader(Board _board) {
        board = _board;
    }
    
    void PlaceSinglePiece(Piece piece, Square square)
    {
        if (piece.IsNone())
        {
            return;
        }

        Squares[square] = piece;
        PieceIndexer pieceIndex = PieceUtils.GetPieceIndex(piece);

        // Piece Square Updates
        PieceSquares[pieceIndex].Add(square);
        BBSet.Add(pieceIndex, square);
    }

    public void LoadPositionFromFen(string fen)
    {
        board.Reset();

        for (int i = 0; i < 12; i++)
        {
            if (PieceSquares[i] == null)
            {
                PieceSquares[i] = new PieceList();
            }
            PieceSquares[i].Reset();
        }

        string[] splitFen = fen.Split(' ');

        if (splitFen.Length < 1)
        {
            return;
        }

        string fenboard = splitFen[0];
        int file = 0;
        int rank = 7;

        foreach(char character in fenboard)
        {
            if (character == '/')
            {
                file = 0;
                rank--;
            }
            else
            {
                if (char.IsDigit(character))
                {
                    file += (int)char.GetNumericValue(character);
                }
                else
                {
                    PlaceSinglePiece(PieceUtils.charToPiece[character], SquareUtils.Index(file, rank));
                    file++;
                }
            }
        }

        if (splitFen.Length >= 2) // Turn
        {
            board.Turn = splitFen[1] == "w";
        }

        if (splitFen.Length >= 3) // Castling
        {
            string castleFen = splitFen[2];
            board.WKCastle = false;
            board.WQCastle = false;
            board.BKCastle = false;
            board.BQCastle = false;

            int whiteKingFile = PieceSquares[PieceIndex.WhiteKing][0].File();
            int blackKingFile = PieceSquares[PieceIndex.BlackKing][0].File();

            if (castleFen == "-")
            {

            }
            else
            {
                if (whiteKingFile == SquareRepresentation.e1)
                {
                    if (castleFen.Contains('K'))
                    {
                        board.WKCastle = true;
                    }
                    if (castleFen.Contains('Q'))
                    {
                        board.WQCastle = true;
                    }
                }
                if (blackKingFile == SquareRepresentation.e1)
                {
                    if (castleFen.Contains('k'))
                    {
                        board.BKCastle = true;
                    }
                    if (castleFen.Contains('q'))
                    {
                        board.BQCastle = true;
                    }
                }
            }
        }

        if (splitFen.Length >= 4) // En-Passant
        {
            string enpassantFen = splitFen[3];
            board.EnpassantFile = 8; // Invalid Index
            if (enpassantFen == "-")
            {

            }
            else
            {
                board.EnpassantFile = SquareUtils.Index(enpassantFen).File();
            }
        }
    
        if (splitFen.Length >= 5) // Fifty-Counter
        {
            board.FiftyRuleHalfClock = Convert.ToInt32(splitFen[4]);
        }

        board.ZobristKey = Zobrist.GetZobristKey(board);

        board.AfterLoadingPosition();
    }
}