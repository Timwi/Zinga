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

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoDecimalLiteralExpression(startIndex, endIndex, NumericalValue);
        public override SucoExpression WithType(SucoType type) => new SucoDecimalLiteralExpression(StartIndex, EndIndex, NumericalValue, type);
        public override SucoExpression DeduceTypes(SucoEnvironment env) => WithType(SucoDecimalType.Instance);
        public override object Interpret(Dictionary<string, object> values) => NumericalValue;
    }
}