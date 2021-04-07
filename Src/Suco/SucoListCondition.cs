namespace Zinga.Suco
{
    public abstract class SucoListCondition : SucoNode
    {
        protected SucoListCondition(int startIndex, int endIndex)
            : base(startIndex, endIndex)
        {
        }

        public abstract string GetJavaScript(SucoEnvironment env);
    }
}