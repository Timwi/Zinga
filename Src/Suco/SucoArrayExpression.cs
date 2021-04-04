using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoArrayExpression : SucoExpression
    {
        public List<SucoExpression> Elements { get; private set; }

        public SucoArrayExpression(int startIndex, int endIndex, List<SucoExpression> elements)
            : base(startIndex, endIndex)
        {
            Elements = elements;
        }
    }
}