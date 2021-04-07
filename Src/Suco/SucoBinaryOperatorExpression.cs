namespace Zinga.Suco
{
    internal class SucoBinaryOperatorExpression : SucoExpression
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

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoBinaryOperatorExpression(startIndex, endIndex, Left, Right, Operator);
        public override SucoExpression WithType(SucoType type) => new SucoBinaryOperatorExpression(StartIndex, EndIndex, Left, Right, Operator, type);

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var left = Left.DeduceTypes(env);
            var right = Right.DeduceTypes(env);
            var resultType = left.Type.GetBinaryOperatorType(Operator, right.Type);
            if (resultType == null)
                throw new SucoCompileException($"Types “{left.Type}” and “{right.Type}” do not support the “{Operator}” operator.", left.StartIndex, right.EndIndex);
            return new SucoBinaryOperatorExpression(StartIndex, EndIndex, left, right, Operator, resultType);
        }

        public override SucoJsResult GetJavaScript(SucoEnvironment env) => Left.Type.GetBinaryOperatorJs(Operator, env, Left, Right);
    }
}