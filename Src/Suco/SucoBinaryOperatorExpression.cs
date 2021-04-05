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

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoBinaryOperatorExpression(startIndex, endIndex, Left, Right, Operator);
        public override SucoExpression WithType(SucoType type) => new SucoBinaryOperatorExpression(StartIndex, EndIndex, Left, Right, Operator, type);
    }
}