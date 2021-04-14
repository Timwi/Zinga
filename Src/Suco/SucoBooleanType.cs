using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoBooleanType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoBooleanType;
        public static readonly SucoType Instance = new SucoBooleanType();
        private SucoBooleanType() { }
        public override string ToString() => "bool";
        public override int GetHashCode() => 1;

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => (op, rightOperand) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.And, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.Or, SucoBooleanType) => SucoBooleanType.Instance,
            _ => base.GetBinaryOperatorType(op, rightOperand)
        };

        public override object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => (op, rightType) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => (bool) left == (bool) right,
            (BinaryOperator.NotEqual, SucoBooleanType) => (bool) left != (bool) right,
            (BinaryOperator.And, SucoBooleanType) => (bool) left && (bool) right,
            (BinaryOperator.Or, SucoBooleanType) => (bool) left || (bool) right,
            _ => base.InterpretBinaryOperator(left, op, rightType, right)
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Not => SucoBooleanType.Instance,
            _ => base.GetUnaryOperatorType(op)
        };

        public override object InterpretUnaryOperator(UnaryOperator op, object operand) => op switch
        {
            UnaryOperator.Not => !(bool) operand,
            _ => base.InterpretUnaryOperator(op, operand)
        };
    }
}
