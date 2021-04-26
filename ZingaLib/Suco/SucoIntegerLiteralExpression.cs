using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoIntegerLiteralExpression : SucoExpression
    {
        public int NumericalValue { get; private set; }

        public SucoIntegerLiteralExpression(int startIndex, int endIndex, int numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            NumericalValue = numericalValue;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context) => new SucoIntegerLiteralExpression(StartIndex, EndIndex, NumericalValue, SucoIntegerType.Instance);
        public override object Interpret(SucoEnvironment env) => NumericalValue;
    }
}