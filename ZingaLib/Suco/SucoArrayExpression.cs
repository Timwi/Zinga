using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;
using Zinga.Lib;

namespace Zinga.Suco
{
    public class SucoArrayExpression : SucoExpression
    {
        public List<SucoExpression> Elements { get; private set; }
        public SucoType ElementType => ((SucoListType) Type).ElementType;

        public SucoArrayExpression(int startIndex, int endIndex, List<SucoExpression> elements, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Elements = elements;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            if (Elements.Count == 0)
                throw new SucoCompileException("Empty array literals are not currently supported.", StartIndex, EndIndex);
            var newElements = Elements.Select(e => e.DeduceTypes(env, context)).ToList();
            for (var i = 0; i < newElements.Count; i++)
                if (newElements.All(e => e.Type.ImplicitlyConvertibleTo(newElements[i].Type)))
                    return new SucoArrayExpression(StartIndex, EndIndex, newElements.Select(e => e.ImplicitlyConvertTo(newElements[i].Type)).ToList(), newElements[i].Type.List());
            throw new SucoCompileException("This array contains elements that are not compatible with one another.", StartIndex, EndIndex);
        }

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var constants = ElementType.CreateArray(Elements.Count);
            var expressions = new SucoExpression[Elements.Count];
            var anyExpressions = false;

            for (var i = 0; i < Elements.Count; i++)
            {
                var opt = Elements[i].Optimize(env, givens);
                if (opt is SucoConstant c)
                    constants.SetValue(c.Value, i);
                else
                {
                    expressions[i] = opt;
                    anyExpressions = true;
                }
            }

            if (!anyExpressions)
                return new SucoConstant(StartIndex, EndIndex, Type, constants);
            return new SucoOptimizedArrayExpression(StartIndex, EndIndex, constants, expressions, Type);
        }

        public override object Interpret(SucoEnvironment env, int?[] grid)
        {
            var array = ElementType.CreateArray(Elements.Count);
            for (var i = 0; i < Elements.Count; i++)
                array.SetValue(Elements[i].Interpret(env, grid), i);
            return array;
        }
    }
}