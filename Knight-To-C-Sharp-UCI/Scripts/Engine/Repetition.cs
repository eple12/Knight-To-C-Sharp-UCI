public class Repetition {
    Board board;
    HashSet<ulong> First;
    HashSet<ulong> Second;

    ulong Key => board.ZobristKey;
    
    [Inline]
    public bool IsThreeFold() {
        return board.PlayedPositions.Contains(Key) || Second.Contains(Key);
    }

    [Inline]
    public void Push() {
        if (First.Add(Key)) {

        }
        else {
            Second.Add(Key);
        }
    }

    [Inline]
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

    [Inline]
    public void Clear() {
        First.Clear();
        Second.Clear();
    }
}