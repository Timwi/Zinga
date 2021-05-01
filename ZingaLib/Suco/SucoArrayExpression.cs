using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;
using Zinga.Lib;

namespace Zinga.Suco
{
    public class SucoArrayExpression : SucoExpression
    {
        public List<SucoExpression> Elements { get; private set; }

        public SucoArrayExpression(int startIndex, int endIndex, List<SucoExpression> elements, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Elements = elements;
        }

        public override object Interpret(SucoEnvironment env) => Elements.Select(e => e.Interpret(env)).ToArray();

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var newElements = Elements.Select(e => e.DeduceTypes(env, context)).ToList();
            for (var i = 0; i < newElements.Count; i++)
                if (newElements.All(e => e.Type.ImplicitlyConvertibleTo(newElements[i].Type)))
                    return new SucoArrayExpression(StartIndex, EndIndex, newElements.Select(e => e.ImplicitlyConvertTo(newElements[i].Type)).ToList(), newElements[i].Type.List());
            throw new SucoCompileException("This array contains elements that are not compatible with one another.", StartIndex, EndIndex);
        }
    }
}