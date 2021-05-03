namespace Zinga.Suco
{
    public class SucoPositionExpression : SucoExpression
    {
        public string Name { get; private set; }

        public SucoPositionExpression(int startIndex, int endIndex, string name, SucoType type)
            : base(startIndex, endIndex, type)
        {
            Name = name;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => this;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => new SucoConstant(StartIndex, EndIndex, Type, env.GetPosition(Name));
        public override object Interpret(SucoEnvironment env, int?[] grid) => env.GetPosition(Name);
    }
}