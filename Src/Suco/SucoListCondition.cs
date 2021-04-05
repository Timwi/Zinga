namespace Zinga.Suco
{
    public abstract class SucoListCondition : SucoExpression
    {
        protected SucoListCondition(int startIndex, int endIndex, SucoType type = null) : base(startIndex, endIndex, type)
        {
        }
    }
}