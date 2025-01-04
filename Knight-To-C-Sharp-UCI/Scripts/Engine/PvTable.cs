using System.Collections.Immutable;

public class PvTable {
    public static readonly ImmutableArray<int> Indexes = Initialize();

    private static ImmutableArray<int> Initialize()
    {
        var indexes = new int[Configuration.MaxDepth];
        int previousPVIndex = 0;
        indexes[0] = previousPVIndex;

        for (int depth = 0; depth < indexes.Length - 1; ++depth)
        {
            indexes[depth + 1] = previousPVIndex + Configuration.MaxDepth - depth;
            previousPVIndex = indexes[depth + 1];
        }

        return [.. indexes];
    }

    // Size = 1+2+3+ ... +(N-1)+N = N(N+1)/2
    public const int PvTableSize = Configuration.MaxDepth * (Configuration.MaxDepth + 1) / 2;

    public Move[] Pv = new Move[PvTableSize];

    
}

// Pv Line length at ply

// ply  maxLengthPV
//     +--------------------------------------------+
// 0   |N                                           |
//     +------------------------------------------+-+
// 1   |N-1                                       |
//     +----------------------------------------+-+
// 2   |N-2                                     |
//     +--------------------------------------+-+
// 3   |N-3                                   |
//     +------------------------------------+-+
// 4   |N-4                                 |
// ...                        /
// N-4 |4      |
//     +-----+-+
// N-3 |3    |
//     +---+-+
// N-2 |2  |
//     +-+-+
// N-1 |1|
//     +-+