using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RT.Util;
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

        protected override SucoExpression deduceTypes(SucoEnvironment env, SucoContext context)
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
                {
                    try
                    {
                        collectionType = env.GetVariable("cells").Type;
                    }
                    catch (SucoTempCompileException tc)
                    {
                        throw new SucoCompileException(tc.Message, clause.StartIndex, clause.EndIndex);
                    }
                }

                if (collectionType is not SucoListType { Inner: SucoType elementType })
                    throw new SucoCompileException($"The variable “{clause.FromVariable}” does not refer to a list.", clause.StartIndex, clause.EndIndex);

                // Ensure the inner condition expressions are all implicitly convertible to booleans
                newEnv = newEnv.DeclareVariable(clause.VariableName, ((SucoListType) collectionType).Inner);
                var newConditions = clause.Conditions.Select(cond =>
                {
                    if (cond is SucoListExpressionCondition expr)
                    {
                        var innerExpression = expr.Expression.DeduceTypes(newEnv, context);
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
                newSelector = Selector.DeduceTypes(newEnv, context);
                selectorType = new SucoListType(newSelector.Type);
            }

            if (anySingleton && !selectorType.Equals(new SucoListType(SucoBooleanType.Instance)))
                throw new SucoCompileException("A list comprehension with a “1” extra must have a boolean selector.", StartIndex, EndIndex);

            return new SucoListComprehensionExpression(StartIndex, EndIndex, newClauses, newSelector, selectorType);
        }

        public override object Interpret(Dictionary<string, object> values)
        {
            var collections = new IEnumerable<object>[Clauses.Count];
            var indexes = new int[Clauses.Count];
            var elements = new object[Clauses.Count];
            IEnumerable<object> recurse(int clIx, Dictionary<string, object> newValues)
            {
                if (clIx == Clauses.Count)
                {
                    yield return Selector.Interpret(newValues);
                    yield break;
                }

                var dic = newValues.ToDictionary();
                var collection = (IList) newValues[Clauses[clIx].FromVariableResolved];
                collections[clIx] = (IEnumerable<object>) collection;
                for (var i = 0; i < collection.Count; i++)
                {
                    if (!Clauses[clIx].HasPlus && indexes.Take(clIx).Contains(i))
                        continue;
                    indexes[clIx] = i;
                    elements[clIx] = collection[i];
                    dic[Clauses[clIx].VariableName] = collection[i];
                    foreach (var condition in Clauses[clIx].Conditions)
                        if (!condition.Interpret(dic, collections[clIx], elements[clIx], indexes[clIx],
                            clIx == 0 ? null : collections[clIx - 1],
                            clIx == 0 ? null : elements[clIx - 1],
                            clIx == 0 ? null : indexes[clIx - 1]))
                            goto skipped;
                    foreach (var result in recurse(clIx + 1, dic))
                        yield return result;
                    skipped:;
                }
            }
            return recurse(0, values);
        }
    }
}