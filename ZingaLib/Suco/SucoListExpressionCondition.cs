﻿namespace Zinga.Suco
{
    internal class SucoListExpressionCondition : SucoListCondition
    {
        public SucoExpression Expression { get; private set; }
        public override string ToString() => $"({Expression})";

        public SucoListExpressionCondition(int startIndex, int endIndex, SucoExpression expression)
            : base(startIndex, endIndex)
        {
            Expression = expression;
        }

        public override SucoListCondition DeduceTypes(SucoTypeEnvironment env, SucoContext context, SucoType elementType)
        {
            var innerExpression = Expression.DeduceTypes(env, context);
            if (!innerExpression.Type.ImplicitlyConvertibleTo(SucoType.Boolean))
                throw new SucoCompileException($"A condition expression must be a boolean (or implicitly convertible to one).", StartIndex, EndIndex);
            return new SucoListExpressionCondition(StartIndex, EndIndex, innerExpression.ImplicitlyConvertTo(SucoType.Boolean));
        }

        public override object Optimize(SucoEnvironment env, int?[] givens) => (bool?) (Expression.Optimize(env, givens) as SucoConstant)?.Value;
        public override bool? Interpret(SucoEnvironment env, int?[] grid) => (bool?) Expression.Interpret(env, grid);
    }
}