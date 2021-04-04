namespace Zinga.Suco
{
    public class SucoUnaryOperatorExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public UnaryOperator Operator { get; private set; }

        public SucoUnaryOperatorExpression(int startIndex, int endIndex, SucoExpression operand, UnaryOperator op)
            : base(startIndex, endIndex)
        {
            Operand = operand;
            Operator = op;
        }
    }
}