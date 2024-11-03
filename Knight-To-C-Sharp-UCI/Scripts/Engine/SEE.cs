using System.Runtime.CompilerServices;

public static class SEE
{
    public static int[] MaterialValues = { 0, 100, 300, 300, 500, 900, 0 };

    public static bool HasPositiveScore(Board board, Move move)
    {
        bool turn = board.Turn;

        int score = Gain(board, move);

        int next = MoveFlag.IsPromotion(move.flag)
            ? MoveFlag.GetPromotionPiece(move.flag, turn)
            : board.Squares[move.startSquare];

        score -= MaterialValues[Piece.GetType(next)];

        // If risking our piece being fully lost and the exchange value is still >= 0
        if (score >= 0)
        {
            return true;
        }
        
        var occupancy = board.BitboardSet.Bitboards[PieceIndex.WhiteAll] | board.BitboardSet.Bitboards[PieceIndex.BlackAll]
            ^ (1ul << move.startSquare)
            ^ (1ul << move.targetSquare);

        // All sliders
        ulong queens = board.BitboardSet.Bitboards[PieceIndex.WhiteQueen] | board.BitboardSet.Bitboards[PieceIndex.BlackQueen];
        ulong rooks = board.BitboardSet.Bitboards[PieceIndex.WhiteRook] | board.BitboardSet.Bitboards[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BitboardSet.Bitboards[PieceIndex.WhiteBishop] | board.BitboardSet.Bitboards[PieceIndex.BlackBishop] | queens;

        ulong attackers = GetAllAttackersTo(board, move.targetSquare, occupancy, rooks, bishops);

        bool us = !turn;

        while (true)
        {
            ulong ourOccupancy = board.BitboardSet.Bitboards[PieceIndex.MakeAll(us)];
            ulong ourAttackers = attackers & ourOccupancy;

            if (ourAttackers == 0)
            {
                break;
            }

            int nextPieceTypeIndex = PopLeastValuableAttacker(board, ref occupancy, ourAttackers, us);
            int nextPieceType = nextPieceTypeIndex + 1;

            // After removing an attacker, there could be a sliding piece attack
            if (Piece.IsDiagonalPiece(nextPieceType))
            {
                attackers |= Magic.GetBishopAttacks(move.targetSquare, occupancy) & bishops;
            }

            if (Piece.IsStraightPiece(nextPieceType))
            {
                attackers |= Magic.GetRookAttacks(move.targetSquare, occupancy) & rooks;
            }

            // Removing used pieces from attackers
            attackers &= occupancy;

            score = -score - 1 - MaterialValues[nextPieceType];
            us = !us;

            if (score >= 0)
            {
                // Our only attacker is our king, but the opponent still has defenders
                if ((nextPieceTypeIndex == PieceIndex.King) && (attackers & ourOccupancy) != 0)
                {
                    us = !us;
                }

                break;
            }
        }

        return turn != us;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Gain(Board board, Move move)
    {
        if (move.flag == MoveFlag.Castling)
        {
            return 0;
        }
        else if (move.flag == MoveFlag.EnpassantCapture)
        {
            return MaterialValues[Piece.Pawn];
        }

        int promotionValue = MoveFlag.GetPromotionPieceValue(move.flag);
        int targetPiece = board.Squares[move.targetSquare];

        return promotionValue - (MoveFlag.IsPromotion(move.flag) ? MaterialValues[Piece.Pawn] : 0) + (targetPiece != Piece.None ? MaterialValues[Piece.GetType(targetPiece)] : 0);
    }

    // Returns TYPE PieceIndex
    public static int PopLeastValuableAttacker(Board board, ref ulong occupancy, ulong attackers, bool isWhiteAttackers)
    {
        // Enemy Offset
        int offset = isWhiteAttackers ? PieceIndex.White : PieceIndex.Black;

        // Loop for PieceIndex, Pawn to King
        for (int i = 0; i < 6; i++)
        {
            ulong overlap = attackers & board.BitboardSet.Bitboards[i + offset];

            if (overlap != 0)
            {
                int square = Bitboard.PopLSB(ref overlap);
                occupancy ^= 1ul << square;

                return i;
            }
        }

        // Returns Invalid PieceIndex for failsafe
        return PieceIndex.Invalid;
    }

    public static ulong GetAllAttackersTo(Board board, int square, ulong occupancy, ulong rooks, ulong bishops)
    {
        return (rooks & Magic.GetRookAttacks(square, occupancy))
            | (bishops & Magic.GetBishopAttacks(square, occupancy))
            | (board.BitboardSet.Bitboards[PieceIndex.WhitePawn] & PreComputedMoveGenData.blackPawnAttackMap[square]) // Reverse White
            | (board.BitboardSet.Bitboards[PieceIndex.BlackPawn] & PreComputedMoveGenData.whitePawnAttackMap[square]) // Reverse Black
            | ((board.BitboardSet.Bitboards[PieceIndex.WhiteKnight] | board.BitboardSet.Bitboards[PieceIndex.BlackKnight])
             & PreComputedMoveGenData.KnightMap[square])
            | ((board.BitboardSet.Bitboards[PieceIndex.WhiteKing] | board.BitboardSet.Bitboards[PieceIndex.BlackKing])
             & PreComputedMoveGenData.KingMap[square]);
    }
}