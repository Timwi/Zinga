namespace Zinga.Suco
{
    public abstract class SucoNode
    {
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public SucoNode(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
