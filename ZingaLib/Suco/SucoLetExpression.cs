﻿using System.Collections.Generic;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoLetExpression : SucoExpression
    {
        public string VariableName { get; private set; }
        public SucoExpression ValueExpression { get; private set; }
        public SucoExpression InnerExpression { get; private set; }

        public SucoLetExpression(int startIndex, int endIndex, string varName, SucoExpression valueExpr, SucoExpression innerExpr, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            VariableName = varName;
            ValueExpression = valueExpr;
            InnerExpression = innerExpr;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var valueExpr = ValueExpression.DeduceTypes(env, context);
            var innerExpr = InnerExpression.DeduceTypes(env.DeclareVariable(VariableName, valueExpr.Type), context);
            return new SucoLetExpression(StartIndex, EndIndex, VariableName, valueExpr, innerExpr, innerExpr.Type);
        }

        public override object Interpret(SucoEnvironment env) => InnerExpression.Interpret(env.DeclareVariable(VariableName, ValueExpression.Interpret(env)));
    }
}