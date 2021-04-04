using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoListClause : SucoExpression
    {
        public string VariableName { get; private set; }
        public bool HasDollar { get; private set; }
        public bool HasPlus { get; private set; }
        public List<SucoListCondition> Conditions { get; private set; }

        public SucoListClause(int startIndex, int endIndex, string variableName, bool hasDollar, bool hasPlus, List<SucoListCondition> conditions)
            : base(startIndex, endIndex)
        {
            VariableName = variableName;
            HasDollar = hasDollar;
            HasPlus = hasPlus;
            Conditions = conditions;
        }
    }
}