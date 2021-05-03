using System;

namespace Zinga.Suco
{
    public class SucoDecimalType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoDecimalType;
        public override string ToString() => "decimal";
        public override int GetHashCode() => 5;
        public override Type CsType => typeof(double?);

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => (op, rightType) switch
        {
            // Decimal op Decimal
            (BinaryOperator.Equal, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.NotEqual, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.LessThan, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.LessThanOrEqual, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.GreaterThan, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.GreaterThanOrEqual, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.Plus, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Minus, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Times, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Modulo, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Divide, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Power, SucoDecimalType) => SucoType.Decimal,

            // Decimal op Int
            (BinaryOperator.Equal, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.NotEqual, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.LessThan, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.GreaterThan, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.Plus, SucoIntegerType) => SucoType.Decimal,
            (BinaryOperator.Minus, SucoIntegerType) => SucoType.Decimal,
            (BinaryOperator.Times, SucoIntegerType) => SucoType.Decimal,
            (BinaryOperator.Modulo, SucoIntegerType) => SucoType.Decimal,
            (BinaryOperator.Divide, SucoIntegerType) => SucoType.Decimal,
            (BinaryOperator.Power, SucoIntegerType) => SucoType.Decimal,
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
            UnaryOperator.Negative => SucoType.Decimal,
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
            "atan2" => new SucoFunctionType((new[] { SucoType.Decimal }, SucoType.Decimal)),
            "sin" => SucoType.Decimal,
            "cos" => SucoType.Decimal,
            "tan" => SucoType.Decimal,
            "sqrt" => SucoType.Decimal,
            "abs" => SucoType.Decimal,
            _ => base.GetMemberType(memberName, context)
        };

        public override object InterpretMemberAccess(string memberName, object operand, SucoEnvironment env, int?[] grid) => memberName switch
        {
            "atan2" => new SucoFunction((new[] { SucoType.Decimal }, SucoType.Decimal, arr => Math.Atan2((double) operand, (double) arr[0]) * 180 / Math.PI)),
            "sin" => Math.Sin((double) operand * Math.PI / 180),
            "cos" => Math.Cos((double) operand * Math.PI / 180),
            "tan" => Math.Tan((double) operand * Math.PI / 180),
            "sqrt" => Math.Sqrt((double) operand),
            "abs" => Math.Abs((double) operand),
            _ => base.InterpretMemberAccess(memberName, operand, env, grid)
        };
    }
}
