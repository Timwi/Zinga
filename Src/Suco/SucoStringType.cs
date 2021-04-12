namespace Zinga.Suco
{
    public class SucoStringType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoStringType;
        public static readonly SucoType Instance = new SucoStringType();
        private SucoStringType() { }
        public override string ToString() => "string";
        public override int GetHashCode() => 4;
    }
}
