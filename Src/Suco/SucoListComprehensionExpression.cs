using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoListComprehensionExpression : SucoExpression
    {
        public List<SucoListClause> Clauses { get; private set; }
        public SucoExpression Selector { get; private set; }

        public SucoListComprehensionExpression(int startIndex, int endIndex, List<SucoListClause> clauses, SucoExpression selector)
            : base(startIndex, endIndex)
        {
            Clauses = clauses;
            Selector = selector;
        }
    }
}