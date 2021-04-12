namespace Zinga.Suco
{
    public class SucoCellType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoCellType;
        public static readonly SucoType Instance = new SucoCellType();
        private SucoCellType() { }
        public override string ToString() => "cell";

        public override SucoType GetMemberType(string memberName) => memberName switch
        {
            "value" => SucoIntegerType.Instance,
            "pos" => SucoIntegerType.Instance,
            "x" => SucoIntegerType.Instance,
            "y" => SucoIntegerType.Instance,
            "box" => SucoIntegerType.Instance,
            _ => base.GetMemberType(memberName)
        };

        public override int GetHashCode() => 2;
    }
}
