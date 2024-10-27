
public static class MateChecker
{
    public enum MateState {None, Checkmate, Stalemate, FiftyDraw, Threefold, Material};

    public static MateState GetPositionState(Board board, Span<Move> moves, bool ExcludeRepetition = false, bool ExcludeFifty = false)
    {
        if (moves.Length == 0)
        {
            if (board.MoveGen.InCheck())
            {
                // Checkmate
                return MateState.Checkmate;
            }
            else
            {
                // Stalemate
                return MateState.Stalemate;
            }
        }
        
        if (board.FiftyRuleHalfClock == 100)
        {
            // Fifty-move rule
            return MateState.FiftyDraw;
        }

        if (IsInsufficientMaterial(board))
        {
            // Insufficient Material
            return MateState.Material;
        }
        
        if (ExcludeRepetition)
        {
            // if (board.PositionHistory.ContainsKey(board.ZobristKey) && board.PositionHistory[board.ZobristKey] > 1)
            // {
            //     return MateState.Threefold;
            // }
        }
        else if (board.PositionHistory[board.ZobristKey] >= 3)
        // else if (board.RepetitionVerify.Contains(board.ZobristKey))
        {
            // Threefold repetition
            return MateState.Threefold;
        }

        return MateState.None;
    }

    // public static int StorePositionHistory(Board board)
    // {
    //     if (board.positionHistory.ContainsKey(board.currentZobristKey))
    //     {
    //         board.positionHistory[board.currentZobristKey] += 1;
    //         return board.positionHistory[board.currentZobristKey];
    //     }
    //     else
    //     {
    //         board.positionHistory.Add(board.currentZobristKey, 1);
    //         return -1;
    //     }
    // }

    public static bool IsInsufficientMaterial(Board board)
    {
        int wq = board.PieceSquares[PieceIndex.WhiteQueen].count;
        int wr = board.PieceSquares[PieceIndex.WhiteRook].count;
        PieceList wb = board.PieceSquares[PieceIndex.WhiteBishop];
        int wn = board.PieceSquares[PieceIndex.WhiteKnight].count;
        int wp = board.PieceSquares[PieceIndex.WhitePawn].count;

        int bq = board.PieceSquares[PieceIndex.BlackQueen].count;
        int br = board.PieceSquares[PieceIndex.BlackRook].count;
        PieceList bb = board.PieceSquares[PieceIndex.BlackBishop];
        int bn = board.PieceSquares[PieceIndex.BlackKnight].count;
        int bp = board.PieceSquares[PieceIndex.BlackPawn].count;

        if (wp != 0 || wq != 0 || wr != 0 || bp != 0 || bq != 0 || br != 0)
        {
            return false;
        }
        if (wn <= 1 && bn <= 1 && wb.count <= 1 && bb.count <= 1)
        {
            if (!(wn == 1 && bn == 1) && wb.count == 0 && bb.count == 0)
            {
                return true;
            }
            if (!(wb.count == 1 && bb.count == 1) && wn == 0 && bn == 0)
            {
                return true;
            }
            if (wn == 0 && bn == 0 && wb.count == 1 && bb.count == 1)
            {
                if (((wb.squares[0] % 8) + (wb.squares[0] / 8)) % 2 == ((bb.squares[0] % 8) + (bb.squares[0] / 8)) % 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void PrintMateState(MateState state)
    {
        Console.WriteLine(ToString(state));
    }

    public static string ToString(MateState state)
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