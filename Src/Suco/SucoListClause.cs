using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoListClause : SucoNode
    {
        public string VariableName { get; private set; }
        public bool HasDollar { get; private set; }
        public bool HasPlus { get; private set; }
        public bool HasSingleton { get; private set; }
        public string FromVariable { get; private set; }
        public List<SucoListCondition> Conditions { get; private set; }
        public SucoType VariableType { get; private set; }

        public SucoListClause(int startIndex, int endIndex, string variableName, bool hasDollar, bool hasPlus, bool hasSingleton, string fromVariable, List<SucoListCondition> conditions, SucoType varType = null)
            : base(startIndex, endIndex)
        {
            VariableName = variableName;
            HasDollar = hasDollar;
            HasPlus = hasPlus;
            HasSingleton = hasSingleton;
            FromVariable = fromVariable;
            Conditions = conditions;
            VariableType = varType;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoListClause(startIndex, endIndex, VariableName, HasDollar, HasPlus, HasSingleton, FromVariable, Conditions);
        public string FromVariableResolved => FromVariable ?? (HasDollar ? "allcells" : "cells");
    }
}