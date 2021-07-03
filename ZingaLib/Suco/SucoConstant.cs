namespace Zinga.Suco
{
    public class SucoConstant : SucoExpression
    {
        public object Value { get; private set; }
        public override object Interpret(SucoEnvironment env, int?[] grid) => Value;
        public override string ToString() => Value.ToString();

        public SucoConstant(int startIndex, int endIndex, SucoType type, object value)
            : base(startIndex, endIndex, type)
        {
            Value = value;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => this;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => this;
    }
}