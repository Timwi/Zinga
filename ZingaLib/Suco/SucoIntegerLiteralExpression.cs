namespace Zinga.Suco
{
    public class SucoIntegerLiteralExpression : SucoExpression
    {
        public int NumericalValue { get; private set; }

        public SucoIntegerLiteralExpression(int startIndex, int endIndex, int numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            NumericalValue = numericalValue;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => new SucoIntegerLiteralExpression(StartIndex, EndIndex, NumericalValue, SucoType.Integer);
        public override object Interpret(SucoEnvironment env, int?[] grid) => NumericalValue;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => new SucoConstant(StartIndex, EndIndex, SucoType.Integer, NumericalValue);
    }
}