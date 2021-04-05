using System.Numerics;

namespace Zinga.Suco
{
    public class SucoListNumberCondition : SucoListCondition
    {
        private BigInteger Number;

        public SucoListNumberCondition(int startIndex, int endIndex, BigInteger number, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Number = number;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoListNumberCondition(startIndex, endIndex, Number);
        public override SucoExpression WithType(SucoType type) => new SucoListNumberCondition(StartIndex, EndIndex, Number, type);
    }
}