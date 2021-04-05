using Zinga.Suco;

namespace Zinga
{
    public class SucoConditionalExpression : SucoExpression
    {
        public SucoExpression Condition { get; private set; }
        public SucoExpression True { get; private set; }
        public SucoExpression False { get; private set; }

        public SucoConditionalExpression(int startIndex, int endIndex, SucoExpression left, SucoExpression truePart, SucoExpression falsePart, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Condition = left;
            True = truePart;
            False = falsePart;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoConditionalExpression(startIndex, endIndex, Condition, True, False);
        public override SucoExpression WithType(SucoType type) => new SucoConditionalExpression(StartIndex, EndIndex, Condition, True, False, type);
    }
}