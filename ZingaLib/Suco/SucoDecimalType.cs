using System;

namespace Zinga.Suco
{
    public class SucoDecimalType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoDecimalType;
        public static readonly SucoType Instance = new SucoDecimalType();
        private SucoDecimalType() { }
        public override string ToString() => "decimal";
        public override int GetHashCode() => 5;

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => (op, rightType) switch
        {
            // Decimal op Decimal
            (BinaryOperator.Equal, SucoDecimalType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoDecimalType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThan, SucoDecimalType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThanOrEqual, SucoDecimalType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThan, SucoDecimalType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThanOrEqual, SucoDecimalType) => SucoBooleanType.Instance,
            (BinaryOperator.Plus, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Minus, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Times, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Modulo, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Divide, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Power, SucoDecimalType) => SucoDecimalType.Instance,

            // Decimal op Int
            (BinaryOperator.Equal, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThan, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThan, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.Plus, SucoIntegerType) => SucoDecimalType.Instance,
            (BinaryOperator.Minus, SucoIntegerType) => SucoDecimalType.Instance,
            (BinaryOperator.Times, SucoIntegerType) => SucoDecimalType.Instance,
            (BinaryOperator.Modulo, SucoIntegerType) => SucoDecimalType.Instance,
            (BinaryOperator.Divide, SucoIntegerType) => SucoDecimalType.Instance,
            (BinaryOperator.Power, SucoIntegerType) => SucoDecimalType.Instance,
            _ => base.GetBinaryOperatorType(op, rightType, context),
        };

        public override object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => (op, rightType) switch
        {
            // Decimal op Decimal
            (BinaryOperator.Equal, SucoDecimalType) => (double) left == (double) right,
            (BinaryOperator.NotEqual, SucoDecimalType) => (double) left != (double) right,
            (BinaryOperator.LessThan, SucoDecimalType) => (double) left < (double) right,
            (BinaryOperator.LessThanOrEqual, SucoDecimalType) => (double) left <= (double) right,
            (BinaryOperator.GreaterThan, SucoDecimalType) => (double) left > (double) right,
            (BinaryOperator.GreaterThanOrEqual, SucoDecimalType) => (double) left >= (double) right,
            (BinaryOperator.Plus, SucoDecimalType) => (double) left + (double) right,
            (BinaryOperator.Minus, SucoDecimalType) => (double) left - (double) right,
            (BinaryOperator.Times, SucoDecimalType) => (double) left * (double) right,
            (BinaryOperator.Modulo, SucoDecimalType) => ((double) left % (double) right + (double) right) % (double) right,
            (BinaryOperator.Divide, SucoDecimalType) => (double) left / (double) right,
            (BinaryOperator.Power, SucoDecimalType) => Math.Pow((double) left, (double) right),

            // Decimal op Int
            (BinaryOperator.Equal, SucoIntegerType) => (double) left == (int) right,
            (BinaryOperator.NotEqual, SucoIntegerType) => (double) left != (int) right,
            (BinaryOperator.LessThan, SucoIntegerType) => (double) left < (int) right,
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => (double) left <= (int) right,
            (BinaryOperator.GreaterThan, SucoIntegerType) => (double) left > (int) right,
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => (double) left >= (int) right,
            (BinaryOperator.Plus, SucoIntegerType) => (double) left + (int) right,
            (BinaryOperator.Minus, SucoIntegerType) => (double) left - (int) right,
            (BinaryOperator.Times, SucoIntegerType) => (double) left * (int) right,
            (BinaryOperator.Modulo, SucoIntegerType) => ((double) left % (int) right + (int) right) % (int) right,
            (BinaryOperator.Divide, SucoIntegerType) => (double) left / (int) right,
            (BinaryOperator.Power, SucoIntegerType) => Math.Pow((double) left, (int) right),

            _ => base.InterpretBinaryOperator(left, op, rightType, right),
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Negative => SucoDecimalType.Instance,
            _ => base.GetUnaryOperatorType(op)
        };

        public override object InterpretUnaryOperator(UnaryOperator op, object operand) => op switch
        {
            UnaryOperator.Negative => -(double) operand,
            _ => base.InterpretUnaryOperator(op, operand)
        };

        public override bool ImplicitlyConvertibleTo(SucoType other) => other switch
        {
            SucoStringType => true,
            _ => base.ImplicitlyConvertibleTo(other)
        };

        public override object InterpretImplicitConversionTo(SucoType type, object operand) => type switch
        {
            SucoStringType => operand.ToString(),
            _ => base.InterpretImplicitConversionTo(type, operand)
        };

        public override SucoType GetMemberType(string memberName, SucoContext context) => memberName switch
        {
            "atan2" => new SucoFunctionType((new[] { SucoDecimalType.Instance }, SucoDecimalType.Instance)),
            "sin" => SucoDecimalType.Instance,
            "cos" => SucoDecimalType.Instance,
            "tan" => SucoDecimalType.Instance,
            "sqrt" => SucoDecimalType.Instance,
            "abs" => SucoDecimalType.Instance,
            _ => base.GetMemberType(memberName, context)
        };

        public override object InterpretMemberAccess(string memberName, object operand) => memberName switch
        {
            "atan2" => new SucoFunction((new[] { SucoDecimalType.Instance }, SucoDecimalType.Instance, arr => Math.Atan2((double) operand, (double) arr[0]) * 180 / Math.PI)),
            "sin" => Math.Sin((double) operand * Math.PI / 180),
            "cos" => Math.Cos((double) operand * Math.PI / 180),
            "tan" => Math.Tan((double) operand * Math.PI / 180),
            "sqrt" => Math.Sqrt((double) operand),
            "abs" => Math.Abs((double) operand),
            _ => base.InterpretMemberAccess(memberName, operand)
        };
    }
}
