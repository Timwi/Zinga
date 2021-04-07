using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

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

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoListComprehensionExpression(startIndex, endIndex, Clauses, Selector);
        public override SucoExpression WithType(SucoType type) => new SucoListComprehensionExpression(StartIndex, EndIndex, Clauses, Selector, type);

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var newEnv = env;
            var newClauses = new List<SucoListClause>();
            var anySingleton = false;
            foreach (var clause in Clauses)
            {
                SucoType collectionType;
                if (clause.FromVariable != null)
                {
                    var collection = newEnv.GetVariable(clause.FromVariable);
                    if (collection == null)
                        throw new SucoCompileException($"The variable “{clause.FromVariable}” is not defined.", clause.StartIndex, clause.EndIndex);
                    collectionType = collection.Type;
                    if (collectionType is not SucoListType)
                        throw new SucoCompileException($"The variable “{clause.FromVariable}” does not refer to a list.", clause.StartIndex, clause.EndIndex);
                }
                else
                    collectionType = env.GetVariable("cells").Type;

                newEnv = newEnv.DeclareVariable(clause.VariableName, ((SucoListType) collectionType).Inner);
                var newConditions = clause.Conditions.Select(cond => cond is SucoListExpressionCondition expr ? new SucoListExpressionCondition(expr.StartIndex, expr.EndIndex, expr.Expression.DeduceTypes(newEnv)) : cond).ToList();
                newClauses.Add(new SucoListClause(clause.StartIndex, clause.EndIndex, clause.VariableName, clause.HasDollar, clause.HasPlus, clause.HasSingleton, clause.FromVariable, newConditions));
                if (clause.HasSingleton)
                    anySingleton = true;
            }

            SucoType selectorType;
            SucoExpression newSelector = null;
            if (newClauses.Count == 1 && Selector == null)
                selectorType = new SucoListType(SucoCellType.Instance);
            else if (Selector == null)
                throw new SucoCompileException("A list comprehension without a selector cannot have more than one clause.", StartIndex, EndIndex);
            else
            {
                newSelector = Selector.DeduceTypes(newEnv);
                selectorType = new SucoListType(newSelector.Type);
            }

            if (anySingleton && !selectorType.Equals(new SucoListType(SucoBooleanType.Instance)))
                throw new SucoCompileException("A list comprehension with a “1” extra must have a boolean selector.", StartIndex, EndIndex);

            return new SucoListComprehensionExpression(StartIndex, EndIndex, newClauses, newSelector, selectorType);
        }

        public override SucoJsResult GetJavaScript(SucoEnvironment env)
        {
            var js = new StringBuilder();
            for (var i = 0; i < Clauses.Count; i++)
            {
                var clause = Clauses[i];
                js.AppendLine($"for (let {clause.VariableName} of {clause.FromVariable ?? (clause.HasDollar ? "allCells" : "cells")})");
                if (!clause.HasPlus && i > 0)
                    js.AppendLine($"if ({Clauses.Take(i).Select(priorClause => $"{clause.VariableName} != {priorClause.VariableName}").JoinString(" && ")})");
                foreach (var condition in clause.Conditions)
                    js.AppendLine(condition.GetJavaScript(env));
#error Unfinished
            }
        }
    }
}