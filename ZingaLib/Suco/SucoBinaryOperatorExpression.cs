using System;

namespace Zinga.Suco
{
    public class SucoBinaryOperatorExpression : SucoExpression
    {
        public SucoExpression Left { get; private set; }
        public SucoExpression Right { get; private set; }
        public BinaryOperator Operator { get; private set; }

        public override string ToString() => $@"({Left} {Operator switch
        {
            BinaryOperator.Or => "|",
            BinaryOperator.And => "&",
            BinaryOperator.Equal => "=",
            BinaryOperator.NotEqual => "!=",
            BinaryOperator.LessThan => "<",
            BinaryOperator.LessThanOrEqual => "≤",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.GreaterThanOrEqual => "≥",
            BinaryOperator.Plus => "+",
            BinaryOperator.Minus => "-",
            BinaryOperator.Times => "×",
            BinaryOperator.Modulo => "%",
            BinaryOperator.Divide => "÷",
            BinaryOperator.Power => "^",
            _ => throw new InvalidOperationException()
        }} {Right})";

        public SucoBinaryOperatorExpression(int startIndex, int endIndex, SucoExpression left, SucoExpression right, BinaryOperator op, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var left = Left.DeduceTypes(env, context);
            var right = Right.DeduceTypes(env, context);
            try
            {
                var resultType = left.Type.GetBinaryOperatorType(Operator, right.Type, context);
                if (resultType == null)
                    throw new SucoCompileException($"Types “{left.Type}” and “{right.Type}” do not support the “{Operator}” operator.", left.StartIndex, right.EndIndex);
                return new SucoBinaryOperatorExpression(StartIndex, EndIndex, left, right, Operator, resultType);
            }
            catch (SucoTempCompileException ce)
            {
                throw new SucoCompileException(ce.Message, StartIndex, EndIndex);
            }
        }

        public override object Interpret(SucoEnvironment env, int?[] grid) => Left.Type.InterpretBinaryOperator(Left.Interpret(env, grid), Operator, Right.Type, Right.Interpret(env, grid));

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var left = Left.Optimize(env, givens);
            var right = Right.Optimize(env, givens);
            return left is SucoConstant cl && right is SucoConstant cr
                ? new SucoConstant(StartIndex, EndIndex, Type, left.Type.InterpretBinaryOperator(cl.Value, Operator, right.Type, cr.Value))
                : new SucoBinaryOperatorExpression(StartIndex, EndIndex, left, right, Operator, Type);
        }
    }
}