namespace Zinga.Suco
{
    public class SucoCellType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoCellType;
        public static readonly SucoType Instance = new SucoCellType();
        private SucoCellType() { }
        public override string ToString() => "cell";
        public override int GetHashCode() => 2;

        public override SucoType GetMemberType(string memberName, SucoContext context) => memberName switch
        {
            //"value" => SucoIntegerType.Instance,
            "pos" => SucoIntegerType.Instance,
            "x" => SucoIntegerType.Instance,
            "y" => SucoIntegerType.Instance,
            "box" => SucoIntegerType.Instance,
            "index" => SucoIntegerType.Instance,
            "value" => context != SucoContext.Constraint ? throw new SucoTempCompileException($"Member “cell.value” can only be used in constraint logic (not SVG code).") : SucoIntegerType.Instance,

            "cx" => SucoDecimalType.Instance,
            "cy" => SucoDecimalType.Instance,

            _ => base.GetMemberType(memberName, context)
        };

        public override object InterpretMemberAccess(string memberName, object operand)
        {
            var op = (Cell) operand;
            return memberName switch
            {
                "pos" => op.Position,
                "x" => op.X,
                "y" => op.Y,
                "box" => op.Box,
                "index" => op.Index,
                "value" => op.Value,

                "cx" => op.X + .5,
                "cy" => op.Y + .5,

                _ => base.InterpretMemberAccess(memberName, operand)
            };
        }
    }
}
