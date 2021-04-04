namespace Zinga.Suco
{
    internal class SucoBinaryOperatorExpression : SucoExpression
    {
        public SucoExpression Left { get; private set; }
        public SucoExpression Right { get; private set; }
        public BinaryOperator Operator { get; private set; }

        public SucoBinaryOperatorExpression(int startIndex, int endIndex, SucoExpression left, SucoExpression right, BinaryOperator op)
            : base(startIndex, endIndex)
        {
            Left = left;
            Right = right;
            Operator = op;
        }
    }
}