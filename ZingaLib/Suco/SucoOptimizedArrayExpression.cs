using System;

namespace Zinga.Suco
{
    public class SucoOptimizedArrayExpression : SucoExpression
    {
        public Array Constants { get; private set; }
        public SucoExpression[] Expressions { get; private set; }

        public SucoOptimizedArrayExpression(int startIndex, int endIndex, Array constants, SucoExpression[] expressions, SucoType type)
            : base(startIndex, endIndex, type)
        {
            Constants = constants;
            Expressions = expressions;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => this;
        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens) => this;
        public override object Interpret(SucoEnvironment env, int?[] grid)
        {
            var result = (Array) Constants.Clone();
            for (var i = 0; i < Expressions.Length; i++)
                if (Expressions[i] != null)
                    result.SetValue(Expressions[i].Interpret(env, grid), i);
            return result;
        }
    }
}