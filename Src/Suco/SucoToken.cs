using System.Numerics;

namespace Zinga.Suco
{
    public struct SucoToken
    {
        public SucoTokenType Type { get; private set; }
        public string StringValue { get; private set; }
        public BigInteger NumericalValue { get; private set; }
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public SucoToken(SucoTokenType type, int startIndex, int endIndex) : this()
        {
            Type = type;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public SucoToken(SucoTokenType type, string identifier, int startIndex, int endIndex) : this()
        {
            Type = type;
            StringValue = identifier;
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
    }
}