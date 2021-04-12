using System.Numerics;

namespace Zinga.Suco
{
    public struct SucoToken
    {
        public SucoTokenType Type { get; private set; }
        public string StringValue { get; private set; }
        public BigInteger NumericalValue { get; private set; }
        public double DecimalValue { get; private set; }
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public SucoToken(SucoTokenType type, int startIndex, int endIndex) : this()
        {
            Type = type;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public SucoToken(SucoTokenType type, string stringValue, int startIndex, int endIndex) : this()
        {
            Type = type;
            StringValue = stringValue;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public SucoToken(SucoTokenType type, BigInteger numericalValue, int startIndex, int endIndex) : this()
        {
            Type = type;
            NumericalValue = numericalValue;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public SucoToken(SucoTokenType type, double decimalValue, int startIndex, int endIndex) : this()
        {
            Type = type;
            DecimalValue = decimalValue;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}