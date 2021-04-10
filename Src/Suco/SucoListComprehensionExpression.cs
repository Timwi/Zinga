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
                // Deduce the type of the iterator variable
                SucoType collectionType;
                if (clause.FromVariable != null)
                {
                    var collection = newEnv.GetVariable(clause.FromVariable);
                    if (collection == null)
                        throw new SucoCompileException($"The variable “{clause.FromVariable}” is not defined.", clause.StartIndex, clause.EndIndex);
                    collectionType = collection.Type;
                }
                else
                    collectionType = env.GetVariable("cells").Type;

                if (collectionType is not SucoListType { Inner: SucoType elementType })
                    throw new SucoCompileException($"The variable “{clause.FromVariable}” does not refer to a list.", clause.StartIndex, clause.EndIndex);

                // Ensure the inner condition expressions are all implicitly convertible to booleans
                newEnv = newEnv.DeclareVariable(clause.VariableName, ((SucoListType) collectionType).Inner);
                var newConditions = clause.Conditions.Select(cond =>
                {
                    if (cond is SucoListExpressionCondition expr)
                    {
                        var innerExpression = expr.Expression.DeduceTypes(newEnv);
                        if (!innerExpression.Type.ImplicitlyConvertibleTo(SucoBooleanType.Instance))
                            throw new SucoCompileException($"A condition expression must be a boolean (or implicitly convertible to one).", expr.StartIndex, expr.EndIndex);
                        return new SucoListExpressionCondition(expr.StartIndex, expr.EndIndex, innerExpression.ImplicitlyConvertTo(SucoBooleanType.Instance));
                    }
                    return cond;
                }).ToList();
                newClauses.Add(new SucoListClause(clause.StartIndex, clause.EndIndex, clause.VariableName, clause.HasDollar, clause.HasPlus, clause.HasSingleton, clause.FromVariable, newConditions, elementType));
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
            var allSingletons = Clauses.All(cl => cl.HasSingleton);
            var anyNonSingletons = Clauses.Any(cl => !cl.HasSingleton);
            if (!allSingletons)
                js.AppendLine($"let $result = [];");
            var closeCurlies = 0;
            var newEnv = env;
            for (var i = 0; i < Clauses.Count; i++)
            {
                var clause = Clauses[i];
                newEnv = newEnv.DeclareVariable(clause.VariableName, clause.VariableType);
                var conditions = new List<string>();
                if (!clause.HasPlus && i > 0)
                    conditions.AddRange(Clauses.Take(i).Select(priorClause => $"{clause.VariableName} != {priorClause.VariableName}"));
                foreach (var cond in clause.Conditions)
                    conditions.Add(cond.GetJavaScript(newEnv));
                var condition = conditions.Count > 0 ? conditions.JoinString(" && ") : null;
                var collection = clause.FromVariable ?? (clause.HasDollar ? "allCells" : "cells");

                if (clause.HasSingleton)
                {
                    var returnFalse = allSingletons ? "return [false];" : $"{{ $result.push(false); continue; }}";
                    if (anyNonSingletons)
                        js.AppendLine("{");
                    if (condition == null)
                    {
                        js.AppendLine($"if ({collection}.length !== 1) {returnFalse}");
                        js.AppendLine($"let {clause.VariableName} = {collection}[0];");
                    }
                    else
                    {
                        js.AppendLine($"let {clause.VariableName}$ix = {collection}.findIndex({clause.VariableName} => {condition});");
                        js.AppendLine($"if ({clause.VariableName}$ix === -1) {returnFalse}");
                        js.AppendLine($"let {clause.VariableName} = {collection}[{clause.VariableName}$ix];");
                    }
                    if (anyNonSingletons)
                        closeCurlies++;
                }
                else
                {
                    js.AppendLine($"for (let {clause.VariableName} of {collection})");
                    if (condition != null)
                        js.AppendLine($"if ({condition})");
                }
            }
            if (allSingletons)
            {
                js.AppendLine($"return {(Selector == null ? Clauses[0].VariableName : Selector.GetJavaScript(newEnv).Code)};");
                if (closeCurlies > 0)
                    js.AppendLine(new string('}', closeCurlies));
            }
            else
            {
                js.AppendLine($"$result.push({(Selector == null ? Clauses[0].VariableName : Selector.GetJavaScript(newEnv).Code)});");
                if (closeCurlies > 0)
                    js.AppendLine(new string('}', closeCurlies));
                js.AppendLine("return $result;");
            }
            return $"(function() {{ {js} }})()";
        }
    }
}