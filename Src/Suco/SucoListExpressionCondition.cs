namespace Zinga.Suco
{
    internal class SucoListExpressionCondition : SucoListCondition
    {
        public SucoExpression Expression { get; private set; }

        public SucoListExpressionCondition(int startIndex, int endIndex, SucoExpression expression, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Expression = expression;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoListExpressionCondition(startIndex, endIndex, Expression);
        public override SucoExpression WithType(SucoType type) => new SucoListExpressionCondition(StartIndex, EndIndex, Expression, type);
    }
}