using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            Clauses = clauses ?? throw new ArgumentNullException(nameof(clauses));
            Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var newEnv = env;
            var newClauses = new List<SucoListClause>();
            var anySingleton = false;
            foreach (var clause in Clauses)
            {
                var newFromExpression = clause.FromExpression?.DeduceTypes(newEnv, context);

                // Deduce the type of the iterator variable
                SucoType collectionType;
                if (newFromExpression != null)
                    collectionType = newFromExpression.Type;
                else if (clause.HasDollar)
                    collectionType = new SucoListType(SucoCellType.Instance);
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
                    throw new SucoCompileException($"The clause for variable “{clause.VariableName}” is attempting to draw elements from something that is not a list.", clause.StartIndex, clause.EndIndex);

                // Ensure the inner condition expressions are all implicitly convertible to booleans
                newEnv = newEnv.DeclareVariable(clause.VariableName, ((SucoListType) collectionType).Inner);
                var newConditions = clause.Conditions.Select(cond => cond.DeduceTypes(newEnv, context, elementType)).ToList();
                newClauses.Add(new SucoListClause(clause.StartIndex, clause.EndIndex, clause.VariableName, clause.HasDollar, clause.HasPlus, clause.HasSingleton, newFromExpression, newConditions, elementType));
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

        public override object Interpret(SucoEnvironment env)
        {
            try
            {
                var indexes = new int[Clauses.Count];
                var collections = new IList[Clauses.Count];
                IEnumerable<object> recurse(int clIx, SucoEnvironment curEnv)
                {
                    if (clIx == Clauses.Count)
                    {
                        yield return Selector.Interpret(curEnv);
                        yield break;
                    }

                    var collection = (IList) Clauses[clIx].FromExpressionResolved.Interpret(curEnv);
                    collections[clIx] = collection;
                    for (var i = 0; i < collection.Count; i++)
                    {
                        if (collection[i] is Cell c)
                        {
                            collections[clIx] = null;
                            if (!Clauses[clIx].HasPlus && Enumerable.Range(0, clIx).Any(ix => collections[ix] == null && indexes[ix] == c.Index))
                                continue;
                            indexes[clIx] = c.Index;
                        }
                        else
                        {
                            if (!Clauses[clIx].HasPlus && Enumerable.Range(0, clIx).Any(ix => collections[ix] == collections[clIx] && indexes[ix] == i))
                                continue;
                            indexes[clIx] = i;
                        }
                        var newEnv = curEnv.DeclareVariable(Clauses[clIx].VariableName, collection, i);
                        foreach (var condition in Clauses[clIx].Conditions)
                        {
                            var result = condition.Interpret(newEnv);
                            if (result == false)
                                goto skipped;
                            else if (result == null)
                                throw new EvaluationIncompleteException();
                        }
                        foreach (var result in recurse(clIx + 1, newEnv))
                            yield return result;
                        skipped:;
                    }
                }
                return recurse(0, env).ToArray();
            }
            catch (EvaluationIncompleteException)
            {
                return null;
            }
        }
    }
}