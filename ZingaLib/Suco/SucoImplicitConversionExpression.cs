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

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => this;
        public override object Interpret(SucoEnvironment env, int?[] grid) => Expression.Type.InterpretImplicitConversionTo(Type, Expression.Interpret(env, grid));

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var optimized = Expression.Optimize(env, givens);
            return optimized is SucoConstant c
                ? new SucoConstant(StartIndex, EndIndex, Type, optimized.Type.InterpretImplicitConversionTo(Type, c.Value))
                : new SucoImplicitConversionExpression(StartIndex, EndIndex, optimized, Type);
        }
    }
}