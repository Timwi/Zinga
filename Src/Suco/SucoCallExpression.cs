using System.Collections.Generic;

namespace Zinga.Suco
{
    internal class SucoCallExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public List<SucoExpression> Arguments { get; private set; }

        public SucoCallExpression(int startIndex, int endIndex, SucoExpression operand, List<SucoExpression> arguments, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Operand = operand;
            Arguments = arguments;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoCallExpression(startIndex, endIndex, Operand, Arguments);
        public override SucoExpression WithType(SucoType type) => new SucoCallExpression(StartIndex, EndIndex, Operand, Arguments, type);
    }
}