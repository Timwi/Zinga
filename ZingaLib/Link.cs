using System;

namespace Zinga.Lib
{
    public struct Xy : IEquatable<Xy>
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Xy(int x, int y)
        {
            X = x;
            Y = y;
        }
        public readonly bool Equals(Xy other) => other.X == X && other.Y == Y;
        public override readonly int GetHashCode() => unchecked(Y * 10247 + X);
        public override readonly bool Equals(object obj) => obj is Xy xy && Equals(xy);
        public override readonly string ToString() => $"({X}, {Y})";
    }
    public struct Link : IEquatable<Link>
    {
        public Xy From { get; private set; }
        public Xy To { get; private set; }

        public Link(Xy from, Xy to)
        {
            From = from;
            To = to;
        }

        public Link(int fromX, int fromY, int toX, int toY) : this(new Xy(fromX, fromY), new Xy(toX, toY)) { }

        public readonly bool Equals(Link other) => (From.Equals(other.From) && To.Equals(other.To)) || (To.Equals(other.From) && From.Equals(other.To));
        public override readonly int GetHashCode() => unchecked(From.GetHashCode() ^ To.GetHashCode());
        public override readonly bool Equals(object obj) => obj is Link link && Equals(link);
        public override readonly string ToString() => $"({From.X}, {From.Y}) ⇒ ({To.X}, {To.Y})";
    }
}