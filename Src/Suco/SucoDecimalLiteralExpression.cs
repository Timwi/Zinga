using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoDecimalLiteralExpression : SucoExpression
    {
        public double NumericalValue { get; private set; }

        public SucoDecimalLiteralExpression(int startIndex, int endIndex, double numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            NumericalValue = numericalValue;
        }

        protected override SucoExpression deduceTypes(SucoEnvironment env, SucoContext context) => new SucoDecimalLiteralExpression(StartIndex, EndIndex, NumericalValue, SucoDecimalType.Instance);
        public override object Interpret(Dictionary<string, object> values) => NumericalValue;
    }
}