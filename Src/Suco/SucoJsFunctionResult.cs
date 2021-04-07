namespace Zinga.Suco
{
    public partial class SucoJsFunctionResult : SucoJsResult
    {
        public SucoFunction Function { get; private set; }

        public SucoJsFunctionResult(SucoFunction function)
        {
            Function = function;
        }
    }
}
