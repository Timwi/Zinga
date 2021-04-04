using System.Collections.Generic;

namespace Zinga.Suco
{
    internal class SucoCallExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public List<SucoExpression> Arguments { get; private set; }

        public SucoCallExpression(int startIndex, int endIndex, SucoExpression operand, List<SucoExpression> arguments)
            : base(startIndex, endIndex)
        {
            Operand = operand;
            Arguments = arguments;
        }
    }
}