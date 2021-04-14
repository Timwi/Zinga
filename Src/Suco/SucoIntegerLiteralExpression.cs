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

        public override SucoExpression DeduceTypes(SucoEnvironment env) => new SucoIntegerLiteralExpression(StartIndex, EndIndex, NumericalValue, SucoIntegerType.Instance);
        public override object Interpret(Dictionary<string, object> values) => NumericalValue;
    }
}