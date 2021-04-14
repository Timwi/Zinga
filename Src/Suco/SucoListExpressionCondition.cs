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

        public override bool Interpret(Dictionary<string, object> values, IEnumerable<object> curList, object cur, int curIx, IEnumerable<object> prevList, object prev, int? prevIx) => (bool) Expression.Interpret(values);
    }
}