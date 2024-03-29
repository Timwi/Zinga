﻿using System.Collections.Generic;
using System.Reflection;

namespace Zinga.Suco
{
    public class SucoOptimizedListComprehensionExpression : SucoExpression
    {
        public IList<(SucoListComprehensionVariable[] variables, List<SucoListCondition> conditions, SucoExpression expr)> Content { get; private set; }
        private readonly SucoListComprehensionExpression _original;
        public override string ToString() => _original.ToString();

        public SucoOptimizedListComprehensionExpression(SucoListComprehensionExpression original, int startIndex, int endIndex, IList<(SucoListComprehensionVariable[] variables, List<SucoListCondition> conditions, SucoExpression expr)> content, SucoType type)
            : base(startIndex, endIndex, type)
        {
            Content = content;
            _original = original;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => this;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => this;
        public override object Interpret(SucoEnvironment env, int?[] grid) => typeof(SucoOptimizedListComprehensionExpression)
            .GetMethod(nameof(interpret), BindingFlags.NonPublic | BindingFlags.Instance)
            .MakeGenericMethod(((SucoListType) Type).ElementType.CsType)
            .Invoke(this, new object[] { env, grid });

        private IEnumerable<T> interpret<T>(SucoEnvironment env, int?[] grid)
        {
            var anyNull = false;
            for (var i = 0; i < Content.Count; i++)
            {
                var (variables, conditions, expr) = Content[i];
                var ne = env.DeclareListComprehensionVariables(variables);
                if (conditions != null)
                    foreach (var condition in conditions)
                    {
                        var result = condition.Interpret(ne, grid);
                        if (result == null)
                        {
                            anyNull = true;
                            goto skipped;
                        }
                        else if (!result.Value)
                            goto skipped;
                    }
                yield return (T) expr.Interpret(ne, grid);
                skipped:;
            }
            if (anyNull)
                yield return (T) (object) null;
        }
    }
}