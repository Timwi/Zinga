namespace Zinga.Suco
{
    public class SucoBooleanLiteralExpression : SucoExpression
    {
        public bool LiteralValue { get; private set; }

        public SucoBooleanLiteralExpression(int startIndex, int endIndex, bool literalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            LiteralValue = literalValue;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => new SucoBooleanLiteralExpression(StartIndex, EndIndex, LiteralValue, SucoType.Boolean);
        public override object Interpret(SucoEnvironment env, int?[] grid) => LiteralValue;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => new SucoConstant(StartIndex, EndIndex, SucoType.Boolean, LiteralValue);
    }
}