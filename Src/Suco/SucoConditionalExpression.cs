﻿using System.Collections.Generic;
using Zinga.Suco;

namespace Zinga
{
    public class SucoConditionalExpression : SucoExpression
    {
        public SucoExpression Condition { get; private set; }
        public SucoExpression True { get; private set; }
        public SucoExpression False { get; private set; }

        public SucoConditionalExpression(int startIndex, int endIndex, SucoExpression condition, SucoExpression truePart, SucoExpression falsePart, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Condition = condition;
            True = truePart;
            False = falsePart;
        }

        public override object Interpret(Dictionary<string, object> values) => (bool) Condition.Interpret(values) ? True.Interpret(values) : False.Interpret(values);

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var condition = Condition.DeduceTypes(env);
            if (!(condition.Type is SucoBooleanType))
                throw new SucoCompileException($"The condition in a ?: operator must be a boolean.", Condition.StartIndex, Condition.EndIndex);

            var trueExpr = True.DeduceTypes(env);
            var falseExpr = False.DeduceTypes(env);
            SucoType thisType;

            if (trueExpr.Type.Equals(falseExpr.Type))
                thisType = trueExpr.Type;
            else if (trueExpr.Type.ImplicitlyConvertibleTo(falseExpr.Type))
            {
                trueExpr = trueExpr.ImplicitlyConvertTo(falseExpr.Type);
                thisType = falseExpr.Type;
            }
            else if (falseExpr.Type.ImplicitlyConvertibleTo(trueExpr.Type))
            {
                falseExpr = falseExpr.ImplicitlyConvertTo(trueExpr.Type);
                thisType = trueExpr.Type;
            }
            else
                throw new SucoCompileException($"Types “{trueExpr.Type}” and “{falseExpr.Type}” are not compatible.", True.StartIndex, False.EndIndex);

            return new SucoConditionalExpression(StartIndex, EndIndex, condition, trueExpr, falseExpr, thisType);
        }
    }
}