using System.Collections.Immutable;
using System.Runtime.CompilerServices;

public class PvTable {
    // Indexes[ply] => Starting Index
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

    public Move this[int index] {
        get {
            return Pv[index];
        }
        set {
            Pv[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFrom(int index) {
        Array.Clear(Pv, index, PvTableSize - index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyFrom(int target, int source, int length) {
        if (Pv[source].IsNull()) {
            ClearFrom(target);
            return;
        }

        Array.Copy(Pv, source, Pv, target, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearAll() {
        Array.Clear(Pv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetRootString() {
        return string.Join(' ', Pv[..Indexes[1]].Where(a => a != Move.NullMove).Select(a => a.San));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearExceptRoot() {
        ClearFrom(Indexes[1]);
    }
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