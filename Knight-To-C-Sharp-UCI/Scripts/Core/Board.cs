
public class Board
{
    public bool Loaded;
    public MoveGenerator MoveGen;

    public int[] Squares;
    public bool Turn;

    public static readonly string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public List<Move> LegalMoves;
    public ulong ZobristKey;


    // Piece Square Recognization
    public PieceList[] PieceSquares;

    public Stack<uint> GameStack;

    public static readonly uint CastlingMask = 0b_0000_0000_0000_0000_0000_0000_0000_1111;
    public static readonly uint CapturedPieceMask = 0b_0000_0000_0000_0000_0000_0001_1111_0000;
    public static readonly uint EnpassantFileMask = 0b_0000_0000_0000_0000_0001_1110_0000_0000;
    public static readonly uint FiftyCounterMask = 0b_1111_1111_1111_1111_1110_0000_0000_0000;

    /* 
        Bit 0: White Kingside
        Bit 1: White Queenside
        Bit 2: Black Kingside
        Bit 3: Black Queenside
    */
    public byte CastlingData;

    public bool WKCastle
    {
        get
        {
            return (CastlingData & 0b00000001) != 0;
        }
        set
        {
            if (value)
            {
                CastlingData |= 1;
            }
            else
            {
                CastlingData &= byte.MaxValue ^ 1;
            }
        }
    }
    public bool WQCastle
    {
        get
        {
            return (CastlingData & 0b00000010) != 0;
        }
        set
        {
            if (value)
            {
                CastlingData |= 1 << 1;
            }
            else
            {
                CastlingData &= byte.MaxValue ^ (1 << 1);
            }
        }
    }
    public bool BKCastle
    {
        get
        {
            return (CastlingData & 0b00000100) != 0;
        }
        set
        {
            if (value)
            {
                CastlingData |= 1 << 2;
            }
            else
            {
                CastlingData &= byte.MaxValue ^ (1 << 2);
            }
        }
    }
    public bool BQCastle
    {
        get
        {
            return (CastlingData & 0b00001000) != 0;
        }
        set
        {
            if (value)
            {
                CastlingData |= 1 << 3;
            }
            else
            {
                CastlingData &= byte.MaxValue ^ (1 << 3);
            }
        }
    }

    // En passant file; a ~ h (0 ~ 7), 8 => No en passant available.
    public int EnpassantFile;

    // If 100, draw by 50-move rule. (Since it counts half-move, after 1.Nf3 Nf6 it's 2)
    public int FiftyRuleHalfClock;

    // For Threefold detection
    public Dictionary<ulong, int> PositionHistory = new Dictionary<ulong, int>();

    public Board()
    {
        MoveGen = new MoveGenerator(this);
        Loaded = false;

        Squares = new int[64];
        Turn = true;

        LegalMoves = new List<Move>();
        ZobristKey = 0;

        PieceSquares = new PieceList[12];

        GameStack = new Stack<uint>();

        CastlingData = 0;
        EnpassantFile = 8;
        FiftyRuleHalfClock = 0;
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
                Console.Write(Piece.PieceToChar(Squares[8 * rank + file]));
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
                char c = Piece.PieceToChar(Squares[8 * rank + file]);
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
        LegalMoves = MoveGen.GenerateMoves();
        Loaded = true;
    }

    public void Reset()
    {
        Loaded = false;

        Squares = new int[64];
        PieceSquares = new PieceList[12];
        GameStack = new Stack<uint>();

        Turn = true;
        LegalMoves = new List<Move>();
        ZobristKey = 0;
        CastlingData = 0;
        EnpassantFile = 8;
        FiftyRuleHalfClock = 0;
        PositionHistory.Clear();
    }

    public void MakeConsoleMove(string move)
    {
        if (move.Length < 4)
        {
            return;
        }

        int startSquare = Square.Index(move.Substring(0, 2));
        int targetSquare = Square.Index(move.Substring(2, 2));

        Move m = Move.NullMove;
        foreach (var item in LegalMoves)
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
        LegalMoves = MoveGen.GenerateMoves();
        // PrintBoardAndMoves();
    }

    public void MakeConsoleMove(Move move)
    {
        MakeMove(move);
        LegalMoves = MoveGen.GenerateMoves();
        // PrintBoardAndMoves();
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

        int movingPiece = Squares[startSquare];
        int capturedPiece = Squares[targetSquare];

        int movingPieceBitboardIndex = Piece.GetBitboardIndex(movingPiece);

        GameStack.Push((uint) (CastlingData | capturedPiece << 4 | EnpassantFile << 9 | FiftyRuleHalfClock << 13));

        FiftyRuleHalfClock++;

        // ZOBRIST UPDATE: REMOVE PREVIOUS ENP.
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        // ZOBRIST UPDATE: CASTLING RIGHTS
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        // Resets fifty-move clock if a pawn moves
        if (Piece.GetType(movingPiece) == Piece.Pawn)
        {
            FiftyRuleHalfClock = 0;
        }

        // If the move is a capturing move;
        if (capturedPiece != Piece.None)
        {
            FiftyRuleHalfClock = 0;

            // ZOBRIST UPDATE
            ZobristKey ^= Zobrist.pieceArray[Piece.GetBitboardIndex(capturedPiece), targetSquare];

            // Rook Captured -> Disable Castling;
            if (capturedPiece == (Piece.White | Piece.Rook))
            {
                if (WKCastle && targetSquare == 7)
                {
                    WKCastle = false;
                }
                if (WQCastle && targetSquare == 0)
                {
                    WQCastle = false;
                }
            }
            else if (capturedPiece == (Piece.Black | Piece.Rook))
            {
                if (BKCastle && targetSquare == 63)
                {
                    BKCastle = false;
                }
                if (BQCastle && targetSquare == 56)
                {
                    BQCastle = false;
                }
            }
        
            // PIECE SQUARE UPDATE
            PieceSquares[Piece.GetBitboardIndex(capturedPiece)].RemovePieceAtSquare(targetSquare);
        }
        else // Checks if the move is enp.
        {
            if (move.flag == MoveFlag.EnpassantCapture) // En passant
            {
                int capturedPawnSquare = global::Square.EnpassantAvailablePawnIndex(EnpassantFile, Turn);

                Squares[capturedPawnSquare] = Piece.None;

                // ZOBRIST UPDATE
                ZobristKey ^= Zobrist.pieceArray[Turn ? BitboardIndex.BlackPawn : BitboardIndex.WhitePawn, capturedPawnSquare];

                // PIECE SQUARE UPDATE
                PieceSquares[Turn ? BitboardIndex.BlackPawn : BitboardIndex.WhitePawn].RemovePieceAtSquare(capturedPawnSquare);
            }
        }

        EnpassantFile = 8;

        if (move.flag == MoveFlag.PawnTwoForward && global::Square.IsValidEnpassantFile(targetSquare % 8, this)) // Enp. Square Calculation;
        {
            EnpassantFile = targetSquare % 8;
        }
        
        // CASTLING
        if (move.flag == MoveFlag.Castling)
        {
            if (Piece.IsWhitePiece(movingPiece))
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare + 1] = Piece.None;
                    Squares[targetSquare - 1] = Piece.White | Piece.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare + 1);
                    PieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    Squares[targetSquare - 2] = Piece.None;
                    Squares[targetSquare + 1] = Piece.White | Piece.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 2];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare - 2);
                    PieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare + 1);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare + 1] = Piece.None;
                    Squares[targetSquare - 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare + 1);
                    PieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    Squares[targetSquare - 2] = Piece.None;
                    Squares[targetSquare + 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 2];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare - 2);
                    PieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare + 1);
                }
            }
        }

        // Castling rights
        if (movingPiece == (Piece.White | Piece.Rook))
        {
            if (startSquare == 0)
            {
                WQCastle = false;
            }
            else if (startSquare == 7)
            {
                WKCastle = false;
            }
        }
        if (movingPiece == (Piece.Black | Piece.Rook))
        {
            if (startSquare == 56)
            {
                BQCastle = false;
            }
            else if (startSquare == 63)
            {
                BKCastle = false;
            }
        }

        if (movingPiece == (Piece.White | Piece.King))
        {
            WKCastle = false;
            WQCastle = false;
        }
        if (movingPiece == (Piece.Black | Piece.King))
        {
            BKCastle = false;
            BQCastle = false;
        }

        // Move the piece
        Squares[targetSquare] = movingPiece;
        Squares[startSquare] = Piece.None;

        // ZOBRIST UPDATE
        ZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, startSquare];
        ZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, targetSquare];

        // PIECE SQUARE UPDATE
        PieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(startSquare);
        PieceSquares[movingPieceBitboardIndex].AddPieceAtSquare(targetSquare);
        
        // Promotion
        if (MoveFlag.IsPromotion(move.flag))
        {
            int promotionPiece = MoveFlag.GetPromotionPiece(move.flag, Turn);
            int promotionBitboardIndex = Piece.GetBitboardIndex(promotionPiece);

            Squares[targetSquare] = promotionPiece;

            // ZOBRIST UPDATE: RE-CALCULATE PIECE KEY
            ZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, targetSquare];
            ZobristKey ^= Zobrist.pieceArray[promotionBitboardIndex, targetSquare];

            // PIECE SQUARE UPDATE
            PieceSquares[promotionBitboardIndex].AddPieceAtSquare(targetSquare);
            PieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(targetSquare);
        }


        // ZOBRIST UPDATE: ENP SQUARE
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        // ZOBRIST UPDATE: CASTLING RIGHTS
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        // ZOBRIST TURN
        ZobristKey ^= Zobrist.sideToMove;

        Turn = !Turn;
        
        StorePosition();
    }

    public void UnmakeMove(Move move)
    {
        PositionHistory[ZobristKey]--;
        
        Turn = !Turn;

        // ZOBRIST TURN
        ZobristKey ^= Zobrist.sideToMove;

        // ZOBRIST REMOVE CASTLING
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        // ZOBRIST REMOVE ENP.
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        int startSquare = move.startSquare;
        int targetSquare = move.targetSquare;

        int movingPiece = Squares[targetSquare];
        int movingPieceBitboardIndex = Piece.GetBitboardIndex(movingPiece);

        Squares[startSquare] = movingPiece;
        Squares[targetSquare] = Piece.None;

        // ZOBRIST PIECE
        ZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, targetSquare];
        ZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, startSquare];
        
        // PIECE SQUARE UPDATE
        PieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(targetSquare);
        PieceSquares[movingPieceBitboardIndex].AddPieceAtSquare(startSquare);

        uint previousGameState = GameStack.Pop();

        // Restore Enp. Square
        EnpassantFile = (int) (previousGameState & EnpassantFileMask) >> 9;

        // ZOBRIST ENP.
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        // Restore Fifty-Clock
        FiftyRuleHalfClock = (int) (previousGameState & FiftyCounterMask) >> 13;

        // Restore Castling Rights
        CastlingData = (byte) (previousGameState & CastlingMask);
        
        // ZOBRIST CASTLING
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        int capturedPiece = (int) (previousGameState & CapturedPieceMask) >> 4;

        // If capture
        if (capturedPiece != Piece.None)
        {
            Squares[targetSquare] = capturedPiece;

            int capturedPieceBitboardIndex = Piece.GetBitboardIndex(capturedPiece);

            // PIECE SQUARE UPDATE
            PieceSquares[capturedPieceBitboardIndex].AddPieceAtSquare(targetSquare);

            // ZOBRIST PIECE
            ZobristKey ^= Zobrist.pieceArray[capturedPieceBitboardIndex, targetSquare];
        }

        // If En-passant
        if (move.flag == MoveFlag.EnpassantCapture)
        {
            int enpassantPawnSquare = global::Square.EnpassantAvailablePawnIndex(EnpassantFile, Turn);
            Squares[enpassantPawnSquare] = (Turn ? Piece.Black : Piece.White) | Piece.Pawn;

            int enemyPawnBitboardIndex = Turn ? BitboardIndex.BlackPawn : BitboardIndex.WhitePawn;

            // PIECE SQUARE UPDATE
            PieceSquares[enemyPawnBitboardIndex].AddPieceAtSquare(enpassantPawnSquare);

            // ZOBRIST ENP. CAPTURE
            ZobristKey ^= Zobrist.pieceArray[enemyPawnBitboardIndex, enpassantPawnSquare];
        }

        // If Castling
        if (move.flag == MoveFlag.Castling)
        {
            if (Turn)
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare - 1] = Piece.None;
                    Squares[targetSquare + 1] = Piece.White | Piece.Rook;

                    // ZOBRIST
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 1];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare - 1);
                    PieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare + 1);
                }
                else
                {
                    Squares[targetSquare + 1] = Piece.None;
                    Squares[targetSquare - 2] = Piece.White | Piece.Rook;

                    // ZOBRIST
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.WhiteRook, targetSquare - 2];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.WhiteRook].RemovePieceAtSquare(targetSquare + 1);
                    PieceSquares[BitboardIndex.WhiteRook].AddPieceAtSquare(targetSquare - 2);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare - 1] = Piece.None;
                    Squares[targetSquare + 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 1];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare - 1);
                    PieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare + 1);
                }
                else
                {
                    Squares[targetSquare + 1] = Piece.None;
                    Squares[targetSquare - 2] = Piece.Black | Piece.Rook;

                    // ZOBRIST
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[BitboardIndex.BlackRook, targetSquare - 2];

                    // PIECE SQUARE UPDATE
                    PieceSquares[BitboardIndex.BlackRook].RemovePieceAtSquare(targetSquare + 1);
                    PieceSquares[BitboardIndex.BlackRook].AddPieceAtSquare(targetSquare - 2);
                }
            }
        }

        if (MoveFlag.IsPromotion(move.flag)) // If Promotion
        {
            Squares[startSquare] = (Turn ? Piece.White : Piece.Black) | Piece.Pawn;

            // ZOBRIST
            ZobristKey ^= Zobrist.pieceArray[movingPieceBitboardIndex, startSquare];
            ZobristKey ^= Zobrist.pieceArray[Turn ? BitboardIndex.WhitePawn : BitboardIndex.BlackPawn, startSquare];

            // PIECE SQUARE UPDATE
            PieceSquares[movingPieceBitboardIndex].RemovePieceAtSquare(startSquare);
            PieceSquares[Turn ? BitboardIndex.WhitePawn : BitboardIndex.BlackPawn].AddPieceAtSquare(startSquare);
        }
    }
    
    public ulong Perft(int depth)
    {
        if (depth == 0)
        {
            return 1;
        }

        ulong nodes = 0;
        List<Move> legalMoves = MoveGen.GenerateMoves();
        
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
        if (PositionHistory.ContainsKey(ZobristKey))
        {
            PositionHistory[ZobristKey]++;
        }
        else
        {
            PositionHistory.Add(ZobristKey, 1);
        }
    }

    // hello this is a random modification i make to test this thing

    void PlaceSinglePiece(int piece, int square)
    {
        if (piece == Piece.None)
        {
            return;
        }

        Squares[square] = piece;

        // Add square; (Piece Squares)
        PieceSquares[Piece.GetBitboardIndex(piece)].AddPieceAtSquare(square);
    }

    public void LoadPositionFromFen(string fen)
    {
        Reset();

        for (int i = 0; i < 12; i++)
        {
            if (PieceSquares[i] == null)
            {
                PieceSquares[i] = new PieceList();
            }
            PieceSquares[i].squares = new int[16]; // Resets piece squares to 0;
            PieceSquares[i].count = 0;
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
                    PlaceSinglePiece(Piece.charToPiece[character], Square.Index(file, rank));
                    file++;
                }
            }
        }

        if (splitFen.Length < 2)
        {
            return;
        }

        // Turn;
        Turn = splitFen[1] == "w";

        if (splitFen.Length < 3)
        {
            return;
        }

        // Castle;
        string castleFen = splitFen[2];
        WKCastle = false;
        WQCastle = false;
        BKCastle = false;
        BQCastle = false;

        if (castleFen == "-")
        {
            // No CASTLING!
        }
        else
        {
            if (castleFen.Contains('K'))
            {
                WKCastle = true;
            }
            if (castleFen.Contains('Q'))
            {
                WQCastle = true;
            }
            if (castleFen.Contains('k'))
            {
                BKCastle = true;
            }
            if (castleFen.Contains('q'))
            {
                BQCastle = true;
            }
        }

        if (splitFen.Length < 4)
        {
            return;
        }
    
        // En passant square;
        string enpassantFen = splitFen[3];
        EnpassantFile = 8; // Invalid Index;
        if (enpassantFen == "-")
        {

        }
        else
        {
            EnpassantFile = Square.Index(enpassantFen) % 8;
        }

        if (splitFen.Length < 5)
        {
            return;
        }

        // Fifty-counter;
        FiftyRuleHalfClock = Convert.ToInt32(splitFen[4]);

        ZobristKey = Zobrist.GetZobristKey(this);
        PositionHistory[ZobristKey] = 1;

        AfterLoadingPosition();
    }

    public void PrintBoardAndMoves()
    {
        // if (ProgramSettings.useLargeBoard)
        // {
        //     PrintLargeBoard();
        // }
        // else
        // {
        //     PrintSmallBoard();
        // }
        PrintLargeBoard();
        PrintCastlingData();
        Move.PrintMoveList(LegalMoves);
    }

    public void PrintCastlingData()
    {
        Console.WriteLine("Castling // " + (WKCastle ? "WK " : " ") + (WQCastle ? "WQ " : " ") + (BKCastle ? "BK " : " ") + (BQCastle ? "BQ " : ""));
    }





}