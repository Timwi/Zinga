using System.Collections.Generic;
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

        public override object Interpret(SucoEnvironment env, int?[] grid)
        {
            var result = Condition.Interpret(env, grid);
            return result == null ? null : (bool) result ? True.Interpret(env, grid) : False.Interpret(env, grid);
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var condition = Condition.DeduceTypes(env, context);
            if (!(condition.Type is SucoBooleanType))
                throw new SucoCompileException($"The condition in a ?: operator must be a boolean.", Condition.StartIndex, Condition.EndIndex);

            var trueExpr = True.DeduceTypes(env, context);
            var falseExpr = False.DeduceTypes(env, context);
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

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var conditionOpt = Condition.Optimize(env, givens);
            if (conditionOpt is SucoConstant c)
                return c.Equals(true) ? True.Optimize(env, givens) : False.Optimize(env, givens);
            return new SucoConditionalExpression(StartIndex, EndIndex, conditionOpt, True.Optimize(env, givens), False.Optimize(env, givens), Type);
        }
    }
}