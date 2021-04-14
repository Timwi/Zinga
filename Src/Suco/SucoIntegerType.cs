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

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => (op, rightOperand) switch
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
            (BinaryOperator.Power, SucoIntegerType) => SucoIntegerType.Instance,

            // Decimal arithmetic
            (BinaryOperator.Plus, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Minus, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Times, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Modulo, SucoDecimalType) => SucoDecimalType.Instance,
            (BinaryOperator.Power, SucoDecimalType) => SucoDecimalType.Instance,
            _ => base.GetBinaryOperatorType(op, rightOperand),
        };

        public override object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => (op, rightType) switch
        {
            // Comparison
            (BinaryOperator.Equal, SucoIntegerType) => (int) left == (int) right,
            (BinaryOperator.NotEqual, SucoIntegerType) => (int) left != (int) right,
            (BinaryOperator.LessThan, SucoIntegerType) => (int) left < (int) right,
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => (int) left <= (int) right,
            (BinaryOperator.GreaterThan, SucoIntegerType) => (int) left > (int) right,
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => (int) left >= (int) right,

            // Integer arithmetic
            (BinaryOperator.Plus, SucoIntegerType) => (int) left + (int) right,
            (BinaryOperator.Minus, SucoIntegerType) => (int) left - (int) right,
            (BinaryOperator.Times, SucoIntegerType) => (int) left * (int) right,
            (BinaryOperator.Modulo, SucoIntegerType) => (int) left % (int) right,
            (BinaryOperator.Power, SucoIntegerType) => (int) BigInteger.Pow((int) left, (int) right),

            // Decimal arithmetic
            (BinaryOperator.Plus, SucoDecimalType) => (int) left + (double) right,
            (BinaryOperator.Minus, SucoDecimalType) => (int) left - (double) right,
            (BinaryOperator.Times, SucoDecimalType) => (int) left * (double) right,
            (BinaryOperator.Modulo, SucoDecimalType) => (int) left % (double) right,
            (BinaryOperator.Power, SucoDecimalType) => Math.Pow((int) left, (double) right),

            _ => base.InterpretBinaryOperator(left, op, rightType, right),
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

        public override SucoType GetMemberType(string memberName)
        {
            // See InterpretMemberAccess
            try
            {
                return SucoDecimalType.Instance.GetMemberType(memberName);
            }
            catch (SucoTempCompileException)
            {
                throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
            }
        }

        public override object InterpretMemberAccess(string memberName, object operand)
        {
            // We want all functions that SucoDecimalType supports to also work on integers by implicitly converting the integer to decimal.
            // This implicit conversion doesn’t happen automatically.
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
