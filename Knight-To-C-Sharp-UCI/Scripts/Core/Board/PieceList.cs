// Always use FOR statement
public class PieceList
{
    public int[] Squares = new int[64];
    int[] map = new int[64];
    public int Count = 0;

    public PieceList() // index 0 ~ count - 1 => valid square index / out of the range (index count ~ ) => garbage data
    {

    }

    public void Add(int square)
    {
		Squares[Count] = square;
		map[square] = Count;
		Count++;
	}

	public void Remove(int square)
    {
		int index = map[square]; // get the index of this element in the squares array
		
		Squares[index] = Squares[Count - 1]; // move last element in array to the place of the removed  ERROR
		map[Squares[index]] = index; // update map to point to the moved element's new location in the array
		Count--;
	}
    
}
