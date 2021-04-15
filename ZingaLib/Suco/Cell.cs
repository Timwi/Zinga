namespace Zinga.Suco
{
    public class Cell
    {
        public int Index;   // within the whole grid
        public int? Position;    // within the constraint’s list of affected cells
        public int X;
        public int Y;
        public int Box;

        public Cell(int ix, int? position)
        {
            Index = ix;
            Position = position;
            X = ix % 9;
            Y = ix / 9;
            Box = X / 3 + 3 * (Y / 3);
        }
    }
}
