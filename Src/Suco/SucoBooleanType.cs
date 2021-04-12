using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoBooleanType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoBooleanType;
        public static readonly SucoType Instance = new SucoBooleanType();
        private SucoBooleanType() { }
        public override string ToString() => "bool";

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => (op, rightOperand) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.And, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.Or, SucoBooleanType) => SucoBooleanType.Instance,
            _ => base.GetBinaryOperatorType(op, rightOperand)
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Not => SucoBooleanType.Instance,
            _ => base.GetUnaryOperatorType(op)
        };

        public override int GetHashCode() => 1;

        public override object InterpretBinaryOperator(Dictionary<string, object> values, SucoExpression left, BinaryOperator op, SucoExpression right) => (op, right.Type) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => (bool) left.Interpret(values) == (bool) right.Interpret(values),
            (BinaryOperator.NotEqual, SucoBooleanType) => (bool) left.Interpret(values) != (bool) right.Interpret(values),
            (BinaryOperator.And, SucoBooleanType) => (bool) left.Interpret(values) && (bool) right.Interpret(values),
            (BinaryOperator.Or, SucoBooleanType) => (bool) left.Interpret(values) || (bool) right.Interpret(values),
            _ => base.InterpretBinaryOperator(values, left, op, right)
        };

        public override object InterpretUnaryOperator(Dictionary<string, object> values, UnaryOperator op, SucoExpression operand) => op switch
        {
            UnaryOperator.Not => !(bool) operand.Interpret(values),
            _ => base.InterpretUnaryOperator(values, op, operand)
        };
    }
}
