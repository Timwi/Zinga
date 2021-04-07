namespace Zinga.Suco
{
    public class SucoCellType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoCellType;
        public static readonly SucoType Instance = new SucoCellType();
        private SucoCellType() { }
        public override string ToString() => "cell";

        public override SucoType GetMemberType(string memberName)
        {
            return memberName switch
            {
                "value" => SucoIntegerType.Instance,
                "pos" => SucoIntegerType.Instance,
                _ => null,
            };
        }
        public override int GetHashCode() => 2;
    }
}
