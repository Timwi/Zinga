using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Lib;

namespace Zinga.Suco
{
    public class SucoStringType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoStringType;
        public static readonly SucoType Instance = new SucoStringType();
        private SucoStringType() { }
        public override string ToString() => "string";
        public override int GetHashCode() => 4;

        public override SucoType GetMemberType(string memberName, SucoContext context) => memberName switch
        {
            "hash" => SucoStringType.Instance,
            _ => base.GetMemberType(memberName, context)
        };

        public override object InterpretMemberAccess(string memberName, object operand) => memberName switch
        {
            "hash" => MD5.ComputeHex((string) operand),
            _ => base.InterpretMemberAccess(memberName, operand)
        };

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => (op, rightType) switch
        {
            (BinaryOperator.Plus, SucoStringType) => SucoStringType.Instance,
            _ => GetBinaryOperatorType(op, rightType, context)
        };

        public override object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => (op, rightType) switch
        {
            (BinaryOperator.Plus, SucoStringType) => (string) left + (string) right,
            _ => base.InterpretBinaryOperator(left, op, rightType, right)
        };
    }
}
