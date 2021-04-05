using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoListClause : SucoExpression
    {
        public string VariableName { get; private set; }
        public bool HasDollar { get; private set; }
        public bool HasPlus { get; private set; }
        public List<SucoListCondition> Conditions { get; private set; }

        public SucoListClause(int startIndex, int endIndex, string variableName, bool hasDollar, bool hasPlus, List<SucoListCondition> conditions, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            VariableName = variableName;
            HasDollar = hasDollar;
            HasPlus = hasPlus;
            Conditions = conditions;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoListClause(startIndex, endIndex, VariableName, HasDollar, HasPlus, Conditions);
        public override SucoExpression WithType(SucoType type) => new SucoListClause(StartIndex, EndIndex, VariableName, HasDollar, HasPlus, Conditions, type);
    }
}