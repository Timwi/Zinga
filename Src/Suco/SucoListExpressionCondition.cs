using System.Collections.Generic;

namespace Zinga.Suco
{
    internal class SucoListExpressionCondition : SucoListCondition
    {
        public SucoExpression Expression { get; private set; }

        public SucoListExpressionCondition(int startIndex, int endIndex, SucoExpression expression)
            : base(startIndex, endIndex)
        {
            Expression = expression;
        }

        public override bool Interpret(Dictionary<string, object> values, object cur, int curIx, int curCount, object prev, int? prevIx, int? prevCount) => (bool) Expression.Interpret(values);
    }
}