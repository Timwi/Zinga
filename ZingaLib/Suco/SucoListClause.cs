using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoListClause : SucoNode
    {
        public string VariableName { get; private set; }
        public bool HasDollar { get; private set; }
        public bool HasPlus { get; private set; }
        public bool HasSingleton { get; private set; }
        public SucoExpression FromExpression { get; private set; }
        public List<SucoListCondition> Conditions { get; private set; }
        public SucoType VariableType { get; private set; }

        public SucoListClause(int startIndex, int endIndex, string variableName, bool hasDollar, bool hasPlus, bool hasSingleton, SucoExpression fromExpression, List<SucoListCondition> conditions, SucoType varType = null)
            : base(startIndex, endIndex)
        {
            VariableName = variableName;
            HasDollar = hasDollar;
            HasPlus = hasPlus;
            HasSingleton = hasSingleton;
            FromExpression = fromExpression;
            Conditions = conditions;
            VariableType = varType;
        }

        public SucoExpression FromExpressionResolved => FromExpression ?? (HasDollar ? new SucoIdentifierExpression(StartIndex, EndIndex, "allcells") : new SucoIdentifierExpression(StartIndex, EndIndex, "cells"));
    }
}