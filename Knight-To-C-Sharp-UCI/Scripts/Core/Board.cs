
public class Board
{
    public int[] position;
    public bool isWhiteTurn;

    public static readonly string initialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public List<Move> currentLegalMoves;
    public ulong currentZobristKey;


    // Piece Square Recognization
    public PieceList[] pieceSquares;

    public Stack<uint> gameStateStack;

    public static readonly uint castlingMask = 0b_0000_0000_0000_0000_0000_0000_0000_1111;
    public static readonly uint capturedPieceMask = 0b_0000_0000_0000_0000_0000_0001_1111_0000;
    public static readonly uint enpassantFileMask = 0b_0000_0000_0000_0000_0001_1110_0000_0000;
    public static readonly uint fiftyCounterMask = 0b_1111_1111_1111_1111_1110_0000_0000_0000;

    /* 
        Bit 0: White Kingside
        Bit 1: White Queenside
        Bit 2: Black Kingside
        Bit 3: Black Queenside
    */
    public byte castlingData;

    public bool isWhiteKingsideCastle
    {
        get
        {
            return (castlingData & 0b00000001) != 0;
        }
        set
        {
            if (value)
            {
                castlingData |= 1;
            }
            else
            {
                castlingData &= byte.MaxValue ^ 1;
            }
        }
    }
    public bool isWhiteQueensideCastle
    {
        get
        {
            return (castlingData & 0b00000010) != 0;
        }
        set
        {
            if (value)
            {
                castlingData |= 1 << 1;
            }
            else
            {
                castlingData &= byte.MaxValue ^ (1 << 1);
            }
        }
    }
    public bool isBlackKingsideCastle
    {
        get
        {
            return (castlingData & 0b00000100) != 0;
        }
        set
        {
            if (value)
            {
                castlingData |= 1 << 2;
            }
            else
            {
                castlingData &= byte.MaxValue ^ (1 << 2);
            }
        }
    }
    public bool isBlackQueensideCastle
    {
        get
        {
            return (castlingData & 0b00001000) != 0;
        }
        set
        {
            if (value)
            {
                castlingData |= 1 << 3;
            }
            else
            {
                castlingData &= byte.MaxValue ^ (1 << 3);
            }
        }
    }

    // En passant file; a ~ h (0 ~ 7), 8 => No en passant available.
    public int enpassantFile;

    // If 100, draw by 50-move rule. (Since it counts half-move, after 1.Nf3 Nf6 it's 2)
    public int fiftyRuleHalfClock;

    // For Threefold detection
    public Dictionary<ulong, int> positionHistory = new Dictionary<ulong, int>();

    public Board()
    {
        position = new int[64];
        isWhiteTurn = true;

        currentLegalMoves = new List<Move>();
        currentZobristKey = 0;

        pieceSquares = new PieceList[12];

        gameStateStack = new Stack<uint>();

        castlingData = 0;
        enpassantFile = 8;
        fiftyRuleHalfClock = 0;
    }

    public void PrintLargeBoard()
    {
        Console.WriteLine("###################################");
        for (int rank = 7; rank >= 0; rank--)
        {
            Console.WriteLine("+---+---+---+---+---+---+---+---+");
            for (int file = 0; file < 8; file++)
            {
                Console.Write("| ");
                Console.Write(Piece.PieceToChar(position[8 * rank + file]));
                Console.Write(" ");
            }
            Console.Write("| ");
            Console.WriteLine(rank + 1);
        }
        Console.WriteLine("+---+---+---+---+---+---+---+---+");
        Console.WriteLine("  a   b   c   d   e   f   g   h");
        Console.WriteLine("###################################");
    }

    public void PrintSmallBoard()
    {
        Console.WriteLine("#################");
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                char c = Piece.PieceToChar(position[8 * rank + file]);
                Console.Write(c == ' ' ? '~' : c);
                Console.Write(' ');
            }
            Console.WriteLine(rank + 1);
        }
        Console.WriteLine("a b c d e f g h");
        Console.WriteLine("#################");
    }

    public void AfterLoadingPosition()
    {
        currentLegalMoves = MoveGen.GenerateMoves(this);
    }

    public void Reset()
    {
        position = new int[64];
        pieceSquares = new PieceList[12];
        gameStateStack = new Stack<uint>();

        isWhiteTurn = true;
        currentLegalMoves = new List<Move>();
        currentZobristKey = 0;
        castlingData = 0;
        enpassantFile = 8;
        fiftyRuleHalfClock = 0;
        positionHistory.Clear();
    }

    public void MakeConsoleMove(string move)
    {
        if (move.Length < 4)
        {
            return;
        }

        int startSquare = Square.SquareNameToIndex(move.Substring(0, 2));
        int targetSquare = Square.SquareNameToIndex(move.Substring(2, 2));

        Move m = Move.NullMove;
        foreach (var item in currentLegalMoves)
        {
            if (item.startSquare == startSquare && item.targetSquare == targetSquare)
            {
                m = item;
                break;
            }
        }

        if (MoveFlag.IsPromotion(m.flag))
        {
            if (move.Length < 5)
            {
                return;
            }
            m.flag = MoveFlag.GetPromotionFlag(move[4]);
        }

        MakeMove(m);
        currentLegalMoves = MoveGen.GenerateMoves(this);
        PrintBoardAndMoves();
    }

    public void MakeMove(Move move)
    {
        if (move.moveValue == 0)
        {
            Console.WriteLine("NULL MOVE => Board.MakeMove()");
            return;
        }

        int startSquare = move.startSquare;
        int targetSquare = move.targetSquare;

        int movingPiece = position[startSquare];
        int capturedPiece = position[targetSquare];

        int movingPieceBitboardIndex = Piece.GetBitboardIndex(movingPiece);

        gameStateStack.Push((uint) (castlingData | capturedPiece << 4 | enpassantFile << 9 | fiftyRuleHalfClock << 13));

        fiftyRuleHalfClock++;

        // ZOBRIST UPDATE: REMOVE PREVIOUS ENP.
        currentZobristKey ^= Zobrist.enpassantArray[enpassantFile];

        // ZOBRIST UPDATE: CASTLING RIGHTS
        currentZobristKey ^= Zobrist.castlingArray[castlingData];

        // Resets fifty-move clock if a pawn moves
        if (Piece.GetType(movingPiece) == Piece.Pawn)
        {
            fiftyRuleHalfClock = 0;
        }

        // If the move is a capturing move;
        if (capturedPiece != Piece.None)
        {
            fiftyRuleHalfClock = 0;

            // ZOBRIST UPDATE
            currentZobristKey ^= Zobrist.pieceArray[Piece.GetBitboardIndex(capturedPiece), targetSquare];

            // Rook Captured -> Disable Castling;
            if (capturedPiece == (Piece.White | Piece.Rook))
            {
                if (isWhiteKingsideCastle && targetSquare == 7)
                {
                    isWhiteKingsideCastle = false;
                }
                if (isWhiteQueensideCastle && targetSquare == 0)
                {
                    isWhiteQueensideCastle = false;
                }
            }
            else if (capturedPiece == (Piece.Black | Piece.Rook))
            {
                if (isBlackKingsideCastle && targetSquare == 63)
                {
                    isBlackKingsideCastle = false;
                }
                if (isBlackQueensideCastle && targetSquare == 56)
                {
                    isBlackQueensideCastle = false;
                }
            }
        
            // PIECE SQUARE UPDATE
            pieceSquares[Piece.GetBitboardIndex(capturedPiece)].RemovePieceAtSquare(targetSquare);
        }
        else // Checks if the move is enp.
        {
            if (move.flag == MoveFlag.EnpassantCapture) // En passant
            {
                int capturedPawnSquare = Square.EnpassantFileToPawnSquare(enpassantFile, isWhiteTurn);

                position[capturedPawnSquare] = Piece.None;

                // ZOBRIST UPDATE
                currentZobristKey ^= Zobrist.pieceArray[isWhiteTurn ? BitboardIndex.BlackPawn : BitboardIndex.WhitePawn, capturedPawnSquare];

                // PIECE SQUARE UPDATE
                pieceSquares[isWhiteTurn ? BitboardIndex.BlackPawn : BitboardIndex.WhitePawn].RemovePieceAtSquare(capturedPawnSquare);
            }
        }

        enpassantFile = 8;

        if (move.flag == MoveFlag.PawnTwoForward && Square.IsValidEnpassantFile(targetSquare % 8, this)) // Enp. Square Calculation;
        {
            enpassantFile = targetSquare % 8;
        }
        
        // CASTLING
        if (move.flag == MoveFlag.Castling)
        {
            if (Piece.IsWhitePiece(movingPiece))
            {
                if (targetSquare == startSquare + 2)
                {
                    position[targetSquare + 1] = Piece.None;
                    position[targetSquare - 1] = Piece.White | Piece.Rook;

                    // ZOBRIST UPDATE
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 1];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare + 1);
                    pieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    position[targetSquare - 2] = Piece.None;
                    position[targetSquare + 1] = Piece.White | Piece.Rook;

                    // ZOBRIST UPDATE
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 2];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare - 2);
                    pieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare + 1);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    position[targetSquare + 1] = Piece.None;
                    position[targetSquare - 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST UPDATE
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 1];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare + 1);
                    pieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    position[targetSquare - 2] = Piece.None;
                    position[targetSquare + 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST UPDATE
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 2];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare - 2);
                    pieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare + 1);
                }
            }
        }

        // Castling rights
        if (movingPiece == (Piece.White | Piece.Rook))
        {
            if (startSquare == 0)
            {
                isWhiteQueensideCastle = false;
            }
            else if (startSquare == 7)
            {
                isWhiteKingsideCastle = false;
            }
        }
        if (movingPiece == (Piece.Black | Piece.Rook))
        {
            if (startSquare == 56)
            {
                isBlackQueensideCastle = false;
            }
            else if (startSquare == 63)
            {
                isBlackKingsideCastle = false;
            }
        }

        if (movingPiece == (Piece.White | Piece.King))
        {
            isWhiteKingsideCastle = false;
            isWhiteQueensideCastle = false;
        }
        if (movingPiece == (Piece.Black | Piece.King))
        {
            isBlackKingsideCastle = false;
            isBlackQueensideCastle = false;
        }

        // Move the piece
        position[targetSquare] = movingPiece;
        position[startSquare] = Piece.None;

        // ZOBRIST UPDATE
        currentZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, startSquare];
        currentZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, targetSquare];

        // PIECE SQUARE UPDATE
        pieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(startSquare);
        pieceSquares[movingPieceBitboardIndex].AddPieceAtSquare(targetSquare);
        
        // Promotion
        if (MoveFlag.IsPromotion(move.flag))
        {
            int promotionPiece = MoveFlag.GetPromotionPiece(move.flag, isWhiteTurn);
            int promotionBitboardIndex = Piece.GetBitboardIndex(promotionPiece);

            position[targetSquare] = promotionPiece;

            // ZOBRIST UPDATE: RE-CALCULATE PIECE KEY
            currentZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, targetSquare];
            currentZobristKey ^= Zobrist.pieceArray[promotionBitboardIndex, targetSquare];

            // PIECE SQUARE UPDATE
            pieceSquares[promotionBitboardIndex].AddPieceAtSquare(targetSquare);
            pieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(targetSquare);
        }


        // ZOBRIST UPDATE: ENP SQUARE
        currentZobristKey ^= Zobrist.enpassantArray[enpassantFile];

        // ZOBRIST UPDATE: CASTLING RIGHTS
        currentZobristKey ^= Zobrist.castlingArray[castlingData];

        // ZOBRIST TURN
        currentZobristKey ^= Zobrist.sideToMove;

        isWhiteTurn = !isWhiteTurn;
        
        StorePosition();
    }

    public void UnmakeMove(Move move)
    {
        positionHistory[currentZobristKey]--;
        
        isWhiteTurn = !isWhiteTurn;

        // ZOBRIST TURN
        currentZobristKey ^= Zobrist.sideToMove;

        // ZOBRIST REMOVE CASTLING
        currentZobristKey ^= Zobrist.castlingArray[castlingData];

        // ZOBRIST REMOVE ENP.
        currentZobristKey ^= Zobrist.enpassantArray[enpassantFile];

        int startSquare = move.startSquare;
        int targetSquare = move.targetSquare;

        int movingPiece = position[targetSquare];
        int movingPieceBitboardIndex = Piece.GetBitboardIndex(movingPiece);

        position[startSquare] = movingPiece;
        position[targetSquare] = Piece.None;

        // ZOBRIST PIECE
        currentZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, targetSquare];
        currentZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, startSquare];
        
        // PIECE SQUARE UPDATE
        pieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(targetSquare);
        pieceSquares[movingPieceBitboardIndex].AddPieceAtSquare(startSquare);

        uint previousGameState = gameStateStack.Pop();

        // Restore Enp. Square
        enpassantFile = (int) (previousGameState & enpassantFileMask) >> 9;

        // ZOBRIST ENP.
        currentZobristKey ^= Zobrist.enpassantArray[enpassantFile];

        // Restore Fifty-Clock
        fiftyRuleHalfClock = (int) (previousGameState & fiftyCounterMask) >> 13;

        // Restore Castling Rights
        castlingData = (byte) (previousGameState & castlingMask);
        
        // ZOBRIST CASTLING
        currentZobristKey ^= Zobrist.castlingArray[castlingData];

        int capturedPiece = (int) (previousGameState & capturedPieceMask) >> 4;

        // If capture
        if (capturedPiece != Piece.None)
        {
            position[targetSquare] = capturedPiece;

            int capturedPieceBitboardIndex = Piece.GetBitboardIndex(capturedPiece);

            // PIECE SQUARE UPDATE
            pieceSquares[capturedPieceBitboardIndex].AddPieceAtSquare(targetSquare);

            // ZOBRIST PIECE
            currentZobristKey ^= Zobrist.pieceArray[capturedPieceBitboardIndex, targetSquare];
        }

        // If En-passant
        if (move.flag == MoveFlag.EnpassantCapture)
        {
            int enpassantPawnSquare = Square.EnpassantFileToPawnSquare(enpassantFile, isWhiteTurn);
            position[enpassantPawnSquare] = (isWhiteTurn ? Piece.Black : Piece.White) | Piece.Pawn;

            int enemyPawnBitboardIndex = isWhiteTurn ? BitboardIndex.BlackPawn : BitboardIndex.WhitePawn;

            // PIECE SQUARE UPDATE
            pieceSquares[enemyPawnBitboardIndex].AddPieceAtSquare(enpassantPawnSquare);

            // ZOBRIST ENP. CAPTURE
            currentZobristKey ^= Zobrist.pieceArray[enemyPawnBitboardIndex, enpassantPawnSquare];
        }

        // If Castling
        if (move.flag == MoveFlag.Castling)
        {
            if (isWhiteTurn)
            {
                if (targetSquare == startSquare + 2)
                {
                    position[targetSquare - 1] = Piece.None;
                    position[targetSquare + 1] = Piece.White | Piece.Rook;

                    // ZOBRIST
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 1];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare - 1);
                    pieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare + 1);
                }
                else
                {
                    position[targetSquare + 1] = Piece.None;
                    position[targetSquare - 2] = Piece.White | Piece.Rook;

                    // ZOBRIST
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 2];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare + 1);
                    pieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare - 2);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    position[targetSquare - 1] = Piece.None;
                    position[targetSquare + 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 1];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare - 1);
                    pieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare + 1);
                }
                else
                {
                    position[targetSquare + 1] = Piece.None;
                    position[targetSquare - 2] = Piece.Black | Piece.Rook;

                    // ZOBRIST
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];
                    currentZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 2];

                    // PIECE SQUARE UPDATE
                    pieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare + 1);
                    pieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare - 2);
                }
            }
        }

        if (MoveFlag.IsPromotion(move.flag)) // If Promotion
        {
            position[startSquare] = (isWhiteTurn ? Piece.White : Piece.Black) | Piece.Pawn;

            // ZOBRIST
            currentZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, startSquare];
            currentZobristKey ^= Zobrist.pieceArray[isWhiteTurn ? BitboardIndex.WhitePawn : BitboardIndex.BlackPawn, startSquare];

            // PIECE SQUARE UPDATE
            pieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(startSquare);
            pieceSquares[isWhiteTurn ? BitboardIndex.WhitePawn : BitboardIndex.BlackPawn].AddPieceAtSquare(startSquare);
        }
    }
    
    public ulong Perft(int depth)
    {
        if (depth == 0)
        {
            return 1;
        }

        ulong nodes = 0;
        List<Move> legalMoves = MoveGen.GenerateMoves(this);
        
        if (depth == 1)
        {
            return (ulong) legalMoves.Count;
        }

        foreach (Move move in legalMoves)
        {
            MakeMove(move);

            nodes += Perft(depth - 1);

            UnmakeMove(move);
        }

        return nodes;
    }

    void StorePosition()
    {
        if (positionHistory.ContainsKey(currentZobristKey))
        {
            positionHistory[currentZobristKey]++;
        }
        else
        {
            positionHistory.Add(currentZobristKey, 1);
        }
    }

    void PlaceSinglePiece(int piece, int square)
    {
        if (piece == Piece.None)
        {
            return;
        }

        position[square] = piece;

        // Add square; (Piece Squares)
        pieceSquares[Piece.GetBitboardIndex(piece)].AddPieceAtSquare(square);
    }

    public void LoadPositionFromFen(string fen)
    {
        Reset();

        for (int i = 0; i < 12; i++)
        {
            if (pieceSquares[i] == null)
            {
                pieceSquares[i] = new PieceList();
            }
            pieceSquares[i].squares = new int[16]; // Resets piece squares to 0;
            pieceSquares[i].count = 0;
        }

        string[] splitFen = fen.Split(' ');

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
                    PlaceSinglePiece(Piece.charToPiece[character], Square.FileRankToSquareIndex(file, rank));
                    file++;
                }
            }
        }

        // Turn;
        isWhiteTurn = splitFen[1] == "w";

        // Castle;
        string castleFen = splitFen[2];
        isWhiteKingsideCastle = false;
        isWhiteQueensideCastle = false;
        isBlackKingsideCastle = false;
        isBlackQueensideCastle = false;

        if (castleFen == "-")
        {
            // No CASTLING!
        }
        else
        {
            if (castleFen.Contains('K'))
            {
                isWhiteKingsideCastle = true;
            }
            if (castleFen.Contains('Q'))
            {
                isWhiteQueensideCastle = true;
            }
            if (castleFen.Contains('k'))
            {
                isBlackKingsideCastle = true;
            }
            if (castleFen.Contains('q'))
            {
                isBlackQueensideCastle = true;
            }
        }
    
        // En passant square;
        string enpassantFen = splitFen[3];
        enpassantFile = 8; // Invalid Index;
        if (enpassantFen == "-")
        {

        }
        else
        {
            enpassantFile = Square.SquareNameToIndex(enpassantFen) % 8;
        }

        // Fifty-counter;
        fiftyRuleHalfClock = splitFen.Length >= 5 && char.IsDigit(splitFen[4].ToCharArray()[0]) ? Convert.ToInt32(splitFen[4]) : 0;

        currentZobristKey = Zobrist.GetZobristKey(this);
        positionHistory[currentZobristKey] = 1;

        AfterLoadingPosition();
    }

    public void PrintBoardAndMoves()
    {
        if (ProgramSettings.useLargeBoard)
        {
            PrintLargeBoard();
        }
        else
        {
            PrintSmallBoard();
        }
        PrintCastlingData();
        Move.PrintMoveList(currentLegalMoves);
    }

    public void PrintCastlingData()
    {
        Console.WriteLine("Castling // " + (isWhiteKingsideCastle ? "WK " : " ") + (isWhiteQueensideCastle ? "WQ " : " ") + (isBlackKingsideCastle ? "BK " : " ") + (isBlackQueensideCastle ? "BQ " : ""));
    }





}