namespace Zinga.Suco
{
    public class SucoDecimalLiteralExpression : SucoExpression
    {
        public double LiteralValue { get; private set; }

        public SucoDecimalLiteralExpression(int startIndex, int endIndex, double numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            LiteralValue = numericalValue;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => new SucoDecimalLiteralExpression(StartIndex, EndIndex, LiteralValue, SucoType.Decimal);
        public override object Interpret(SucoEnvironment env, int?[] grid) => LiteralValue;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => new SucoConstant(StartIndex, EndIndex, SucoType.Decimal, LiteralValue);
    }
}