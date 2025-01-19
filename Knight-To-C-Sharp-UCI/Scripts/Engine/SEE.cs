public static class SEE
{
    // Use Simplified Values
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

        score -= MaterialValues[PieceUtils.GetType(next)];

        // If risking our piece being fully lost and the exchange value is still >= 0
        if (score >= 0)
        {
            return true;
        }
        
        ulong occupancy = board.BBSet[PieceIndex.WhiteAll] | board.BBSet[PieceIndex.BlackAll];
        occupancy ^= 1ul << move.startSquare;
        occupancy ^= 1ul << move.targetSquare;

        // All sliders
        ulong queens = board.BBSet[PieceIndex.WhiteQueen] | board.BBSet[PieceIndex.BlackQueen];
        ulong rooks = board.BBSet[PieceIndex.WhiteRook] | board.BBSet[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BBSet[PieceIndex.WhiteBishop] | board.BBSet[PieceIndex.BlackBishop] | queens;

        ulong attackers = GetAllAttackersTo(board, move.targetSquare, occupancy, rooks, bishops);

        bool us = !turn;

        while (true)
        {
            ulong ourOccupancy = board.BBSet[PieceIndex.MakeAll(us)];
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
            if (PieceUtils.IsDiagonalPiece(nextPieceType))
            {
                attackers |= Magic.GetBishopAttacks(move.targetSquare, occupancy) & bishops;
            }

            if (PieceUtils.IsStraightPiece(nextPieceType))
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

        score -= MaterialValues[PieceUtils.GetType(next)];

        // If risking our piece being fully lost and the exchange value is still >= 0
        if (score >= 0)
        {
            return true;
        }
        
        ulong occupancy = board.BBSet[PieceIndex.WhiteAll] | board.BBSet[PieceIndex.BlackAll];
        occupancy ^= 1ul << move.startSquare;
        occupancy ^= 1ul << move.targetSquare;

        // All sliders
        ulong queens = board.BBSet[PieceIndex.WhiteQueen] | board.BBSet[PieceIndex.BlackQueen];
        ulong rooks = board.BBSet[PieceIndex.WhiteRook] | board.BBSet[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BBSet[PieceIndex.WhiteBishop] | board.BBSet[PieceIndex.BlackBishop] | queens;

        ulong attackers = GetAllAttackersTo(board, move.targetSquare, occupancy, rooks, bishops);

        bool us = !turn;

        while (true)
        {
            ulong ourOccupancy = board.BBSet[PieceIndex.MakeAll(us)];
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
            if (PieceUtils.IsDiagonalPiece(nextPieceType))
            {
                attackers |= Magic.GetBishopAttacks(move.targetSquare, occupancy) & bishops;
            }

            if (PieceUtils.IsStraightPiece(nextPieceType))
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

    [Inline]
    public static int Gain(Board board, Move move)
    {
        if (move.flag == MoveFlag.Castling)
        {
            return 0;
        }
        else if (move.flag == MoveFlag.EnpassantCapture)
        {
            return MaterialValues[PieceUtils.Pawn];
        }

        int promotionValue = MoveFlag.GetPromotionPieceValue(move.flag);
        int targetPiece = board.Squares[move.targetSquare];

        return promotionValue - (MoveFlag.IsPromotion(move.flag) ? MaterialValues[PieceUtils.Pawn] : 0) + (targetPiece != PieceUtils.None ? MaterialValues[targetPiece.Type()] : 0);
    }

    // Returns TYPE PieceIndex
    public static int PopLeastValuableAttacker(Board board, ref ulong occupancy, ulong attackers, bool isWhiteAttackers)
    {
        // Enemy Offset
        int offset = isWhiteAttackers ? PieceIndex.White : PieceIndex.Black;

        // Loop for PieceIndex, Pawn to King
        for (int i = 0; i < 6; i++)
        {
            ulong overlap = attackers & board.BBSet[i + offset];

            if (overlap != 0)
            {
                int square = BitboardUtils.PopLSB(ref overlap);
                occupancy ^= 1ul << square;

                return i;
            }
        }

        // Returns Invalid PieceIndex for failsafe
        return PieceIndex.Invalid;
    }

    [Inline]
    public static ulong GetAllAttackersTo(Board board, int square, ulong occupancy, ulong rooks, ulong bishops)
    {
        return (rooks & Magic.GetRookAttacks(square, occupancy)) | 
            (bishops & Magic.GetBishopAttacks(square, occupancy)) | 

            (board.BBSet[PieceIndex.WhitePawn] & PreComputedMoveGenData.BlackPawnAttackMap[square]) |  // Reverse White
            (board.BBSet[PieceIndex.BlackPawn] & PreComputedMoveGenData.WhitePawnAttackMap[square]) |  // Reverse Black

            (
                (board.BBSet[PieceIndex.WhiteKnight] | board.BBSet[PieceIndex.BlackKnight]) & 
                PreComputedMoveGenData.KnightMap[square]
            ) | 
            (board.BBSet[PieceIndex.WhiteKing] | board.BBSet[PieceIndex.BlackKing])
             & 
            PreComputedMoveGenData.KingMap[square] & occupancy;
    }

    // Returns the blockers (both colors) that blocks sliding attacks to square from sliders bitboard.
    // pinners bitboard contains the pinner pieces.
    public static ulong SliderBlockers(Board board, ulong sliders, int square, ref ulong pinners)
    {
        ulong blockers = 0;
        pinners = 0;

        ulong queens = board.BBSet[PieceIndex.WhiteQueen] | board.BBSet[PieceIndex.BlackQueen];
        ulong rooks = board.BBSet[PieceIndex.WhiteRook] | board.BBSet[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BBSet[PieceIndex.WhiteBishop] | board.BBSet[PieceIndex.BlackBishop] | queens;

        ulong all = board.BBSet[PieceIndex.WhiteAll] | board.BBSet[PieceIndex.BlackAll];

        ulong friendlyAll = board.BBSet[PieceUtils.IsWhitePiece(board.Squares[square]) ? PieceIndex.WhiteAll : PieceIndex.BlackAll];

        // Snipers are sliders that attack square when a piece and other snipers are removed
        ulong snipers = ((Magic.GetRookAttacks(square, 0) & rooks) | (Magic.GetBishopAttacks(square, 0) & bishops)) & sliders;
        ulong occupancy = all ^ snipers;

        while (snipers != 0)
        {
            int sniperSq = BitboardUtils.PopLSB(ref snipers);
            // b is the blocker bitboard
            ulong b = Bits.BetweenBitboards[square, sniperSq] & occupancy;

            // b has only one significant bit
            if (b != 0 && !b.MoreThanOne())
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

        [Inline]
        public void Calculate(Board board)
        {
            WhitePinners = 0;
            BlackPinners = 0;
            WhiteBlockers = 0;
            BlackBlockers = 0;

            WhiteBlockers = SliderBlockers(board, board.BBSet[PieceIndex.BlackAll], board.PieceSquares[PieceIndex.WhiteKing][0], ref BlackPinners);
            BlackBlockers = SliderBlockers(board, board.BBSet[PieceIndex.WhiteAll], board.PieceSquares[PieceIndex.BlackKing][0], ref WhitePinners);
        }
    }
}