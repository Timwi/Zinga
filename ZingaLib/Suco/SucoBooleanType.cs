using System;

namespace Zinga.Suco
{
    public class SucoBooleanType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoBooleanType;
        public SucoBooleanType() { }
        public override string ToString() => "bool";
        public override int GetHashCode() => 1;

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => (op, rightType) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => SucoType.Boolean,
            (BinaryOperator.NotEqual, SucoBooleanType) => SucoType.Boolean,
            (BinaryOperator.And, SucoBooleanType) => SucoType.Boolean,
            (BinaryOperator.Or, SucoBooleanType) => SucoType.Boolean,
            _ => base.GetBinaryOperatorType(op, rightType, context)
        };

        private static T? op<T>(bool? left, bool? right, Func<bool, bool, T> fnc) where T : struct => left == null || right == null ? null : fnc(left.Value, right.Value);

        public override object InterpretBinaryOperator(object left, BinaryOperator @operator, SucoType rightType, object right) => (@operator, rightType) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => op((bool?) left, (bool?) right, (a, b) => a == b),
            (BinaryOperator.NotEqual, SucoBooleanType) => op((bool?) left, (bool?) right, (a, b) => a != b),
            (BinaryOperator.And, SucoBooleanType) => (bool?) left == false || (bool?) right == false ? false : left == null || right == null ? null : true,
            (BinaryOperator.Or, SucoBooleanType) => (bool?) left == true || (bool?) right == true ? true : left == null || right == null ? null : false,
            _ => base.InterpretBinaryOperator(left, @operator, rightType, right)
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Not => SucoType.Boolean,
            _ => base.GetUnaryOperatorType(op)
        };

        public override object InterpretUnaryOperator(UnaryOperator op, object operand) => op switch
        {
            UnaryOperator.Not => !(bool) operand,
            _ => base.InterpretUnaryOperator(op, operand)
        };
    }
}
