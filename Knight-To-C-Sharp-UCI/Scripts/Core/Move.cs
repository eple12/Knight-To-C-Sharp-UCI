
public struct Move
{
    // moveValue => Stores data (startSquare, targetSquare, moveFlag)
    public ushort moveValue;

    const ushort startSquareMask = 0b0000000000111111;
    const ushort targetSquareMask = 0b0000111111000000;
    const ushort flagMask = 0b1111000000000000;

    public static Move NullMove
    {
        get
        {
            return new Move(0);
        }
    }

    public int startSquare
    {
        get
        {
            return moveValue & startSquareMask;
        }
    }
    public int targetSquare
    {
        get
        {
            return (moveValue & targetSquareMask) >> 6;
        }
    }
    public int flag
    {
        get
        {
            return (moveValue & flagMask) >> 12;
        }
        set
        {
            moveValue = (ushort) ((moveValue & (startSquareMask | targetSquareMask)) | (value << 12));
        }
    }
    
    public static bool IsSame(Move m1, Move m2)
    {
        return m1.moveValue == m2.moveValue;
    }
    public static void PrintMoveList(List<Move> moves)
    {
        string s = "";

        foreach (var item in moves)
        {
            // PrintMove(item);
            s += $"{Square.SquareIndexToName(item.startSquare)}{Square.SquareIndexToName(item.targetSquare)} ";
        }
        
        Console.WriteLine(s);
    }
    public static void PrintMoveList(Move[] moves)
    {
        string s = "";

        foreach (var item in moves)
        {
            // PrintMove(item);
            s += $"{Square.SquareIndexToName(item.startSquare)}{Square.SquareIndexToName(item.targetSquare)} ";
        }
        
        Console.WriteLine(s);
    }

    public static void PrintMove(Move move)
    {
        Console.WriteLine(MoveString(move));
    }

    public static string MoveString(Move move)
    {
        return $"{Square.SquareIndexToName(move.startSquare)}{Square.SquareIndexToName(move.targetSquare)}";
    }

    public Move(int startSquare, int targetSquare)
    {
        moveValue = (ushort) startSquare;
        moveValue |= (ushort) (targetSquare << 6);
    }

    public Move(int startSquare, int targetSquare, int moveFlag)
    {
        moveValue = (ushort) startSquare;
        moveValue |= (ushort) (targetSquare << 6);
        moveValue |= (ushort) (moveFlag << 12);
    }

    public Move(ushort _moveValue)
    {
        moveValue = _moveValue;
    }

}

// Move flags
public readonly struct MoveFlag
{
    public const int None = 0;
    public const int EnpassantCapture = 1;
    public const int Castling = 2;
    public const int PromoteToQueen = 3;
    public const int PromoteToKnight = 4;
    public const int PromoteToRook = 5;
    public const int PromoteToBishop = 6;
    public const int PawnTwoForward = 7;

    public static bool IsPromotion(int flag)
    {
        return flag >= PromoteToQueen && flag <= PromoteToBishop;
    }

    public static int GetPromotionPiece(int flag, bool isWhiteTurn)
    {
        // Returns Piece Value
        switch (flag)
        {
            case PromoteToQueen:
                return (isWhiteTurn ? Piece.White : Piece.Black) | Piece.Queen;
            case PromoteToKnight:
                return (isWhiteTurn ? Piece.White : Piece.Black) | Piece.Knight;
            case PromoteToRook:
                return (isWhiteTurn ? Piece.White : Piece.Black) | Piece.Rook;
            case PromoteToBishop:
                return (isWhiteTurn ? Piece.White : Piece.Black) | Piece.Bishop;
        }

        // Just in case if the flag is not a promotion one, returns invalid Piece
        return Piece.None;
    }

    public static int GetPromotionPieceValue(int flag)
    {
        // Returns Evaluation Value
        switch (flag)
        {
            case PromoteToQueen:
                return Evaluation.queenValue;
            case PromoteToKnight:
                return Evaluation.knightValue;
            case PromoteToRook:
                return Evaluation.rookValue;
            case PromoteToBishop:
                return Evaluation.bishopValue;
        }

        // FailSafe
        return 0;
    }

    public static int GetPromotionFlag(int piece)
    {
        int pieceType = Piece.GetType(piece);

        if (pieceType == Piece.Queen)
        {
            return PromoteToQueen;
        }
        else if (pieceType == Piece.Rook)
        {
            return PromoteToRook;
        }
        else if (pieceType == Piece.Knight)
        {
            return PromoteToKnight;
        }
        else if (pieceType == Piece.Bishop)
        {
            return PromoteToBishop;
        }

        // FailSafe
        return None;
    }

    public static int GetPromotionFlag(char c)
    {
        switch (c)
        {
            case 'q':
                return PromoteToQueen;
            case 'r':
                return PromoteToRook;
            case 'b':
                return PromoteToBishop;
            case 'n':
                return PromoteToKnight;
            default:
                break;
        }
        return None;
    }
}