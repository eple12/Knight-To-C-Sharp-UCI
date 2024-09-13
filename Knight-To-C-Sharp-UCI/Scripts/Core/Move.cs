
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
    public static bool IsNull(Move m)
    {
        return IsSame(m, NullMove);
    }
    public static void PrintMoveList(List<Move> moves)
    {
        string s = "";

        foreach (var item in moves)
        {
            // PrintMove(item);
            s += MoveString(item) + ' ';
        }
        
        Console.WriteLine(s);
    }
    public static void PrintMoveList(Move[] moves)
    {
        string s = "";

        foreach (var item in moves)
        {
            // PrintMove(item);
            s += MoveString(item) + ' ';
        }
        
        Console.WriteLine(s);
    }

    public static void PrintMove(Move move)
    {
        Console.WriteLine(MoveString(move));
    }

    public static string MoveString(Move move)
    {
        if (IsSame(move, NullMove))
        {
            return NullMoveString;
        }

        string promotion = "";
        if (MoveFlag.IsPromotion(move.flag))
        {
            switch(move.flag)
            {
                case MoveFlag.PromoteToQueen:
                    promotion = "q";
                    break;
                case MoveFlag.PromoteToRook:
                    promotion = "r";
                    break;
                case MoveFlag.PromoteToBishop:
                    promotion = "b";
                    break;
                case MoveFlag.PromoteToKnight:
                    promotion = "n";
                    break;
                default:
                    break;
            }
        }
        return $"{Square.Name(move.startSquare)}{Square.Name(move.targetSquare)}" + promotion;
    }
    public static Move FindMove(List<Move> moves, string moveString)
    {
        if (moves.Count < 1)
        {
            return NullMove;
        }

        Move move = moves[0];

        foreach (var m in moves)
        {
            if (MoveString(m) == moveString)
            {
                return m;
            }
        }

        return move;
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

    public static readonly string NullMoveString = "(none)";
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

    public static int GetPromotionPiece(int flag, bool isWhite = true)
    {
        // Returns Piece Value
        switch (flag)
        {
            case PromoteToQueen:
                return (isWhite ? Piece.White : Piece.Black) | Piece.Queen;
            case PromoteToKnight:
                return (isWhite ? Piece.White : Piece.Black) | Piece.Knight;
            case PromoteToRook:
                return (isWhite ? Piece.White : Piece.Black) | Piece.Rook;
            case PromoteToBishop:
                return (isWhite ? Piece.White : Piece.Black) | Piece.Bishop;
        }

        // Just in case if the flag is not a promotion one, returns invalid Piece
        return Piece.None;
    }

    public static int GetPromotionPieceValue(int flag)
    {
        return Evaluation.GetAbsPieceValue(GetPromotionPiece(flag));
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