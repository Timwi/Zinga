using System.Collections.Generic;

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

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoArrayExpression(startIndex, endIndex, Elements);
        public override SucoExpression WithType(SucoType type) => new SucoArrayExpression(StartIndex, EndIndex, Elements, type);
    }
}