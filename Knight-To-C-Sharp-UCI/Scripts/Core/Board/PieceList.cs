// Always use FOR statement
public class PieceList
{
    public int[] squares = new int[64];
    int[] map = new int[64];
    public int count = 0;

    public PieceList() // index 0 ~ count - 1 => valid square index / out of the range (index count ~ ) => garbage data
    {

    }

    public void Add(int square)
    {
		squares[count] = square;
		map[square] = count;
		count++;
	}

	public void Remove(int square)
    {
		int index = map[square]; // get the index of this element in the squares array
		
		squares[index] = squares[count - 1]; // move last element in array to the place of the removed  ERROR
		map[squares[index]] = index; // update map to point to the moved element's new location in the array
		count--;
	}
    
}
