public static class MateChecker
{
    public enum MateState { None, Checkmate, Stalemate, FiftyDraw, Threefold, Material };

    public static MateState GetPositionState(Board board, Span<Move> moves, bool ExcludeFifty = false)
    {
        if (moves.Length == 0)
        {
            if (board.InCheck())
            {
                return MateState.Checkmate;
            }
            else
            {
                return MateState.Stalemate;
            }
        }
        
        if (!ExcludeFifty && board.FiftyRuleHalfClock >= 100)
        {
            // Fifty-move rule
            return MateState.FiftyDraw;
        }

        if (IsInsufficientMaterial(board))
        {
            // Insufficient Material
            return MateState.Material;
        }

        return MateState.None;
    }

    [Inline]
    public static bool IsInsufficientMaterial(Board board)
    {
        int wq = board.PieceSquares[PieceIndex.WhiteQueen].Count;
        int wr = board.PieceSquares[PieceIndex.WhiteRook].Count;
        PieceList wb = board.PieceSquares[PieceIndex.WhiteBishop];
        int wn = board.PieceSquares[PieceIndex.WhiteKnight].Count;
        int wp = board.PieceSquares[PieceIndex.WhitePawn].Count;

        int bq = board.PieceSquares[PieceIndex.BlackQueen].Count;
        int br = board.PieceSquares[PieceIndex.BlackRook].Count;
        PieceList bb = board.PieceSquares[PieceIndex.BlackBishop];
        int bn = board.PieceSquares[PieceIndex.BlackKnight].Count;
        int bp = board.PieceSquares[PieceIndex.BlackPawn].Count;

        // Major pieces remaining
        if (wp != 0 || wq != 0 || wr != 0 || bp != 0 || bq != 0 || br != 0)
        {
            return false;
        }

        // Insufficient Material
        if (wn <= 1 && bn <= 1 && wb.Count <= 1 && bb.Count <= 1)
        {
            if (!(wn == 1 && bn == 1) && wb.Count == 0 && bb.Count == 0)
            {
                return true;
            }
            if (!(wb.Count == 1 && bb.Count == 1) && wn == 0 && bn == 0)
            {
                return true;
            }
            if (wn == 0 && bn == 0 && wb.Count == 1 && bb.Count == 1)
            {
                if (((wb[0] % 8) + (wb[0] / 8)) % 2 == ((bb[0] % 8) + (bb[0] / 8)) % 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void PrintMateState(MateState state)
    {
        Console.WriteLine(state.ToMateString());
    }

    public static string ToMateString(this MateState state)
    {
        switch (state)
        {
            case MateState.Checkmate:
                return "Checkmate!";
            
            case MateState.Stalemate:
                return "Stalemate!";

            case MateState.FiftyDraw:
                return "Draw!\n(Fifty-Move rule)";

            case MateState.Threefold:
                return "Draw!\n(Threefold)";        

            case MateState.Material:
                return "Draw!\n(Insufficient material)";            
            
            default:
                return "";
        }
    }
}