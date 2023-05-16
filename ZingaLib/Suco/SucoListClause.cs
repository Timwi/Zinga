using System.Collections.Generic;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;

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
        public override string ToString() => $"{(HasDollar ? "$" : "")}{(HasPlus ? "+" : "")}{(HasSingleton ? "1 " : "")}{VariableName}{FromExpression.NullOr(fe => $" from ({fe})")}{Conditions.Select(c => $" {c}").JoinString()}";

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

        public SucoExpression ResolveFromExpression() => FromExpression ?? (HasDollar ? new SucoIdentifierExpression(StartIndex, EndIndex, "allcells") : new SucoIdentifierExpression(StartIndex, EndIndex, "cells"));
    }
}