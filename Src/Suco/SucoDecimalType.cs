using System;
using System.Collections.Generic;
using System.Linq;

namespace Zinga.Suco
{
    public class SucoDecimalType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoDecimalType;
        public static readonly SucoType Instance = new SucoDecimalType();
        private SucoDecimalType() { }
        public override string ToString() => "decimal";
        public override int GetHashCode() => 5;

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => (op, rightOperand) switch
        {
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
            (BinaryOperator.Power, SucoDecimalType) => SucoDecimalType.Instance,
            _ => base.GetBinaryOperatorType(op, rightOperand),
        };

        public override object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => (op, rightType) switch
        {
            (BinaryOperator.Equal, SucoDecimalType) => (double) left == (double) right,
            (BinaryOperator.NotEqual, SucoDecimalType) => (double) left != (double) right,
            (BinaryOperator.LessThan, SucoDecimalType) => (double) left < (double) right,
            (BinaryOperator.LessThanOrEqual, SucoDecimalType) => (double) left <= (double) right,
            (BinaryOperator.GreaterThan, SucoDecimalType) => (double) left > (double) right,
            (BinaryOperator.GreaterThanOrEqual, SucoDecimalType) => (double) left >= (double) right,
            (BinaryOperator.Plus, SucoDecimalType) => (double) left + (double) right,
            (BinaryOperator.Minus, SucoDecimalType) => (double) left - (double) right,
            (BinaryOperator.Times, SucoDecimalType) => (double) left * (double) right,
            (BinaryOperator.Modulo, SucoDecimalType) => (double) left % (double) right,
            (BinaryOperator.Power, SucoDecimalType) => Math.Pow((double) left, (double) right),
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

        public override SucoType GetMemberType(string memberName) => memberName switch
        {
            "atan2" => new SucoFunctionType((new[] { SucoDecimalType.Instance }, SucoDecimalType.Instance)),
            "sin" => SucoDecimalType.Instance,
            "cos" => SucoDecimalType.Instance,
            "tan" => SucoDecimalType.Instance,
            "sqrt" => SucoDecimalType.Instance,
            _ => base.GetMemberType(memberName)
        };

        public override object InterpretMemberAccess(string memberName, object operand) => memberName switch
        {
            "atan2" => new SucoFunction((new[] { SucoDecimalType.Instance }, SucoDecimalType.Instance, arr => Math.Atan2((double) operand, (double) arr[0]) * 180 / Math.PI)),
            "sin" => Math.Sin((double) operand * Math.PI / 180),
            "cos" => Math.Cos((double) operand * Math.PI / 180),
            "tan" => Math.Tan((double) operand * Math.PI / 180),
            "sqrt" => Math.Sqrt((double) operand),
            _ => base.InterpretMemberAccess(memberName, operand)
        };
    }
}
