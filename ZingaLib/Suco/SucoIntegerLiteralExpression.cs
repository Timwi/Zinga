namespace Zinga.Suco
{
    public class SucoIntegerLiteralExpression : SucoExpression
    {
        public int LiteralValue { get; private set; }
        public override string ToString() => LiteralValue.ToString();

        public SucoIntegerLiteralExpression(int startIndex, int endIndex, int numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            LiteralValue = numericalValue;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => new SucoIntegerLiteralExpression(StartIndex, EndIndex, LiteralValue, SucoType.Integer);
        public override object Interpret(SucoEnvironment env, int?[] grid) => LiteralValue;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => new SucoConstant(StartIndex, EndIndex, SucoType.Integer, LiteralValue);
    }
}