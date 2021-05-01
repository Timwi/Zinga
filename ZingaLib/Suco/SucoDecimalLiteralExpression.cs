namespace Zinga.Suco
{
    public class SucoDecimalLiteralExpression : SucoExpression
    {
        public double NumericalValue { get; private set; }

        public SucoDecimalLiteralExpression(int startIndex, int endIndex, double numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            NumericalValue = numericalValue;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => new SucoDecimalLiteralExpression(StartIndex, EndIndex, NumericalValue, SucoType.Decimal);
        public override object Interpret(SucoEnvironment env) => NumericalValue;
    }
}