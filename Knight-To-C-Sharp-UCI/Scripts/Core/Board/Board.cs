
public class Board
{
    public bool Loaded;
    public MoveGenerator MoveGen;

    public int[] Squares;
    public bool Turn;

    public static readonly string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public Move[] LegalMoves;
    public ulong ZobristKey;


    // Piece Square Recognization
    public PieceList[] PieceSquares;
    public Bitboard BitboardSet;

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

    // In-Check Cache value
    bool InCheckCachedValue;
    bool HasCachedInCheckValue;

    public Board()
    {
        Loaded = false;

        Squares = new int[64];
        Turn = true;

        LegalMoves = new Move[MoveGenerator.MaxMoves];
        ZobristKey = 0;

        PieceSquares = new PieceList[12];
        BitboardSet = new Bitboard();

        GameStack = new Stack<uint>();

        CastlingData = 0;
        EnpassantFile = 8;
        FiftyRuleHalfClock = 0;
        
        MoveGen = new MoveGenerator(this);

        InCheckCachedValue = false;
        HasCachedInCheckValue = false;
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
    public string GetSmallBoard()
    {
        string s = "";
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                char c = Piece.PieceToChar(Squares[8 * rank + file]);
                s += (c == ' ' ? '~' : c);
                s += (' ');
            }
            // Console.WriteLine(rank + 1);
            s += '\n';
        }
        return s;
    }

    public void AfterLoadingPosition()
    {
        UpdateLegalMoves();
        Loaded = true;
        HasCachedInCheckValue = false;
    }

    public void Reset()
    {
        Loaded = false;

        Squares = new int[64];
        PieceSquares = new PieceList[12];
        BitboardSet = new Bitboard();
        GameStack = new Stack<uint>();

        Turn = true;
        LegalMoves = new Move[MoveGenerator.MaxMoves];
        ZobristKey = 0;
        CastlingData = 0;
        EnpassantFile = 8;
        FiftyRuleHalfClock = 0;
        PositionHistory.Clear();

        HasCachedInCheckValue = false;
    }

    public void MakeConsoleMove(string move)
    {
        if (move.Length < 4)
        {
            return;
        }

        Move m = Move.FindMove(LegalMoves, move);

        MakeMove(m);
        UpdateLegalMoves();
        // PrintBoardAndMoves();
    }

    public void MakeConsoleMove(Move move)
    {
        MakeMove(move);
        UpdateLegalMoves();
        // PrintBoardAndMoves();
    }

    public void MakeMove(Move move)
    {
        if (move.moveValue == 0)
        {
            Console.WriteLine("WARNING: Could not make the move since it is a NULL move.");
            return;
        }

        int startSquare = move.startSquare;
        int targetSquare = move.targetSquare;

        int movingPiece = Squares[startSquare];
        int capturedPiece = Squares[targetSquare];

        int movingPieceIndex = Piece.GetPieceIndex(movingPiece);

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
            int capturedPieceIndex = Piece.GetPieceIndex(capturedPiece);
            FiftyRuleHalfClock = 0;

            // ZOBRIST UPDATE
            ZobristKey ^= Zobrist.pieceArray[Piece.GetPieceIndex(capturedPiece), targetSquare];

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
            PieceSquares[capturedPieceIndex].Remove(targetSquare);
            BitboardSet.Remove(capturedPieceIndex, targetSquare);
        }
        else // Checks if the move is enp.
        {
            if (move.flag == MoveFlag.EnpassantCapture) // En passant
            {
                int capturedPawnSquare = Square.EnpassantAvailablePawnIndex(EnpassantFile, Turn);

                Squares[capturedPawnSquare] = Piece.None;

                int enemyPawnIndex = PieceIndex.MakePawn(!Turn);

                // ZOBRIST UPDATE
                ZobristKey ^= Zobrist.pieceArray[enemyPawnIndex, capturedPawnSquare];

                // PIECE SQUARE UPDATE
                PieceSquares[enemyPawnIndex].Remove(capturedPawnSquare);
                BitboardSet.Remove(enemyPawnIndex, capturedPawnSquare);
            }
        }

        EnpassantFile = 8;

        if (move.flag == MoveFlag.PawnTwoForward && Square.IsValidEnpassantFile(targetSquare % 8, this)) // Enp. Square Calculation;
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
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare - 1);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.WhiteRook, targetSquare + 1);
                    BitboardSet.Add(PieceIndex.WhiteRook, targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    Squares[targetSquare - 2] = Piece.None;
                    Squares[targetSquare + 1] = Piece.White | Piece.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 2];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare - 2);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare + 1);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.WhiteRook, targetSquare - 2);
                    BitboardSet.Add(PieceIndex.WhiteRook, targetSquare + 1);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare + 1] = Piece.None;
                    Squares[targetSquare - 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare - 1);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.BlackRook, targetSquare + 1);
                    BitboardSet.Add(PieceIndex.BlackRook, targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    Squares[targetSquare - 2] = Piece.None;
                    Squares[targetSquare + 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 2];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare - 2);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare + 1);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.BlackRook, targetSquare - 2);
                    BitboardSet.Add(PieceIndex.BlackRook, targetSquare + 1);
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
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, startSquare];
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, targetSquare];

        // PIECE SQUARE UPDATE
        PieceSquares[movingPieceIndex].Remove(startSquare);
        PieceSquares[movingPieceIndex].Add(targetSquare);

        // Bitboard
        BitboardSet.Remove(movingPieceIndex, startSquare);
        BitboardSet.Add(movingPieceIndex, targetSquare);
        
        // Promotion
        if (MoveFlag.IsPromotion(move.flag))
        {
            int promotionPiece = MoveFlag.GetPromotionPiece(move.flag, Turn);
            int promotionPieceIndex = Piece.GetPieceIndex(promotionPiece);

            Squares[targetSquare] = promotionPiece;

            // ZOBRIST UPDATE: RE-CALCULATE PIECE KEY
            ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, targetSquare];
            ZobristKey ^= Zobrist.pieceArray[promotionPieceIndex, targetSquare];

            // PIECE SQUARE UPDATE
            PieceSquares[promotionPieceIndex].Add(targetSquare);
            PieceSquares[movingPieceIndex].Remove(targetSquare);

            // Bitboard
            BitboardSet.Remove(movingPieceIndex, targetSquare);
            BitboardSet.Add(promotionPieceIndex, targetSquare);
        }


        // ZOBRIST UPDATE: ENP SQUARE
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        // ZOBRIST UPDATE: CASTLING RIGHTS
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        // ZOBRIST TURN
        ZobristKey ^= Zobrist.sideToMove;

        Turn = !Turn;
        
        // StorePosition();
        // if (RepetitionData.Contains(ZobristKey))
        // {
        //     RepetitionVerify.Add(ZobristKey);
        // }
        // else
        // {
        //     RepetitionData.Add(ZobristKey);
        // }
        if (PositionHistory.ContainsKey(ZobristKey))
        {
            PositionHistory[ZobristKey]++;
        }
        else
        {
            PositionHistory.Add(ZobristKey, 1);
        }

        HasCachedInCheckValue = false;
    }

    public void UnmakeMove(Move move)
    {
        // try
        // {
        // PositionHistory[ZobristKey]--;
        // if (RepetitionVerify.Contains(ZobristKey))
        // {
        //     RepetitionVerify.Remove(ZobristKey);
        // }
        // else
        // {
        //     RepetitionData.Remove(ZobristKey);
        // }
        PositionHistory[ZobristKey]--;
        
        // }
        // catch (Exception)
        // {
        //     PrintSmallBoard();
        //     throw;
        // }
        
        
        
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
        int movingPieceIndex = Piece.GetPieceIndex(movingPiece);

        Squares[startSquare] = movingPiece;
        Squares[targetSquare] = Piece.None;

        // ZOBRIST PIECE
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, targetSquare];
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, startSquare];
        
        // PIECE SQUARE UPDATE
        PieceSquares[movingPieceIndex].Remove(targetSquare);
        PieceSquares[movingPieceIndex].Add(startSquare);

        // Bitboard
        BitboardSet.Remove(movingPieceIndex, targetSquare);
        BitboardSet.Add(movingPieceIndex, startSquare);

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

            int capturedPieceIndex = Piece.GetPieceIndex(capturedPiece);

            // PIECE SQUARE UPDATE
            PieceSquares[capturedPieceIndex].Add(targetSquare);

            BitboardSet.Add(capturedPieceIndex, targetSquare);

            // ZOBRIST PIECE
            ZobristKey ^= Zobrist.pieceArray[capturedPieceIndex, targetSquare];
        }

        // If En-passant
        if (move.flag == MoveFlag.EnpassantCapture)
        {
            int enpassantPawnSquare = Square.EnpassantAvailablePawnIndex(EnpassantFile, Turn);
            Squares[enpassantPawnSquare] = (Turn ? Piece.Black : Piece.White) | Piece.Pawn;

            int enemyPawnIndex = Turn ? PieceIndex.BlackPawn : PieceIndex.WhitePawn;

            // PIECE SQUARE UPDATE
            PieceSquares[enemyPawnIndex].Add(enpassantPawnSquare);

            BitboardSet.Add(enemyPawnIndex, enpassantPawnSquare);

            // ZOBRIST ENP. CAPTURE
            ZobristKey ^= Zobrist.pieceArray[enemyPawnIndex, enpassantPawnSquare];
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
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare - 1);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare + 1);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.WhiteRook, targetSquare - 1);
                    BitboardSet.Add(PieceIndex.WhiteRook, targetSquare + 1);
                }
                else
                {
                    Squares[targetSquare + 1] = Piece.None;
                    Squares[targetSquare - 2] = Piece.White | Piece.Rook;

                    // ZOBRIST
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 2];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare - 2);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.WhiteRook, targetSquare + 1);
                    BitboardSet.Add(PieceIndex.WhiteRook, targetSquare - 2);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare - 1] = Piece.None;
                    Squares[targetSquare + 1] = Piece.Black | Piece.Rook;

                    // ZOBRIST
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare - 1);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare + 1);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.BlackRook, targetSquare - 1);
                    BitboardSet.Add(PieceIndex.BlackRook, targetSquare + 1);
                }
                else
                {
                    Squares[targetSquare + 1] = Piece.None;
                    Squares[targetSquare - 2] = Piece.Black | Piece.Rook;

                    // ZOBRIST
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 2];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare - 2);

                    // Bitboard
                    BitboardSet.Remove(PieceIndex.BlackRook, targetSquare + 1);
                    BitboardSet.Add(PieceIndex.BlackRook, targetSquare - 2);
                }
            }
        }

        if (MoveFlag.IsPromotion(move.flag)) // If Promotion
        {
            Squares[startSquare] = (Turn ? Piece.White : Piece.Black) | Piece.Pawn;

            int pawnIndex = Turn ? PieceIndex.WhitePawn : PieceIndex.BlackPawn;

            // ZOBRIST
            ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, startSquare];
            ZobristKey ^= Zobrist.pieceArray[pawnIndex, startSquare];

            // PIECE SQUARE UPDATE
            PieceSquares[movingPieceIndex].Remove(startSquare);
            PieceSquares[pawnIndex].Add(startSquare);

            // Bitboard
            BitboardSet.Remove(movingPieceIndex, startSquare);
            BitboardSet.Add(pawnIndex, startSquare);
        }

        HasCachedInCheckValue = false;
    }
    
    public void UpdateLegalMoves()
    {
        LegalMoves = MoveGen.GenerateMoves().ToArray();
    }
    // void StorePosition()
    // {
    //     if (PositionHistory.ContainsKey(ZobristKey))
    //     {
    //         PositionHistory[ZobristKey]++;
    //     }
    //     else
    //     {
    //         PositionHistory.Add(ZobristKey, 1);
    //     }
    // }

    void PlaceSinglePiece(int piece, int square)
    {
        if (piece == Piece.None)
        {
            return;
        }

        Squares[square] = piece;
        int pieceIndex = Piece.GetPieceIndex(piece);

        // Add square; (Piece Squares)
        PieceSquares[pieceIndex].Add(square);
        BitboardSet.Add(pieceIndex, square);
    }

    public void LoadPositionFromFen(string fen)
    {
        // Console.WriteLine(fen);
        Reset();

        for (int i = 0; i < 12; i++)
        {
            if (PieceSquares[i] == null)
            {
                PieceSquares[i] = new PieceList();
            }
            PieceSquares[i].Squares = new int[16]; // Resets piece squares to 0;
            PieceSquares[i].Count = 0;
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

        if (splitFen.Length >= 2) // Turn
        {
            Turn = splitFen[1] == "w";
        }

        if (splitFen.Length >= 3) // Castling
        {
            // Castle;
            string castleFen = splitFen[2];
            WKCastle = false;
            WQCastle = false;
            BKCastle = false;
            BQCastle = false;

            int whiteKingFile = PieceSquares[PieceIndex.WhiteKing].Squares[0] % 8;
            int blackKingFile = PieceSquares[PieceIndex.BlackKing].Squares[0] % 8;

            if (castleFen == "-")
            {
                // No CASTLING!
            }
            else
            {
                if (whiteKingFile == 4)
                {
                    if (castleFen.Contains('K'))
                    {
                        WKCastle = true;
                    }
                    if (castleFen.Contains('Q'))
                    {
                        WQCastle = true;
                    }
                }
                if (blackKingFile == 4)
                {
                    if (castleFen.Contains('k'))
                    {
                        BKCastle = true;
                    }
                    if (castleFen.Contains('q'))
                    {
                        BQCastle = true;
                    }
                }
            }
        }

        if (splitFen.Length >= 4) // En passant
        {
            string enpassantFen = splitFen[3];
            EnpassantFile = 8; // Invalid Index;
            if (enpassantFen == "-")
            {

            }
            else
            {
                EnpassantFile = Square.Index(enpassantFen) % 8;
            }
        }
    
        if (splitFen.Length >= 5) // Fifty-Counter
        {
            FiftyRuleHalfClock = Convert.ToInt32(splitFen[4]);
        }

        ZobristKey = Zobrist.GetZobristKey(this);
        PositionHistory.Add(ZobristKey, 1);
        // RepetitionData.Add(ZobristKey);

        AfterLoadingPosition();
    }

    public void PrintBoardAndMoves()
    {
        PrintLargeBoard();
        PrintCastlingData();
        Console.WriteLine($"Total {LegalMoves.Length}");
        Move.PrintMoveList(LegalMoves);
    }

    public void PrintCastlingData()
    {
        Console.WriteLine("Castling // " + (WKCastle ? "WK " : " ") + (WQCastle ? "WQ " : " ") + (BKCastle ? "BK " : " ") + (BQCastle ? "BQ " : ""));
    }

    public bool InCheck()
    {
        if (HasCachedInCheckValue)
        {
            return InCheckCachedValue;
        }
        else
        {
            InCheckCachedValue = CalculateInCheck();
            HasCachedInCheckValue = true;
            return InCheckCachedValue;
        }
    }
    bool CalculateInCheck()
    {
        int friendlyKingSquare = PieceSquares[PieceIndex.MakeKing(Turn)].Squares[0];
        ulong blockers = BitboardSet.Bitboards[PieceIndex.WhiteAll] | BitboardSet.Bitboards[PieceIndex.BlackAll];

        int enemyBishop = PieceIndex.MakeBishop(!Turn);
        int enemyRook = PieceIndex.MakeRook(!Turn);
        int enemyQueen = PieceIndex.MakeQueen(!Turn);

        ulong enemyStraightSliders = BitboardSet.Bitboards[enemyRook] | BitboardSet.Bitboards[enemyQueen];
        ulong enemyDiagonalSliders = BitboardSet.Bitboards[enemyBishop] | BitboardSet.Bitboards[enemyQueen];

        if (enemyStraightSliders != 0)
        {
            ulong rookAttacks = Magic.GetRookAttacks(friendlyKingSquare, blockers);
            if ((rookAttacks & enemyStraightSliders) != 0)
            {
                return true;
            }
        }
        if (enemyDiagonalSliders != 0)
        {
            ulong bishopAttacks = Magic.GetBishopAttacks(friendlyKingSquare, blockers);
            if ((bishopAttacks & enemyDiagonalSliders) != 0)
            {
                return true;
            }
        }

        ulong enemyKnights = BitboardSet.Bitboards[PieceIndex.MakeKnight(!Turn)];
        if ((PreComputedMoveGenData.KnightMap[friendlyKingSquare] & enemyKnights) != 0)
        {
            return true;
        }

        ulong enemyPawns = BitboardSet.Bitboards[PieceIndex.MakePawn(!Turn)];
        ulong pawnAttackMask = Turn ? PreComputedMoveGenData.whitePawnAttackMap[friendlyKingSquare] : PreComputedMoveGenData.blackPawnAttackMap[friendlyKingSquare];
        if ((pawnAttackMask & enemyPawns) != 0)
        {
            return true;
        }

        return false;
    }

}