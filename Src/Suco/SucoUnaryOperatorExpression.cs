namespace Zinga.Suco
{
    public class SucoUnaryOperatorExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public UnaryOperator Operator { get; private set; }

        public SucoUnaryOperatorExpression(int startIndex, int endIndex, SucoExpression operand, UnaryOperator op, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Operand = operand;
            Operator = op;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoUnaryOperatorExpression(startIndex, endIndex, Operand, Operator);
        public override SucoExpression WithType(SucoType type) => new SucoUnaryOperatorExpression(StartIndex, EndIndex, Operand, Operator, type);

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var op = Operand.DeduceTypes(env);
            var resultType = op.Type.GetUnaryOperatorType(Operator);
            if (resultType == null)
                throw new SucoCompileException($"Type “{op.Type}” does not support the “{Operator}” unary operator.", StartIndex, EndIndex);
            return new SucoUnaryOperatorExpression(StartIndex, EndIndex, op, Operator, resultType);
        }

        public override SucoJsResult GetJavaScript(SucoEnvironment env) => Operand.Type.GetUnaryOperatorJs(Operator, env, Operand);
    }
}