public class Board
{
    public MoveGenerator MoveGen;

    public Square[] Squares;
    public bool Turn;

    public static readonly string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public Move[] LegalMoves;
    public ulong ZobristKey;


    // Piece Square Recognization
    public PieceList[] PieceSquares;
    public BitboardSet BBSet;

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

    // En-Passant file: a ~ h (0 ~ 7)
    // 8: En-Passant is not available
    public int EnpassantFile;

    // If 100, draw by 50-move rule. (Since it counts half-move, after 1.Nf3 Nf6 it's 2)
    public int FiftyRuleHalfClock;

    // For Threefold detection
    public HashSet<ulong> PlayedPositions;

    // In-Check Cache value
    bool InCheckCachedValue;
    bool HasCachedInCheckValue;

    // Position Loader
    PositionLoader positionLoader;

    public Board()
    {
        Squares = new int[64];
        Turn = true;

        LegalMoves = new Move[MoveGenerator.MaxMoves];
        ZobristKey = 0;

        PieceSquares = new PieceList[12];
        BBSet = new BitboardSet();

        GameStack = new Stack<uint>();

        CastlingData = 0;
        EnpassantFile = 8;
        FiftyRuleHalfClock = 0;
        
        MoveGen = new MoveGenerator(this);

        PlayedPositions = new ();

        positionLoader = new (this);

        InCheckCachedValue = false;
        HasCachedInCheckValue = false;

        LoadInitialPosition();
    }
    
    // Reset
    public void Reset()
    {
        Squares = new int[64];
        PieceSquares = new PieceList[12];
        BBSet = new BitboardSet();
        GameStack = new Stack<uint>();

        Turn = true;
        LegalMoves = new Move[MoveGenerator.MaxMoves];
        ZobristKey = 0;
        CastlingData = 0;
        EnpassantFile = 8;
        FiftyRuleHalfClock = 0;
        PlayedPositions.Clear();

        HasCachedInCheckValue = false;
    }

    // Making Moves
    public void MakeMove(Move move, bool inSearch = false)
    {
        if (move.IsNull())
        {
            Console.WriteLine("WARNING: Could not make the move since it is a NULL move.");
            return;
        }

        Square startSquare = move.startSquare;
        Square targetSquare = move.targetSquare;

        Piece movingPiece = Squares[startSquare];
        Piece capturedPiece = Squares[targetSquare];

        PieceIndexer movingPieceIndex = movingPiece.PieceIndexer();

        GameStack.Push((uint) (CastlingData | capturedPiece << 4 | EnpassantFile << 9 | FiftyRuleHalfClock << 13));

        FiftyRuleHalfClock++;

        // Zobrist Update: Remove previous En-Passant file data
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        // Zobrist Update: Castling
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        // Resets fifty-move clock if a pawn moves
        if (movingPiece.Type() == PieceUtils.Pawn)
        {
            FiftyRuleHalfClock = 0;
        }

        // If the move is a capturing move;
        if (!capturedPiece.IsNone())
        {
            PieceIndexer capturedPieceIndex = capturedPiece.PieceIndexer();
            FiftyRuleHalfClock = 0;

            // Zobrist Update: Piece
            ZobristKey ^= Zobrist.pieceArray[capturedPieceIndex, targetSquare];

            // If a rook is captured, update the castling data
            if (capturedPieceIndex == PieceIndex.WhiteRook)
            {
                if (WKCastle && targetSquare == SquareRepresentation.h1)
                {
                    WKCastle = false;
                }
                if (WQCastle && targetSquare == SquareRepresentation.a1)
                {
                    WQCastle = false;
                }
            }
            else if (capturedPieceIndex == PieceIndex.BlackRook)
            {
                if (BKCastle && targetSquare == SquareRepresentation.h8)
                {
                    BKCastle = false;
                }
                if (BQCastle && targetSquare == SquareRepresentation.a8)
                {
                    BQCastle = false;
                }
            }
        
            // Piece Square Updates
            PieceSquares[capturedPieceIndex].Remove(targetSquare);
            BBSet.Remove(capturedPieceIndex, targetSquare);
        }
        else // Check if this move is En-Passant
        {
            if (move.flag == MoveFlag.EnpassantCapture) // En passant
            {
                Square capturedPawnSquare = SquareUtils.EnpassantStartMid(EnpassantFile, Turn);

                Squares[capturedPawnSquare] = PieceUtils.None;

                PieceIndexer enemyPawnIndex = PieceIndex.MakePawn(!Turn);

                // Zobrist Update: Piece
                ZobristKey ^= Zobrist.pieceArray[enemyPawnIndex, capturedPawnSquare];

                // Piece Square Updates
                PieceSquares[enemyPawnIndex].Remove(capturedPawnSquare);
                BBSet.Remove(enemyPawnIndex, capturedPawnSquare);
            }
        }

        EnpassantFile = 8;

        // Update the En-Passant file if possible
        if (move.flag == MoveFlag.PawnTwoForward && SquareUtils.IsEnpassantPossible(targetSquare.File(), this))
        {
            EnpassantFile = targetSquare.File();
        }
        
        // Handle Castling
        if (move.flag == MoveFlag.Castling)
        {
            if (movingPiece.IsWhite())
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare + 1] = PieceUtils.None;
                    Squares[targetSquare - 1] = PieceUtils.White | PieceUtils.Rook;

                    // Zobrist Update: Piece
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 1];

                    // Piece Square Updates
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare - 1);

                    // Bitboard Updates
                    BBSet.Remove(PieceIndex.WhiteRook, targetSquare + 1);
                    BBSet.Add(PieceIndex.WhiteRook, targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    Squares[targetSquare - 2] = PieceUtils.None;
                    Squares[targetSquare + 1] = PieceUtils.White | PieceUtils.Rook;

                    // Zobrist Update: Piece
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 2];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];

                    // Piece Square Updates
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare - 2);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare + 1);

                    // Bitboard Updates
                    BBSet.Remove(PieceIndex.WhiteRook, targetSquare - 2);
                    BBSet.Add(PieceIndex.WhiteRook, targetSquare + 1);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare + 1] = PieceUtils.None;
                    Squares[targetSquare - 1] = PieceUtils.Black | PieceUtils.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare - 1);

                    // Bitboard
                    BBSet.Remove(PieceIndex.BlackRook, targetSquare + 1);
                    BBSet.Add(PieceIndex.BlackRook, targetSquare - 1);
                }
                else if (targetSquare == startSquare - 2)
                {
                    Squares[targetSquare - 2] = PieceUtils.None;
                    Squares[targetSquare + 1] = PieceUtils.Black | PieceUtils.Rook;

                    // ZOBRIST UPDATE
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 2];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];

                    // PIECE SQUARE UPDATE
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare - 2);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare + 1);

                    // Bitboard
                    BBSet.Remove(PieceIndex.BlackRook, targetSquare - 2);
                    BBSet.Add(PieceIndex.BlackRook, targetSquare + 1);
                }
            }
        }

        // Update Castling
        if (movingPieceIndex == PieceIndex.WhiteRook)
        {
            if (startSquare == SquareRepresentation.a1)
            {
                WQCastle = false;
            }
            else if (startSquare == SquareRepresentation.h1)
            {
                WKCastle = false;
            }
        }
        else if (movingPieceIndex == PieceIndex.BlackRook)
        {
            if (startSquare == SquareRepresentation.a8)
            {
                BQCastle = false;
            }
            else if (startSquare == SquareRepresentation.h8)
            {
                BKCastle = false;
            }
        }

        if (movingPieceIndex == PieceIndex.WhiteKing)
        {
            WKCastle = false;
            WQCastle = false;
        }
        else if (movingPieceIndex == PieceIndex.BlackKing)
        {
            BKCastle = false;
            BQCastle = false;
        }

        // Move the piece
        Squares[targetSquare] = movingPiece;
        Squares[startSquare] = PieceUtils.None;

        // Zobrist Update: Piece
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, startSquare];
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, targetSquare];

        // Piece Square Updates
        PieceSquares[movingPieceIndex].Remove(startSquare);
        PieceSquares[movingPieceIndex].Add(targetSquare);

        // Bitboard Updates
        BBSet.Remove(movingPieceIndex, startSquare);
        BBSet.Add(movingPieceIndex, targetSquare);
        
        // Promotion
        if (MoveFlag.IsPromotion(move.flag))
        {
            Piece promotionPiece = MoveFlag.GetPromotionPiece(move.flag, Turn);
            PieceIndexer promotionPieceIndex = promotionPiece.PieceIndexer();

            Squares[targetSquare] = promotionPiece;

            // Zobrist Update: Recalculate the piece
            ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, targetSquare];
            ZobristKey ^= Zobrist.pieceArray[promotionPieceIndex, targetSquare];

            // Piece Square Updates
            PieceSquares[promotionPieceIndex].Add(targetSquare);
            PieceSquares[movingPieceIndex].Remove(targetSquare);

            // Bitboard Updates
            BBSet.Remove(movingPieceIndex, targetSquare);
            BBSet.Add(promotionPieceIndex, targetSquare);
        }

        // Zobrist Update: En-Passant file
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        // Zobrist Update: Castling
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        // Zobrist Update: Turn
        ZobristKey ^= Zobrist.sideToMove;

        Turn = !Turn;

        // Move made in-game: Add to the played-positions
        if (!inSearch) {
            PlayedPositions.Add(ZobristKey);
        }

        HasCachedInCheckValue = false;
    }
    public void UnmakeMove(Move move)
    {
        Turn = !Turn;

        // Zobrist Update: Turn
        ZobristKey ^= Zobrist.sideToMove;

        // Zobrist Update: Castling
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        // Zobrist Update: En-Passant file
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        Square startSquare = move.startSquare;
        Square targetSquare = move.targetSquare;

        Piece movingPiece = Squares[targetSquare];
        PieceIndexer movingPieceIndex = movingPiece.PieceIndexer();

        Squares[startSquare] = movingPiece;
        Squares[targetSquare] = PieceUtils.None;

        // Zobrist Update: Piece
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, targetSquare];
        ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, startSquare];
        
        // Piece Square Updates
        PieceSquares[movingPieceIndex].Remove(targetSquare);
        PieceSquares[movingPieceIndex].Add(startSquare);

        // Bitboard Updates
        BBSet.Remove(movingPieceIndex, targetSquare);
        BBSet.Add(movingPieceIndex, startSquare);

        uint previousGameState = GameStack.Pop();

        // Restore En-Passant file
        EnpassantFile = (int) (previousGameState & EnpassantFileMask) >> 9;

        // Zobrist Update: En-Passant file
        ZobristKey ^= Zobrist.enpassantArray[EnpassantFile];

        // Restore Fifty-Clock
        FiftyRuleHalfClock = (int) (previousGameState & FiftyCounterMask) >> 13;

        // Restore Castling Data
        CastlingData = (byte) (previousGameState & CastlingMask);
        
        // Zobrist Update: Castling
        ZobristKey ^= Zobrist.castlingArray[CastlingData];

        Piece capturedPiece = (Piece) (previousGameState & CapturedPieceMask) >> 4;

        // If the move was a capture
        if (!capturedPiece.IsNone())
        {
            Squares[targetSquare] = capturedPiece;

            PieceIndexer capturedPieceIndex = capturedPiece.PieceIndexer();

            // Piece Square Updates
            PieceSquares[capturedPieceIndex].Add(targetSquare);

            BBSet.Add(capturedPieceIndex, targetSquare);

            // Zobrist Update: Piece
            ZobristKey ^= Zobrist.pieceArray[capturedPieceIndex, targetSquare];
        }

        // If En-passant
        if (move.flag == MoveFlag.EnpassantCapture)
        {
            Square enpassantPawnSquare = SquareUtils.EnpassantStartMid(EnpassantFile, Turn);
            Squares[enpassantPawnSquare] = (Turn ? PieceUtils.Black : PieceUtils.White) | PieceUtils.Pawn;

            PieceIndexer enemyPawnIndex = Turn ? PieceIndex.BlackPawn : PieceIndex.WhitePawn;

            // Piece Square Updates
            PieceSquares[enemyPawnIndex].Add(enpassantPawnSquare);

            BBSet.Add(enemyPawnIndex, enpassantPawnSquare);

            // Zobrist Update: Piece
            ZobristKey ^= Zobrist.pieceArray[enemyPawnIndex, enpassantPawnSquare];
        }

        // If the move was a castling
        if (move.flag == MoveFlag.Castling)
        {
            if (Turn)
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare - 1] = PieceUtils.None;
                    Squares[targetSquare + 1] = PieceUtils.White | PieceUtils.Rook;

                    // Zobrist Update: Piece
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];

                    // Piece Square Updates
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare - 1);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare + 1);

                    // Bitboard Updates
                    BBSet.Remove(PieceIndex.WhiteRook, targetSquare - 1);
                    BBSet.Add(PieceIndex.WhiteRook, targetSquare + 1);
                }
                else
                {
                    Squares[targetSquare + 1] = PieceUtils.None;
                    Squares[targetSquare - 2] = PieceUtils.White | PieceUtils.Rook;

                    // Zobrist Update: Piece
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.WhiteRook, targetSquare - 2];

                    // Piece Square Updates
                    PieceSquares[PieceIndex.WhiteRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.WhiteRook].Add(targetSquare - 2);

                    // Bitboard Updates
                    BBSet.Remove(PieceIndex.WhiteRook, targetSquare + 1);
                    BBSet.Add(PieceIndex.WhiteRook, targetSquare - 2);
                }
            }
            else
            {
                if (targetSquare == startSquare + 2)
                {
                    Squares[targetSquare - 1] = PieceUtils.None;
                    Squares[targetSquare + 1] = PieceUtils.Black | PieceUtils.Rook;

                    // Zobrist Update: Piece
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];

                    // Piece Square Updates
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare - 1);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare + 1);

                    // Bitboard Updates
                    BBSet.Remove(PieceIndex.BlackRook, targetSquare - 1);
                    BBSet.Add(PieceIndex.BlackRook, targetSquare + 1);
                }
                else
                {
                    Squares[targetSquare + 1] = PieceUtils.None;
                    Squares[targetSquare - 2] = PieceUtils.Black | PieceUtils.Rook;

                    // Zobrist Update: Piece
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare + 1];
                    ZobristKey ^= Zobrist.pieceArray[PieceIndex.BlackRook, targetSquare - 2];

                    // Piece Square Updates
                    PieceSquares[PieceIndex.BlackRook].Remove(targetSquare + 1);
                    PieceSquares[PieceIndex.BlackRook].Add(targetSquare - 2);

                    // Bitboard Updates
                    BBSet.Remove(PieceIndex.BlackRook, targetSquare + 1);
                    BBSet.Add(PieceIndex.BlackRook, targetSquare - 2);
                }
            }
        }

        // If the move was a promotion
        if (MoveFlag.IsPromotion(move.flag))
        {
            Squares[startSquare] = (Turn ? PieceUtils.White : PieceUtils.Black) | PieceUtils.Pawn;

            int pawnIndex = Turn ? PieceIndex.WhitePawn : PieceIndex.BlackPawn;

            // Zobrist Update: Piece
            ZobristKey ^= Zobrist.pieceArray[movingPieceIndex, startSquare];
            ZobristKey ^= Zobrist.pieceArray[pawnIndex, startSquare];

            // Piece Square Updates
            PieceSquares[movingPieceIndex].Remove(startSquare);
            PieceSquares[pawnIndex].Add(startSquare);

            // Bitboard Updates
            BBSet.Remove(movingPieceIndex, startSquare);
            BBSet.Add(pawnIndex, startSquare);
        }

        HasCachedInCheckValue = false;
    }
    
    public void MakeConsoleMove(string move)
    {
        if (move.Length < 4)
        {
            return;
        }

        MakeMove(LegalMoves.FindMove(move));
        UpdateLegalMoves();
    }
    public void MakeConsoleMove(Move move)
    {
        MakeMove(move);
        UpdateLegalMoves();
    }
    public void UpdateLegalMoves()
    {
        LegalMoves = MoveGen.GenerateMoves().ToArray();
    }

    // Check Calculation
    [Inline]
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
        int friendlyKingSquare = PieceSquares[PieceIndex.MakeKing(Turn)][0];
        ulong blockers = BBSet[PieceIndex.WhiteAll] | BBSet[PieceIndex.BlackAll];

        int enemyBishop = PieceIndex.MakeBishop(!Turn);
        int enemyRook = PieceIndex.MakeRook(!Turn);
        int enemyQueen = PieceIndex.MakeQueen(!Turn);

        ulong enemyStraightSliders = BBSet[enemyRook] | BBSet[enemyQueen];
        ulong enemyDiagonalSliders = BBSet[enemyBishop] | BBSet[enemyQueen];

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

        ulong enemyKnights = BBSet[PieceIndex.MakeKnight(!Turn)];
        if ((PreComputedMoveGenData.KnightMap[friendlyKingSquare] & enemyKnights) != 0)
        {
            return true;
        }

        ulong enemyPawns = BBSet[PieceIndex.MakePawn(!Turn)];
        ulong pawnAttackMask = Turn ? PreComputedMoveGenData.WhitePawnAttackMap[friendlyKingSquare] : PreComputedMoveGenData.BlackPawnAttackMap[friendlyKingSquare];
        if ((pawnAttackMask & enemyPawns) != 0)
        {
            return true;
        }

        return false;
    }

    // Loading Positions
    public void LoadInitialPosition()
    {
        LoadPositionFromFen(InitialFen);
    }
    public void LoadPositionFromFen(string fen)
    {
        positionLoader.LoadPositionFromFen(fen);
    }
    public void AfterLoadingPosition()
    {
        UpdateLegalMoves();
        HasCachedInCheckValue = false;
    }

    // Printing
    public void PrintLargeBoard()
    {
        Console.WriteLine("###################################");
        for (int rank = 7; rank >= 0; rank--)
        {
            Console.WriteLine("+---+---+---+---+---+---+---+---+");
            for (int file = 0; file < 8; file++)
            {
                Console.Write("| ");
                Console.Write(PieceUtils.PieceToChar(Squares[8 * rank + file]));
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
                char c = PieceUtils.PieceToChar(Squares[8 * rank + file]);
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
                char c = PieceUtils.PieceToChar(Squares[8 * rank + file]);
                s += c == ' ' ? '~' : c;
                s += ' ';
            }
            s += '\n';
        }
        return s;
    }
    public void PrintBoardAndMoves()
    {
        PrintLargeBoard();
        PrintCastlingData();
        Console.WriteLine($"Total {LegalMoves.Length}");
        LegalMoves.Print();
    }
    public void PrintCastlingData()
    {
        Console.WriteLine($"Castling: {(WKCastle ? "K" : "")}{(WQCastle ? "Q" : "")}{(BKCastle ? "k" : "")}{(BQCastle ? "q" : "")}");
    }
}