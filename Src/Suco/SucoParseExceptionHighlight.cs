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

        public static implicit operator SucoParseExceptionHighlight(int tokenIndex) => new(tokenIndex, null);
        public static implicit operator SucoParseExceptionHighlight(SucoNode node) => new(node.StartIndex, node.EndIndex);
        public static implicit operator SucoParseExceptionHighlight(SucoToken token) => new(token.StartIndex, token.EndIndex);
    }
}