using System.Numerics;

namespace Zinga.Suco
{
    public class SucoNumberLiteralExpression : SucoExpression
    {
        public BigInteger NumericalValue { get; private set; }

        public SucoNumberLiteralExpression(int startIndex, int endIndex, BigInteger numericalValue)
            : base(startIndex, endIndex)
        {
            NumericalValue = numericalValue;
        }
    }
}