namespace Zinga.Suco
{
    public class SucoJsCodeResult : SucoJsResult
    {
        public string JsCode { get; private set; }

        public SucoJsCodeResult(string jsCode)
        {
            JsCode = jsCode;
        }

        public override string Code => JsCode;
    }
}
