using static EvaluationConstants;

public class Evaluation
{
    Searcher searcher;
    Board board;
    
    MaterialInfo whiteMaterial;
    MaterialInfo blackMaterial;

    EvaluationData evalData;

    EvaluationBitboards bitboards;

    public Evaluation(Searcher _searcher)
    {
        searcher = _searcher;
        board = searcher.GetBoard();

        evalData = new EvaluationData(board);
        whiteMaterial = new(board, white: true);
        blackMaterial = new(board, white: false);
    }

    public int Evaluate(bool verbose = false)
    {
        int sign = board.Turn ? 1 : -1;

        whiteMaterial.GetMaterialInfo(board, white: true);
        blackMaterial.GetMaterialInfo(board, white: false);

        evalData.InitializePhaseSum();

        bitboards.Get(board);
        
        EvaluateAllTerms();

        if (verbose) {
            evalData.Print();
        }

        return evalData.Sum() * sign;
    }

    void EvaluateAllTerms() {
        int phase = evalData.currentPhase;

        evalData.materialValue = Material(phase);
        evalData.sum = evalData.materialValue;
        evalData.sum += PieceMobility(phase);
        evalData.sum += Outpost(phase);
        evalData.sum += PieceSquareTableScore(phase);
        evalData.sum += CalculateMopUpScore(phase);
        evalData.sum += CalculateOpenFileBonus(phase);
        evalData.sum += EvaluatePawns(phase);
        evalData.sum += KingSafety(phase);
    }

    // Material
    int Material(int phase) {
        int material = 0;
        for (int i = 0; i < 5; i++) {
            material += (whiteMaterial.numPieces[i] - blackMaterial.numPieces[i]) * MaterialValues[i][phase];
        }
        return material;
    }

    // Piece Mobility
    int PieceMobility(int phase)
    {
        int score = 0;

        for (int color = 0; color < 2; color++)
        {
            PieceList bishops = board.PieceSquares[PieceIndex.Bishop + color * 6];
            Bitboard friendlyPawn = color == 0 ? bitboards.whitePawns : bitboards.blackPawns;

            for (int i = 0; i < bishops.Count; i++)
            {
                Bitboard attackBB = Magic.GetBishopAttacks(bishops[i], bitboards.all) ^ friendlyPawn;

                int bonus = attackBB.Count() * PieceMobilityPerSquare[phase];

                if (color == 0) {
                    score += bonus;
                }
                else {
                    score -= bonus;
                }
            }

            PieceList rooks = board.PieceSquares[PieceIndex.Rook + color * 6];
            for (int i = 0; i < rooks.Count; i++)
            {
                Bitboard attackBB = Magic.GetRookAttacks(rooks[i], bitboards.all) ^ friendlyPawn;

                int bonus = attackBB.Count() * PieceMobilityPerSquare[phase];

                if (color == 0) {
                    score += bonus;
                }
                else {
                    score -= bonus;
                }
            }
        }

        return score;
    }

    // Outpost
    int Outpost(int phase)
    {
        int r = 0;

        for (int color = 0; color < 2; color++)
        {
            Bitboard enemyPawns = (color == 0) ? bitboards.blackPawns : bitboards.whitePawns;
            Bitboard friendlyPawns = (color == 0) ? bitboards.whitePawns : bitboards.blackPawns;

            PieceList knights = board.PieceSquares[PieceIndex.Knight + color * 6];

            Bitboard[] passMask = (color == 0) ? Bits.WhitePassedPawnMask : Bits.BlackPassedPawnMask;
            Bitboard[] enemyPawnAttackMap = (color == 0) ? PreComputedMoveGenData.BlackPawnAttackMap : PreComputedMoveGenData.WhitePawnAttackMap;

            for (int i = 0; i < knights.Count; i++) {
                int square = knights[i];

                if (
                    (passMask[square] & enemyPawns & Bits.AdjacentFilesMask[square.File()]) == 0 && 
                    ((enemyPawnAttackMap[square] & friendlyPawns) != 0)
                ) {
                    r += OutpostBonus[phase];
                }
            }
        }

        return r;
    }
    
    // Piece square table
    int PieceSquareTableScore(int phase)
    {
        int value = 0;
        
        for (int color = 0; color < 2; color++)
        {
            bool white = color == 0;

            int v = 0;

            v += ReadPieceSquareTable(PieceSquareTable.Pawn, board.PieceSquares[PieceIndex.MakePawn(white)], white, phase);
            v += ReadPieceSquareTable(PieceSquareTable.Knight, board.PieceSquares[PieceIndex.MakeKnight(white)], white, phase);
            v += ReadPieceSquareTable(PieceSquareTable.Bishop, board.PieceSquares[PieceIndex.MakeBishop(white)], white, phase);
            v += ReadPieceSquareTable(PieceSquareTable.Rook, board.PieceSquares[PieceIndex.MakeRook(white)], white, phase);
            v += ReadPieceSquareTable(PieceSquareTable.Queen, board.PieceSquares[PieceIndex.MakeQueen(white)], white, phase);
            v += ReadPieceSquareTable(PieceSquareTable.King, board.PieceSquares[PieceIndex.MakeKing(white)], white, phase);

            if (white) {
                value += v;
            }
            else {
                value -= v;
            }
        }

        return value;
    }
    [Inline]
    int ReadPieceSquareTable(Score[] table, PieceList pieceList, bool white, int phase)
    {
        int value = 0;

        for (int i = 0; i < pieceList.Count; i++)
        {
            value += PieceSquareTable.Read(table, pieceList[i], white, phase);
        }

        return value;
    }
    
    // Mop-Up
    int CalculateMopUpScore(int phase)
    {
        int score = 0;

        for (int color = 0; color < 2; color++) {
            bool white = color == 0;

            int materialDelta = evalData.materialValue * (white ? 1 : -1);

            if (materialDelta - MaterialValues[0][256] * 2 >= 0) // Winning
            {
                int mopUpScore = 0;
                int friendlyKingSquare = (white ? whiteMaterial : blackMaterial).kingSquare;
                int enemyKingSquare = (white ? blackMaterial : whiteMaterial).kingSquare;

                // Friendly king closer to the enemy king
                mopUpScore += (14 - PreComputedEvalData.DistanceFromSquare[friendlyKingSquare, enemyKingSquare]) * CloserToEnemyKing[phase];
                
                // Enemy king in the corner
                mopUpScore += PreComputedEvalData.DistanceFromCenter[enemyKingSquare] * EnemyKingCorner[phase];

                // Force the enemy king to the corner of the friendly bishop's color
                ulong friendlyBishop = white ? bitboards.whiteBishops : bitboards.blackBishops;
                if (friendlyBishop != 0) {
                    // Has a light squared bishop
                    if ((friendlyBishop & Bits.LightSquares) != 0) {
                        mopUpScore += (7 - Math.Min(
                            PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.a8], 
                            PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.h1]
                        )) * EnemyKingFriendlyBishopSquare[phase];
                    }
                    // Has a dark squared bishop
                    if ((friendlyBishop & Bits.DarkSquares) != 0) {
                        mopUpScore += (7 - Math.Min(
                            PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.h8], 
                            PreComputedEvalData.RangeDistanceFromSquare[enemyKingSquare, SquareRepresentation.a1]
                        )) * EnemyKingFriendlyBishopSquare[phase];
                    }
                }

                if (white) {
                    score += mopUpScore;
                }
                else {
                    score -= mopUpScore;
                }
            }
        }
        
        return score;
    }    
    
    // Open Files
    int CalculateOpenFileBonus(int phase)
    {
        int score = 0;

        for (int color = 0; color < 2; color++) {
            int v = 0;
            bool white = color == 0;

            PieceList rooks = board.PieceSquares[PieceIndex.MakeRook(white)];

            for (int i = 0; i < rooks.Count; i++) {
                int square = rooks[i];

                if (IsOpenFile(square)) {
                    v += RookOnOpenFileBonus[phase];
                }
                else if (IsSemiOpenFile(square)) {
                    v += RookOnSemiOpenFileBonus[phase];
                }
            }

            if (white) {
                score += v;
            }
            else {
                score -= v;
            }
        }
    
        return score;
    }

    // Pawns
    int EvaluatePawns(int phase)
    {
        int score = 0;

        for (int color = 0; color < 2; color++)
        {
            bool white = color == 0;

            int eval = 0;
            ulong frinedlyPawns = white ? bitboards.whitePawns : bitboards.blackPawns;
            ulong enemyPawns = white ? bitboards.blackPawns : bitboards.whitePawns;

            int numIsolated = 0;

            PieceList pawns = board.PieceSquares[white ? PieceIndex.WhitePawn : PieceIndex.BlackPawn];
            for (int i = 0; i < pawns.Count; i++)
            {
                int square = pawns[i];

                ulong passedPawnMask = (white ? Bits.WhitePassedPawnMask : Bits.BlackPassedPawnMask)[square];
                if ((passedPawnMask & enemyPawns) == 0)
                {
                    int rank = square.Rank();
                    eval += PassedPawnBonus[white ? 7 - rank : rank][phase];
                }

                if ((frinedlyPawns & Bits.AdjacentFilesMask[square.File()]) == 0)
                {
                    numIsolated++;
                }
            }

            eval -= IsolatedPawnPenaltyByCount[numIsolated][phase];

            // Space Advantage
            // eval += (white ? bitboards.whitePawnBehind : bitboards.blackPawnBehind).Count() * SpaceAdvantagePerSquare[phase];

            if (white) {
                score += eval;
            }
            else {
                score -= eval;
            }
        }

        return score;
    }

    // King Safety
    int KingSafety(int phase)
    {
        int score = 0;

        for (int color = 0; color < 2; color++) {
            bool isWhite = color == 0;

            int kingSquare = (isWhite ? whiteMaterial : blackMaterial).kingSquare;

            ulong totalAttacks = Magic.GetRookAttacks(kingSquare, bitboards.all) | Magic.GetBishopAttacks(kingSquare, bitboards.all);
            ulong us = board.BBSet[PieceIndex.MakeAll(isWhite)];

            totalAttacks &= ~(us | (isWhite ? bitboards.blackPawnAttacks : bitboards.whitePawnAttacks));
            int val = totalAttacks.Count();

            // Enemy Queen on the board
            if ((board.BBSet[PieceIndex.MakeRook(!isWhite)] | board.BBSet[PieceIndex.MakeQueen(!isWhite)]) != 0) {
                if (IsOpenFile(kingSquare)) {
                    val += KingOpenFilePenalty[phase];
                }
                else if (IsSemiOpenFile(kingSquare)) {
                    val += KingSemiOpenFilePenalty[phase];
                }
            }

            int ownPiecesAroundCount = 
            (PreComputedMoveGenData.KingMap[kingSquare] & board.BBSet[PieceIndex.MakePawn(isWhite)]).Count();

            if (isWhite) {
                score += val + ownPiecesAroundCount * KingShieldBonus[phase];
            }
            else {
                score -= val + ownPiecesAroundCount * KingShieldBonus[phase];
            }
        }
        
        return score;
    }

    [Inline]
    bool IsOpenFile(int square)
    {
        // (File & All Pawns) == 0
        return ((Bits.FileA << square.File()) & (board.BBSet[PieceIndex.WhitePawn] | board.BBSet[PieceIndex.BlackPawn])) == 0;
    }
    [Inline]
    bool IsOpenFileFromSide(int square, bool white)
    {
        return ((Bits.FileA << square.File()) & board.BBSet[PieceIndex.MakePawn(white)]) == 0;
    }
    [Inline]
    bool IsSemiOpenFile(int square)
    {
        return IsOpenFileFromSide(square, white: true) || IsOpenFileFromSide(square, white: false);
    }

    // Move Ordering
    [Inline]
    public static int GetAbsPieceValue(int piece)
    {
        int pieceType = piece.Type();

        switch (pieceType) {
            case PieceUtils.Pawn:
                return MaterialValues[0][0];
            case PieceUtils.Knight:
                return MaterialValues[1][0];
            case PieceUtils.Bishop:
                return MaterialValues[2][0];
            case PieceUtils.Rook:
                return MaterialValues[3][0];
            case PieceUtils.Queen:
                return MaterialValues[4][0];

            default:
                return 0;
        }
    }

    // Checkmate Detection
    [Inline]
    public bool IsMateScore(int score)
    {
        if (Math.Abs(score) >= CheckmateEval - Configuration.MaxDepth)
        {
            return true;
        }

        return false;
    }

    [Inline]
    public int MateInPly(int mateScore)
    {
        return CheckmateEval - Math.Abs(mateScore);
    }

    // Helper Structures
    struct MaterialInfo
    {
        public int[] numPieces;

        public int kingSquare;

        public MaterialInfo(Board board, bool white)
        {
            numPieces = new int[5];

            GetMaterialInfo(board, white);
        }

        public void GetMaterialInfo(Board board, bool white)
        {
            for (int i = 0; i < 5; i++) {
                numPieces[i] = board.PieceSquares[i + (white ? 0 : 6)].Count;
            }

            kingSquare = board.PieceSquares[PieceIndex.MakeKing(white)][0];
        }
    }
    struct EvaluationBitboards
    {
        // Kings
        public ulong whiteKing;
        public ulong blackKing;

        // Pieces
        public ulong whiteQueens;
        public ulong whiteRooks;
        public ulong whiteBishops;
        public ulong whiteKnights;
        public ulong whitePawns;
        public ulong blackQueens;
        public ulong blackRooks;
        public ulong blackBishops;
        public ulong blackKnights;
        public ulong blackPawns;

        public ulong all;

        // Pawn Space & Pieces behind the pawns
        public ulong whitePawnAttacks;
        public ulong blackPawnAttacks;

        public void Get(Board board)
        {
            ref BitboardSet bitboards = ref board.BBSet;
            
            whiteKing = bitboards[PieceIndex.WhiteKing];
            blackKing = bitboards[PieceIndex.BlackKing];

            whiteQueens = bitboards[PieceIndex.WhiteQueen];
            whiteRooks = bitboards[PieceIndex.WhiteRook];
            whiteBishops = bitboards[PieceIndex.WhiteBishop];
            whiteKnights = bitboards[PieceIndex.WhiteKnight];
            whitePawns = bitboards[PieceIndex.WhitePawn];

            blackQueens = bitboards[PieceIndex.BlackQueen];
            blackRooks = bitboards[PieceIndex.BlackRook];
            blackBishops = bitboards[PieceIndex.BlackBishop];
            blackKnights = bitboards[PieceIndex.BlackKnight];
            blackPawns = bitboards[PieceIndex.BlackPawn];

            all = bitboards[PieceIndex.WhiteAll] | bitboards[PieceIndex.BlackAll];

            whitePawnAttacks = 0;
            blackPawnAttacks = 0;

            ulong whitePawnClone = whitePawns;
            ulong blackPawnClone = blackPawns;

            while (whitePawnClone != 0) {
                int square = BitboardUtils.PopLSB(ref whitePawnClone);
                whitePawnAttacks |= PreComputedMoveGenData.WhitePawnAttackMap[square];
            }

            while (blackPawnClone != 0) {
                int square = BitboardUtils.PopLSB(ref blackPawnClone);
                blackPawnAttacks |= PreComputedMoveGenData.BlackPawnAttackMap[square];
            }
        //     ulong whitePawnClone = whitePawns;
        //     while (whitePawnClone != 0)
        //     {
        //         int square = BitboardUtils.PopLSB(ref whitePawnClone);
        //         int file = square.File();
        //         int rank = square.Rank();

        //         ulong fileMask = Bits.FileMask[file];
        //         whitePawnBehind |= fileMask & Bits.BlackForwardMask[rank];
        //     }
        //     whitePawnBehind |= Bits.Rank1;

        //     ulong blackPawnClone = blackPawns;
        //     while (blackPawnClone != 0)
        //     {
        //         int square = BitboardUtils.PopLSB(ref blackPawnClone);
        //         int file = square.File();
        //         int rank = square.Rank();

        //         ulong fileMask = Bits.FileMask[file];
        //         blackPawnBehind |= fileMask & Bits.WhiteForwardMask[rank];
        //     }
        //     blackPawnBehind |= Bits.RankMask[7];
        }
    }
    struct EvaluationData
    {
        Board board;
        
        // Endgame Phase constants
        const int QueenEndgamePhase = 4;
        const int RookEndgamePhase = 2;
        const int BishopEndgamePhase = 1;
        const int KnightEndgamePhase = 1;

        readonly int[] EndgamePhases = { 0, KnightEndgamePhase, BishopEndgamePhase, RookEndgamePhase, QueenEndgamePhase };

        const int TotalEndgamePhase = 2 * QueenEndgamePhase + 4 * RookEndgamePhase + 4 * BishopEndgamePhase + 
        4 * KnightEndgamePhase;

        public int currentPhase = 0;

        public int sum = 0;
        public int materialValue = 0;

        public EvaluationData(Board board)
        {
            this.board = board;

            InitializePhaseSum();
        }

        [Inline]
        public int Phase()
        {
            int phase = TotalEndgamePhase;

            for (int piece = 1; piece < 5; piece++) {
                phase -= (board.PieceSquares[piece].Count + board.PieceSquares[piece + 6].Count) * EndgamePhases[piece];
            }

            return (phase * 256 + (TotalEndgamePhase >> 1)) / TotalEndgamePhase;
        }

        [Inline]
        public int Sum()
        {
            return sum;
        }

        [Inline]
        public void InitializePhaseSum()
        {
            currentPhase = Phase();
            sum = 0;
            materialValue = 0;
        }
    
        [Inline]
        public void Print() {
            Console.WriteLine($"Eval: {sum}");
        }
    }

}