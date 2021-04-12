namespace Zinga.Suco
{
    public class SucoDecimalType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoDecimalType;
        public static readonly SucoType Instance = new SucoDecimalType();
        private SucoDecimalType() { }
        public override string ToString() => "decimal";
        public override int GetHashCode() => 5;
    }
}
