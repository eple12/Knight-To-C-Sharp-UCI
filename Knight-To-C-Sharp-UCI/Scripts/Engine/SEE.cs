using System.Runtime.CompilerServices;

public static class SEE
{
    public static int[] MaterialValues = { 0, 100, 300, 300, 500, 900, 0 };

    // Same function as HasPositiveScore, but only handles capture moves
    public static bool IsGoodCapture(Board board, Move move, SEEPinData pinData)
    {
        bool turn = board.Turn;

        int score = Evaluation.GetAbsPieceValue(board.Squares[move.targetSquare]);

        if (score < 0)
        {
            return false;
        }

        int next = MoveFlag.IsPromotion(move.flag)
            ? MoveFlag.GetPromotionPiece(move.flag, turn)
            : board.Squares[move.startSquare];

        score -= MaterialValues[Piece.GetType(next)];

        // If risking our piece being fully lost and the exchange value is still >= 0
        if (score >= 0)
        {
            return true;
        }
        
        ulong occupancy = board.BitboardSet.Bitboards[PieceIndex.WhiteAll] | board.BitboardSet.Bitboards[PieceIndex.BlackAll];
        occupancy ^= 1ul << move.startSquare;
        occupancy ^= 1ul << move.targetSquare;

        // All sliders
        ulong queens = board.BitboardSet.Bitboards[PieceIndex.WhiteQueen] | board.BitboardSet.Bitboards[PieceIndex.BlackQueen];
        ulong rooks = board.BitboardSet.Bitboards[PieceIndex.WhiteRook] | board.BitboardSet.Bitboards[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BitboardSet.Bitboards[PieceIndex.WhiteBishop] | board.BitboardSet.Bitboards[PieceIndex.BlackBishop] | queens;

        ulong attackers = GetAllAttackersTo(board, move.targetSquare, occupancy, rooks, bishops);

        bool us = !turn;

        // ulong whitePinners = 0;
        // ulong blackPinners = 0;
        // ulong whiteBlockers = SliderBlockers(board, board.BitboardSet.Bitboards[PieceIndex.BlackAll], board.PieceSquares[PieceIndex.WhiteKing].Squares[0], ref blackPinners);
        // ulong blackBlockers = SliderBlockers(board, board.BitboardSet.Bitboards[PieceIndex.WhiteAll], board.PieceSquares[PieceIndex.BlackKing].Squares[0], ref whitePinners);

        while (true)
        {
            ulong ourOccupancy = board.BitboardSet.Bitboards[PieceIndex.MakeAll(us)];
            ulong ourAttackers = attackers & ourOccupancy;

            if (((us ? pinData.BlackPinners : pinData.WhitePinners) & occupancy) != 0)
            {
                ourAttackers &= ~(us ? pinData.WhiteBlockers : pinData.BlackBlockers);
            }

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

    public static bool HasPositiveScore(Board board, Move move, SEEPinData pinData)
    {
        bool turn = board.Turn;

        int score = Gain(board, move);

        if (score < 0)
        {
            return false;
        }

        int next = MoveFlag.IsPromotion(move.flag)
            ? MoveFlag.GetPromotionPiece(move.flag, turn)
            : board.Squares[move.startSquare];

        score -= MaterialValues[Piece.GetType(next)];

        // If risking our piece being fully lost and the exchange value is still >= 0
        if (score >= 0)
        {
            return true;
        }
        
        ulong occupancy = board.BitboardSet.Bitboards[PieceIndex.WhiteAll] | board.BitboardSet.Bitboards[PieceIndex.BlackAll];
        occupancy ^= 1ul << move.startSquare;
        occupancy ^= 1ul << move.targetSquare;

        // All sliders
        ulong queens = board.BitboardSet.Bitboards[PieceIndex.WhiteQueen] | board.BitboardSet.Bitboards[PieceIndex.BlackQueen];
        ulong rooks = board.BitboardSet.Bitboards[PieceIndex.WhiteRook] | board.BitboardSet.Bitboards[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BitboardSet.Bitboards[PieceIndex.WhiteBishop] | board.BitboardSet.Bitboards[PieceIndex.BlackBishop] | queens;

        ulong attackers = GetAllAttackersTo(board, move.targetSquare, occupancy, rooks, bishops);

        bool us = !turn;

        // ulong whitePinners = 0;
        // ulong blackPinners = 0;
        // ulong whiteBlockers = SliderBlockers(board, board.BitboardSet.Bitboards[PieceIndex.BlackAll], board.PieceSquares[PieceIndex.WhiteKing].Squares[0], ref blackPinners);
        // ulong blackBlockers = SliderBlockers(board, board.BitboardSet.Bitboards[PieceIndex.WhiteAll], board.PieceSquares[PieceIndex.BlackKing].Squares[0], ref whitePinners);

        while (true)
        {
            ulong ourOccupancy = board.BitboardSet.Bitboards[PieceIndex.MakeAll(us)];
            ulong ourAttackers = attackers & ourOccupancy;

            if (((us ? pinData.BlackPinners : pinData.WhitePinners) & occupancy) != 0)
            {
                ourAttackers &= ~(us ? pinData.WhiteBlockers : pinData.BlackBlockers);
            }

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

            | (board.BitboardSet.Bitboards[PieceIndex.WhiteKing] | board.BitboardSet.Bitboards[PieceIndex.BlackKing])
             & PreComputedMoveGenData.KingMap[square]

             & occupancy;
    }

    // Returns the blockers (both colors) that blocks sliding attacks to square from sliders bitboard.
    // pinners bitboard contains the pinner pieces.
    public static ulong SliderBlockers(Board board, ulong sliders, int square, ref ulong pinners)
    {
        ulong blockers = 0;
        pinners = 0;

        ulong queens = board.BitboardSet.Bitboards[PieceIndex.WhiteQueen] | board.BitboardSet.Bitboards[PieceIndex.BlackQueen];
        ulong rooks = board.BitboardSet.Bitboards[PieceIndex.WhiteRook] | board.BitboardSet.Bitboards[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BitboardSet.Bitboards[PieceIndex.WhiteBishop] | board.BitboardSet.Bitboards[PieceIndex.BlackBishop] | queens;

        ulong all = board.BitboardSet.Bitboards[PieceIndex.WhiteAll] | board.BitboardSet.Bitboards[PieceIndex.BlackAll];

        ulong friendlyAll = board.BitboardSet.Bitboards[Piece.IsWhitePiece(board.Squares[square]) ? PieceIndex.WhiteAll : PieceIndex.BlackAll];

        // Snipers are sliders that attack square when a piece and other snipers are removed
        ulong snipers = ((Magic.GetRookAttacks(square, 0) & rooks) | (Magic.GetBishopAttacks(square, 0) & bishops)) & sliders;
        ulong occupancy = all ^ snipers;

        while (snipers != 0)
        {
            int sniperSq = Bitboard.PopLSB(ref snipers);
            // b is the blocker bitboard
            ulong b = Bits.BetweenBitboards[square, sniperSq] & occupancy;

            // b has only one significant bit
            if (b != 0 && !Bitboard.MoreThanOne(b))
            {
                blockers |= b;

                // Blocked by a friendly piece
                if ((b & friendlyAll) != 0)
                {
                    pinners |= 1ul << sniperSq;
                }
            }
        }

        return blockers;
    }

    // Structure for holding the pin data
    public struct SEEPinData
    {
        public ulong WhitePinners;
        public ulong BlackPinners;
        public ulong WhiteBlockers;
        public ulong BlackBlockers;

        public SEEPinData()
        {
            WhitePinners = 0;
            BlackPinners = 0;
            WhiteBlockers = 0;
            BlackBlockers = 0;
        }

        public void Calculate(Board board)
        {
            WhitePinners = 0;
            BlackPinners = 0;
            WhiteBlockers = 0;
            BlackBlockers = 0;

            WhiteBlockers = SliderBlockers(board, board.BitboardSet.Bitboards[PieceIndex.BlackAll], board.PieceSquares[PieceIndex.WhiteKing].Squares[0], ref BlackPinners);
            BlackBlockers = SliderBlockers(board, board.BitboardSet.Bitboards[PieceIndex.WhiteAll], board.PieceSquares[PieceIndex.BlackKing].Squares[0], ref WhitePinners);
        }
    }
}