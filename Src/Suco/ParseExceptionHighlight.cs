namespace Zinga.Suco
{
    public class ParseExceptionHighlight
    {
        public int StartIndex { get; private set; }
        public int? EndIndex { get; private set; }

        public ParseExceptionHighlight(int startIndex, int? endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public static implicit operator ParseExceptionHighlight(int tokenIndex) => new ParseExceptionHighlight(tokenIndex, null);
        public static implicit operator ParseExceptionHighlight(SucoExpression expr) => new ParseExceptionHighlight(expr.StartIndex, expr.EndIndex);
        public static implicit operator ParseExceptionHighlight(SucoToken token) => new ParseExceptionHighlight(token.StartIndex, token.EndIndex);
    }
}