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
        ulong occupancy = board.BitboardSet.Bitboards[PieceIndex.WhiteAll] | board.BitboardSet.Bitboards[PieceIndex.BlackAll];
        ulong queens = board.BitboardSet.Bitboards[PieceIndex.WhiteQueen] | board.BitboardSet.Bitboards[PieceIndex.BlackQueen];
        ulong rooks = board.BitboardSet.Bitboards[PieceIndex.WhiteRook] | board.BitboardSet.Bitboards[PieceIndex.BlackRook] | queens;
        ulong bishops = board.BitboardSet.Bitboards[PieceIndex.WhiteBishop] | board.BitboardSet.Bitboards[PieceIndex.BlackBishop] | queens;
        ulong attackers = SEE.GetAllAttackersTo(board, d4, occupancy, rooks, bishops);

        Console.WriteLine("occupancy");
        Bitboard.Print(occupancy);

        Console.WriteLine("Bishops");
        Bitboard.Print(bishops);

        Console.WriteLine("Rooks");
        Bitboard.Print(rooks);

        Console.WriteLine("Attackers");
        Bitboard.Print(attackers);

        int result = SEE.PopLeastValuableAttacker(board, ref occupancy, attackers, !board.Turn);
        Console.WriteLine(result);
    }
    public static void SEEPositive()
    {
        for (int i = 0; i < board.LegalMoves.Length; i++)
        {
            if (board.Squares[board.LegalMoves[i].targetSquare] == Piece.None)
            {
                continue;
            }

            Move.PrintMove(board.LegalMoves[i]);
            Console.WriteLine(SEE.HasPositiveScore(board, board.LegalMoves[i]));
        }
    }

    public static void MoveOrderGetScoresTest()
    {
        MoveOrder moveOrder = new(MainProcess.engine.GetEngine());

        Span<Move> moves = stackalloc Move[128];
        MainProcess.board.MoveGen.GenerateMoves(ref moves, genOnlyCaptures: true);
        moveOrder.GetOrderedList(moves, Move.NullMove, inQSearch: true, 0);

        int[] scores = moveOrder.GetLastMoveScores();
        foreach (var item in scores)
        {
            Console.WriteLine(item);
        }
    }






}