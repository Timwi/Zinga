using System;

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
            //"value" => SucoType.Integer,
            "pos" => SucoType.Integer,
            "x" => SucoType.Integer,
            "y" => SucoType.Integer,
            "box" => SucoType.Integer,
            "index" => SucoType.Integer,
            "value" => context != SucoContext.Constraint ? throw new SucoTempCompileException($"Member “cell.value” can only be used in constraint logic (not SVG code).") : SucoType.Integer,

            "cx" => SucoType.Decimal,
            "cy" => SucoType.Decimal,

            "orthogonal" => new SucoFunctionType((new[] { SucoType.Cell }, SucoType.Boolean)),
            "adjacent" => new SucoFunctionType((new[] { SucoType.Cell }, SucoType.Boolean)),

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

                "orthogonal" => new SucoFunction((parameters: new[] { SucoType.Cell }, returnType: SucoType.Boolean, interpreter: args => op.Orthogonal((Cell) args[0]))),
                "adjacent" => new SucoFunction((parameters: new[] { SucoType.Cell }, returnType: SucoType.Boolean, interpreter: args => op.Adjacent((Cell) args[0]))),

                _ => base.InterpretMemberAccess(memberName, operand)
            };
        }
    }
}
