using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

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

        public override object Interpret(Dictionary<string, object> values) => Elements.Select(e => e.Interpret(values)).ToArray();

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var newElements = Elements.Select(e => e.DeduceTypes(env)).ToList();
            for (var i = 0; i < newElements.Count; i++)
                if (newElements.All(e => e.Type.ImplicitlyConvertibleTo(newElements[i].Type)))
                    return new SucoArrayExpression(StartIndex, EndIndex, newElements.Select(e => e.ImplicitlyConvertTo(newElements[i].Type)).ToList(), newElements[i].Type);
            throw new SucoCompileException("This array contains elements that are not compatible with one another.", StartIndex, EndIndex);
        }
    }
}