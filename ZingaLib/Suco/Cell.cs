using System;

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

        public bool Orthogonal(Cell other) => Index != other.Index && ((X == other.X && Math.Abs(Y - other.Y) == 1) || (Y == other.Y && Math.Abs(X - other.X) == 1));
        public bool Adjacent(Cell other) => Index != other.Index && Math.Abs(X - other.X) <= 1 && Math.Abs(Y - other.Y) <= 1;
    }
}
