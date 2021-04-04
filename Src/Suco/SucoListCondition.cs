namespace Zinga.Suco
{
    public abstract class SucoListCondition : SucoExpression
    {
        protected SucoListCondition(int startIndex, int endIndex) : base(startIndex, endIndex)
        {
        }
    }
}