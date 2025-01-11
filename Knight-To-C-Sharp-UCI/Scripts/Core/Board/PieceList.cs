// Always use FOR statement
public class PieceList
{
    int[] Squares = new Square[64];
    int[] map = new int[64];
	private int count = 0;
    public int Count
	{
		get { return count; }
	}

    public PieceList() // index 0 ~ count - 1 => valid square index / out of the range (index count ~ ) => garbage data
    {

    }

    public void Add(Square square)
    {
		Squares[Count] = square;
		map[square] = Count;
		count++;
	}

	public void Remove(Square square)
    {
		int index = map[square]; // get the index of this element in the squares array
		
		Squares[index] = Squares[Count - 1]; // move last element in array to the place of the removed  ERROR
		map[Squares[index]] = index; // update map to point to the moved element's new location in the array
		count--;
	}

	public void Reset() {
		Squares = new Square[64];
		count = 0;
	}
    
	public Square this[int index] {
		get {
			return Squares[index];
		}
	}
}
