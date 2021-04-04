using Zinga.Suco;

namespace Zinga
{
    public class SucoConditionalExpression : SucoExpression
    {
        public SucoExpression Condition { get; private set; }
        public SucoExpression True { get; private set; }
        public SucoExpression False { get; private set; }

        public SucoConditionalExpression(int startIndex, int endIndex, SucoExpression left, SucoExpression truePart, SucoExpression falsePart)
            : base(startIndex, endIndex)
        {
            Condition = left;
            True = truePart;
            False = falsePart;
        }
    }
}