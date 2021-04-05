using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoListComprehensionExpression : SucoExpression
    {
        public List<SucoListClause> Clauses { get; private set; }
        public SucoExpression Selector { get; private set; }

        public SucoListComprehensionExpression(int startIndex, int endIndex, List<SucoListClause> clauses, SucoExpression selector, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Clauses = clauses;
            Selector = selector;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoListComprehensionExpression(startIndex, endIndex, Clauses, Selector);
        public override SucoExpression WithType(SucoType type) => new SucoListComprehensionExpression(StartIndex, EndIndex, Clauses, Selector, type);
    }
}