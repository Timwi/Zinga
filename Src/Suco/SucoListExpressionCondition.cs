namespace Zinga.Suco
{
    internal class SucoListExpressionCondition : SucoListCondition
    {
        public SucoExpression Expression { get; private set; }

        public SucoListExpressionCondition(int startIndex, int endIndex, SucoExpression expression)
            : base(startIndex, endIndex)
        {
            Expression = expression;
        }
    }
}