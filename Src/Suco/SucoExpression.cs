namespace Zinga.Suco
{
    public abstract class SucoExpression
    {
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public SucoExpression(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
