namespace Zinga.Suco
{
    public class SucoImplicitConversionExpression : SucoExpression
    {
        public SucoExpression Expression { get; private set; }

        public SucoImplicitConversionExpression(int startIndex, int endIndex, SucoExpression inner, SucoType type)
            : base(startIndex, endIndex, type)
        {
            Expression = inner;
        }

        public override SucoExpression WithType(SucoType type) => new SucoImplicitConversionExpression(StartIndex, EndIndex, Expression, type);
        public override SucoExpression DeduceTypes(SucoEnvironment env) => this;
        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoImplicitConversionExpression(startIndex, endIndex, Expression, Type);
    }
}