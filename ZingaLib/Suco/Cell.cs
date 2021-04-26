namespace Zinga.Suco
{
    public class Cell
    {
        public int Index;   // within the whole grid
        public int? Position;    // within the constraint’s list of affected cells
        public int X;
        public int Y;
        public int Box;
        public int? Value;

        public Cell(int ix, int? position, int? value)
        {
            Index = ix;
            Position = position;
            X = ix % 9;
            Y = ix / 9;
            Box = X / 3 + 3 * (Y / 3);
            Value = value;
        }

        public override string ToString() => $"[{(char) ('A' + X)}{Y + 1}/{Box + 1}]{(Position == null ? "" : $"#{Position.Value}")}={Value}";
    }
}
