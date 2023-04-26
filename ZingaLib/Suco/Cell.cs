using System;

namespace Zinga.Suco
{
    public class Cell
    {
        public int Index;   // within the whole grid
        public int X;
        public int Y;

        public Cell(int ix, int width)
        {
            Index = ix;
            X = ix % width;
            Y = ix / width;
        }

        public override string ToString() => $"[{(char) ('A' + X)}{Y + 1}/{X / 3 + 3 * (Y / 3) + 1}]";

        public bool Orthogonal(Cell other) => Index != other.Index && ((X == other.X && Math.Abs(Y - other.Y) == 1) || (Y == other.Y && Math.Abs(X - other.X) == 1));
        public bool Adjacent(Cell other) => Index != other.Index && Math.Abs(X - other.X) <= 1 && Math.Abs(Y - other.Y) <= 1;
    }
}
