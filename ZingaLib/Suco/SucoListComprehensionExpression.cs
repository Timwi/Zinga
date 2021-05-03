using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Lib;

namespace Zinga.Suco
{
    public class SucoListComprehensionExpression : SucoExpression
    {
        public List<SucoListClause> Clauses { get; private set; }
        public SucoExpression Selector { get; private set; }
        private SucoContext _context;

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
                    collectionType = SucoType.Cell.List();
                else
                {
                    try
                    {
                        collectionType = env.GetVariableType("cells");
                    }
                    catch (SucoTempCompileException tc)
                    {
                        throw new SucoCompileException(tc.Message, clause.StartIndex, clause.EndIndex);
                    }
                }

                if (collectionType is not SucoListType { Inner: SucoType elementType })
                    throw new SucoCompileException($"The clause for variable “{clause.VariableName}” is attempting to draw elements from something that is not a list.", clause.StartIndex, clause.EndIndex);

                // Ensure the inner condition expressions are all implicitly convertible to booleans
                newEnv = newEnv.DeclareVariable(clause.VariableName, ((SucoListType) collectionType).Inner, isInListComprehension: true);
                var newConditions = clause.Conditions.Select(cond => cond.DeduceTypes(newEnv, context, elementType)).ToList();
                newClauses.Add(new SucoListClause(clause.StartIndex, clause.EndIndex, clause.VariableName, clause.HasDollar, clause.HasPlus, clause.HasSingleton, newFromExpression, newConditions, elementType));
                if (clause.HasSingleton)
                    anySingleton = true;
            }

            SucoType resultType;
            SucoExpression newSelector = null;
            if (newClauses.Count == 1 && Selector == null)
                resultType = newClauses[0].VariableType.List();
            else if (Selector == null)
                throw new SucoCompileException("A list comprehension without a selector cannot have more than one clause.", StartIndex, EndIndex);
            else
            {
                newSelector = Selector.DeduceTypes(newEnv, context);
                resultType = newSelector.Type.List();
            }

            if (anySingleton && !resultType.Equals(SucoType.Boolean.List()))
                throw new SucoCompileException("A list comprehension with a “1” extra must have a boolean selector.", StartIndex, EndIndex);

            return new SucoListComprehensionExpression(StartIndex, EndIndex, newClauses, newSelector, resultType) { _context = context };
        }

        private IList<(SucoListComprehensionVariable[] variables, List<SucoListCondition> conditions, SucoExpression expr)> optimizeClause(int clauseIx, SucoListComprehensionVariable[] variables, SucoEnvironment env, int?[] givens)
        {
            if (clauseIx == Clauses.Count)
                return new (SucoListComprehensionVariable[] variables, List<SucoListCondition> conditions, SucoExpression expr)[]
                {
                    (variables, null, Selector == null ? new SucoConstant(StartIndex, EndIndex, ((SucoListType) Type).Inner, env.GetValue(Clauses[0].VariableName)) : Selector.Optimize(env, givens))
                };

            var collection = Clauses[clauseIx].FromExpressionResolved.Optimize(env, givens);
            if (collection is not SucoConstant c)
                return null;

            var computedResults = new List<(SucoListComprehensionVariable[] variables, List<SucoListCondition> conditions, SucoExpression expr)>();
            var list = (IEnumerable) c.Value;
            var pos = 1;
            foreach (var item in list)
            {
                if (!Clauses[clauseIx].HasPlus && variables != null && variables.Any(v => v.List == list && v.Position == pos))
                    goto skipped;

                var newEnv = env.DeclareVariable(Clauses[clauseIx].VariableName, item, list, pos);
                var conditions = new List<SucoListCondition>();
                foreach (var condition in Clauses[clauseIx].Conditions)
                {
                    var condResult = condition.Optimize(newEnv, givens);
                    if (condResult == null)
                        conditions.Add(condition);
                    else if (condResult.Equals(false))
                        goto skipped;
                }
                var nextVariable = new SucoListComprehensionVariable(Clauses[clauseIx].VariableName, item, list, position: pos);
                var innerResult = optimizeClause(clauseIx + 1, variables == null ? new[] { nextVariable } : variables.Insert(variables.Length, nextVariable), newEnv, givens);
                if (innerResult == null)
                    return null;
                computedResults.AddRange(innerResult.Select(ir => (ir.variables, conditions: ir.conditions == null ? conditions.Count == 0 ? null : conditions : conditions.Count == 0 ? ir.conditions : ir.conditions.Concat(conditions).ToList(), ir.expr)));
                skipped:;
                pos++;
            }
            return computedResults;
        }

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var optimized = optimizeClause(0, null, env, givens);
            if (optimized == null)
                return this;

            if (optimized.All(tup => tup.conditions == null && tup.expr is SucoConstant))
            {
                var array = Array.CreateInstance(((SucoListType) Type).Inner.CsType, optimized.Count);
                for (var i = 0; i < array.Length; i++)
                    array.SetValue(((SucoConstant) optimized[i].expr).Value, i);
                return new SucoConstant(StartIndex, EndIndex, Type, array);
            }

            return new SucoOptimizedListComprehensionExpression(StartIndex, EndIndex, optimized, Type);
        }

        public override object Interpret(SucoEnvironment env, int?[] grid)
        {
            var positions = new int[Clauses.Count];
            var collections = new object[Clauses.Count];

            IEnumerable<object> recurse(int clIx, SucoEnvironment curEnv)
            {
                if (clIx == Clauses.Count)
                {
                    yield return Selector.Interpret(curEnv, grid);
                    yield break;
                }

                var anyConditionNull = false;
                var list = (IEnumerable) Clauses[clIx].FromExpressionResolved.Interpret(curEnv, grid);
                collections[clIx] = list;
                int? oneFoundPosition = null;
                object oneFoundValue = null;
                bool nullFound = false;
                var pos = 1;
                foreach (var item in list)
                {
                    if (item is Cell c)
                    {
                        // Optimization: if the cell value is not known, we’ll assume that the constraint will evaluate as null anyway
                        if (_context == SucoContext.Constraint && grid[c.Index] == null)
                        {
                            anyConditionNull = true;
                            goto skipped;
                        }

                        collections[clIx] = null;
                        if (!Clauses[clIx].HasPlus && Enumerable.Range(0, clIx).Any(ix => collections[ix] == null && positions[ix] == c.Index))
                            goto skipped;
                        positions[clIx] = c.Index;
                    }
                    else
                    {
                        if (!Clauses[clIx].HasPlus && Enumerable.Range(0, clIx).Any(ix => collections[ix] == collections[clIx] && positions[ix] == pos))
                            goto skipped;
                        positions[clIx] = pos;
                    }
                    var newEnv = curEnv.DeclareVariable(Clauses[clIx].VariableName, item, list, pos);
                    foreach (var condition in Clauses[clIx].Conditions)
                    {
                        var result = condition.Interpret(newEnv, grid);
                        if (result == false)
                            goto skipped;
                        else if (result == null && Clauses[clIx].HasSingleton)
                        {
                            nullFound = true;
                            goto skipped;
                        }
                        else if (result == null)
                        {
                            anyConditionNull = true;
                            goto skipped;
                        }
                    }
                    if (Clauses[clIx].HasSingleton)
                    {
                        if (oneFoundPosition != null)
                        {
                            yield return false;
                            yield break;
                        }
                        oneFoundPosition = pos;
                        oneFoundValue = item;
                    }
                    else
                    {
                        foreach (var result in recurse(clIx + 1, newEnv))
                            yield return result;
                    }
                    skipped:;
                    pos++;
                }

                if (Clauses[clIx].HasSingleton)
                {
                    if (oneFoundPosition == null)
                    {
                        yield return nullFound ? null : false;
                        yield break;
                    }
                    foreach (var result in recurse(clIx + 1, curEnv.DeclareVariable(Clauses[clIx].VariableName, oneFoundValue, list, oneFoundPosition.Value)))
                        yield return Equals(result, true) && nullFound ? null : result;
                }
                else if (anyConditionNull)
                    yield return null;
            }

            return recurse(0, env);
        }
    }
}