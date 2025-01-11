using static SquareRepresentation;

public static class Test
{
    static Board board;

    static Test()
    {
        board = MainProcess.board;
    }

    public static void CurrentTest()
    {
        // Add Tests Here
        // SEEPopLeastAttackerTest();
        SEEPositive();
        // MoveOrderGetScoresTest();
    }

    public static void SEEPopLeastAttackerTest()
    {
        ulong occupancy = board.BBSet[PieceIndex.WhiteAll] | board.BBSet[PieceIndex.BlackAll];
        ulong queens = board.BBSet[PieceIndex.WhiteQueen] | board.BBSet[PieceIndex.BlackQueen];
        ulong rooks = board.BBSet[PieceIndex.WhiteRook] | board.BBSet[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BBSet[PieceIndex.WhiteBishop] | board.BBSet[PieceIndex.BlackBishop] | queens;
        ulong attackers = SEE.GetAllAttackersTo(board, d4, occupancy, rooks, bishops);

        int result = SEE.PopLeastValuableAttacker(board, ref occupancy, attackers, !board.Turn);
        Console.WriteLine(result);
    }
    public static void SEEPositive()
    {
        SEE.SEEPinData pinData = new();
        pinData.Calculate(board);

        for (int i = 0; i < board.LegalMoves.Length; i++)
        {
            if (board.Squares[board.LegalMoves[i].targetSquare] == PieceUtils.None)
            {
                continue;
            }

            board.LegalMoves[i].Print();
            Console.WriteLine(SEE.HasPositiveScore(board, board.LegalMoves[i], pinData));
        }
    }

    public static void MoveOrderGetScoresTest()
    {
        MoveOrder moveOrder = new(MainProcess.engine.GetEngine());

        Span<Move> moves = stackalloc Move[128];
        MainProcess.board.MoveGen.GenerateMoves(ref moves, genOnlyCaptures: true);
        moveOrder.GetOrderedList(ref moves, Move.NullMove, inQSearch: true, 0, default, new int[moves.Length]);

        int[] scores = moveOrder.GetLastMoveScores();
        foreach (var item in scores)
        {
            Console.WriteLine(item);
        }
    }






}