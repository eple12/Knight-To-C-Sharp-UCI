using System.Runtime.CompilerServices;

public class Repetition {
    Board board;
    HashSet<ulong> First;
    HashSet<ulong> Second;

    ulong Key => board.ZobristKey;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsThreeFold() {
        return board.PlayedPositions.Contains(Key) || Second.Contains(Key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push() {
        if (First.Add(Key)) {

        }
        else {
            Second.Add(Key);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pop() {
        if (Second.Remove(Key)) {

        }
        else {
            First.Remove(Key);
        }
    }

    public Repetition(Board _board) {
        board = _board;

        First = new();
        Second = new();
    }

    public void Clear() {
        First.Clear();
        Second.Clear();
    }
}