using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoBinaryOperatorExpression : SucoExpression
    {
        public SucoExpression Left { get; private set; }
        public SucoExpression Right { get; private set; }
        public BinaryOperator Operator { get; private set; }

        public SucoBinaryOperatorExpression(int startIndex, int endIndex, SucoExpression left, SucoExpression right, BinaryOperator op, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        protected override SucoExpression deduceTypes(SucoEnvironment env, SucoContext context)
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

        public override object Interpret(Dictionary<string, object> values) => Left.Type.InterpretBinaryOperator(Left.Interpret(values), Operator, Right.Type, Right.Interpret(values));
    }
}