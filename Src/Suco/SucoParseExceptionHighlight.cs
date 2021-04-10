namespace Zinga.Suco
{
    public class SucoParseExceptionHighlight
    {
        public int StartIndex { get; private set; }
        public int? EndIndex { get; private set; }

        public SucoParseExceptionHighlight(int startIndex, int? endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public static implicit operator SucoParseExceptionHighlight(int tokenIndex) => new SucoParseExceptionHighlight(tokenIndex, null);
        public static implicit operator SucoParseExceptionHighlight(SucoExpression expr) => new SucoParseExceptionHighlight(expr.StartIndex, expr.EndIndex);
        public static implicit operator SucoParseExceptionHighlight(SucoToken token) => new SucoParseExceptionHighlight(token.StartIndex, token.EndIndex);
    }
}