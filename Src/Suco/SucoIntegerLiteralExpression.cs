using System.Collections.Generic;
using System.Numerics;

namespace Zinga.Suco
{
    public class SucoIntegerLiteralExpression : SucoExpression
    {
        public BigInteger NumericalValue { get; private set; }

        public SucoIntegerLiteralExpression(int startIndex, int endIndex, BigInteger numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            NumericalValue = numericalValue;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoIntegerLiteralExpression(startIndex, endIndex, NumericalValue);
        public override SucoExpression WithType(SucoType type) => new SucoIntegerLiteralExpression(StartIndex, EndIndex, NumericalValue, type);
        public override SucoExpression DeduceTypes(SucoEnvironment env) => WithType(SucoIntegerType.Instance);
        public override object Interpret(Dictionary<string, object> values) => NumericalValue;
    }
}