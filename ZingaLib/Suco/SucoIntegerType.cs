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
            // Comparison
            (BinaryOperator.Equal, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThan, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThan, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => SucoBooleanType.Instance,

            // Integer arithmetic
            (BinaryOperator.Plus, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Minus, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Times, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Modulo, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Divide, SucoIntegerType) => context != SucoContext.Constraint ? SucoDecimalType.Instance : throw new SucoTempCompileException("Suco does not allow the use of division in puzzle constraints. Rewrite the equation to use multiplication instead (for example: instead of a.value/b.value = 2, write a.value = 2*b.value)."),
            (BinaryOperator.Power, SucoIntegerType) => SucoIntegerType.Instance,

            // Decimal arithmetic
            (BinaryOperator.Plus, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Minus, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Times, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Modulo, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Power, SucoDecimalType) => SucoDecimalType.Instance,
            _ => base.GetBinaryOperatorType(op, rightType, context),
        };

        private static T? op<T>(int? left, int? right, Func<int, int, T> fnc) where T : struct => left == null || right == null ? null : fnc(left.Value, right.Value);

        public override object InterpretBinaryOperator(object left, BinaryOperator @operator, SucoType rightType, object right) => (@operator, rightType) switch
        {
            // Comparison
            (BinaryOperator.Equal, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a == b),
            (BinaryOperator.NotEqual, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a != b),
            (BinaryOperator.LessThan, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a < b),
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a <= b),
            (BinaryOperator.GreaterThan, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a > b),
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => op((int?) left, (int?) right, (a, b) => a >= b),

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
            UnaryOperator.Negative => SucoIntegerType.Instance,
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
                case "abs": return SucoIntegerType.Instance;
            }

            // See InterpretMemberAccess
            try
            {
                return SucoDecimalType.Instance.GetMemberType(memberName, context);
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
                return SucoDecimalType.Instance.InterpretMemberAccess(memberName, (double) (int) operand);
            }
            catch (SucoTempCompileException)
            {
                throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
            }
        }
    }
}
