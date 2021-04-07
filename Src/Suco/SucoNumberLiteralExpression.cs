using System.Numerics;

namespace Zinga.Suco
{
    public class SucoNumberLiteralExpression : SucoExpression
    {
        public BigInteger NumericalValue { get; private set; }

        public SucoNumberLiteralExpression(int startIndex, int endIndex, BigInteger numericalValue, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            NumericalValue = numericalValue;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoNumberLiteralExpression(startIndex, endIndex, NumericalValue);
        public override SucoExpression WithType(SucoType type) => new SucoNumberLiteralExpression(StartIndex, EndIndex, NumericalValue, type);
        public override SucoExpression DeduceTypes(SucoEnvironment env) => WithType(SucoIntegerType.Instance);
        public override SucoJsResult GetJavaScript(SucoEnvironment env) => NumericalValue.ToString();
    }
}