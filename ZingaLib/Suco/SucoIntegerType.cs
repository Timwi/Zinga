using System;
using System.Numerics;

namespace Zinga.Suco
{
    public class SucoIntegerType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoIntegerType;
        public static readonly SucoType Instance = new SucoIntegerType();
        private SucoIntegerType() { }
        public override string ToString() => "int";
        public override int GetHashCode() => 3;

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => (op, rightType) switch
        {
            // Comparison with Int
            (BinaryOperator.Equal, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.NotEqual, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.LessThan, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.GreaterThan, SucoIntegerType) => SucoType.Boolean,
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => SucoType.Boolean,

            // Comparison with Decimal
            (BinaryOperator.Equal, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.NotEqual, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.LessThan, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.LessThanOrEqual, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.GreaterThan, SucoDecimalType) => SucoType.Boolean,
            (BinaryOperator.GreaterThanOrEqual, SucoDecimalType) => SucoType.Boolean,

            // Arithmetic with Int
            (BinaryOperator.Plus, SucoIntegerType) => SucoType.Integer,
            (BinaryOperator.Minus, SucoIntegerType) => SucoType.Integer,
            (BinaryOperator.Times, SucoIntegerType) => SucoType.Integer,
            (BinaryOperator.Modulo, SucoIntegerType) => SucoType.Integer,
            (BinaryOperator.Divide, SucoIntegerType) => context != SucoContext.Constraint ? SucoType.Decimal : throw new SucoTempCompileException("Suco does not allow the use of division in puzzle constraints. Rewrite the equation to use multiplication instead (for example: instead of a.value/b.value = 2, write a.value = 2*b.value)."),
            (BinaryOperator.Power, SucoIntegerType) => SucoType.Integer,

            // Arithmetic with Decimal
            (BinaryOperator.Plus, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Minus, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Times, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Modulo, SucoDecimalType) => SucoType.Decimal,
            (BinaryOperator.Power, SucoDecimalType) => SucoType.Decimal,
            _ => base.GetBinaryOperatorType(op, rightType, context),
        };

        private static T? op<T>(int? left, int? right, Func<int, int, T> fnc) where T : struct => left == null || right == null ? null : fnc(left.Value, right.Value);

        public override object InterpretBinaryOperator(object left, BinaryOperator @operator, SucoType rightType, object right) => (@operator, rightType) switch
        {
            // Comparison with Int
            (BinaryOperator.Equal, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a == b),
            (BinaryOperator.NotEqual, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a != b),
            (BinaryOperator.LessThan, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a < b),
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a <= b),
            (BinaryOperator.GreaterThan, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a > b),
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a >= b),

            // Comparison with Decimal
            (BinaryOperator.Equal, SucoDecimalType) => (int) left == (double) right,
            (BinaryOperator.NotEqual, SucoDecimalType) => (int) left != (double) right,
            (BinaryOperator.LessThan, SucoDecimalType) => (int) left < (double) right,
            (BinaryOperator.LessThanOrEqual, SucoDecimalType) => (int) left <= (double) right,
            (BinaryOperator.GreaterThan, SucoDecimalType) => (int) left > (double) right,
            (BinaryOperator.GreaterThanOrEqual, SucoDecimalType) => (int) left >= (double) right,

            // Integer arithmetic
            (BinaryOperator.Plus, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a + b),
            (BinaryOperator.Minus, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a - b),
            (BinaryOperator.Times, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a * b),
            (BinaryOperator.Modulo, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => (a % b + b) % b),
            (BinaryOperator.Divide, SucoIntegerType) => (double) (int) left / (int) right,
            (BinaryOperator.Power, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => (int) BigInteger.Pow(a, b)),

            // Decimal arithmetic
            (BinaryOperator.Plus, SucoDecimalType) => (int) left + (double) right,
            (BinaryOperator.Minus, SucoDecimalType) => (int) left - (double) right,
            (BinaryOperator.Times, SucoDecimalType) => (int) left * (double) right,
            (BinaryOperator.Modulo, SucoDecimalType) => ((int) left % (double) right + (double) right) % (double) right,
            (BinaryOperator.Power, SucoDecimalType) => Math.Pow((int) left, (double) right),

            _ => base.InterpretBinaryOperator(left, @operator, rightType, right),
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Negative => SucoType.Integer,
            _ => base.GetUnaryOperatorType(op),
        };

        public override object InterpretUnaryOperator(UnaryOperator op, object operand) => op switch
        {
            UnaryOperator.Negative => -(int) operand,
            _ => base.InterpretUnaryOperator(op, operand),
        };

        public override bool ImplicitlyConvertibleTo(SucoType other) => other switch
        {
            SucoDecimalType => true,
            SucoStringType => true,
            _ => base.ImplicitlyConvertibleTo(other)
        };

        public override object InterpretImplicitConversionTo(SucoType type, object operand) => type switch
        {
            SucoDecimalType => (double) (int) operand,
            SucoStringType => operand.ToString(),
            _ => base.InterpretImplicitConversionTo(type, operand)
        };

        public override SucoType GetMemberType(string memberName, SucoContext context)
        {
            switch (memberName)
            {
                case "abs": return SucoType.Integer;
            }

            // See InterpretMemberAccess
            try
            {
                return SucoType.Decimal.GetMemberType(memberName, context);
            }
            catch (SucoTempCompileException)
            {
                throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
            }
        }

        public override object InterpretMemberAccess(string memberName, object operand)
        {
            switch (memberName)
            {
                case "abs": return operand == null ? null : Math.Abs((int) operand);
            }

            // We want all functions that SucoDecimalType supports to also work on integers by implicitly converting the integer to decimal.
            try
            {
                return SucoType.Decimal.InterpretMemberAccess(memberName, (double) (int) operand);
            }
            catch (SucoTempCompileException)
            {
                throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
            }
        }
    }
}
