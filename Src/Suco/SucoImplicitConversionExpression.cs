using System.Collections.Generic;

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

        public override SucoExpression DeduceTypes(SucoEnvironment env) => this;
        public override object Interpret(Dictionary<string, object> values) => Expression.Type.InterpretImplicitConversionTo(Type, Expression.Interpret(values));
    }
}