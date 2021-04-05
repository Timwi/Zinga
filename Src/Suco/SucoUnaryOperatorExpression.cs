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

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoUnaryOperatorExpression(startIndex, endIndex, Operand, Operator);
        public override SucoExpression WithType(SucoType type) => new SucoUnaryOperatorExpression(StartIndex, EndIndex, Operand, Operator, type);
    }
}