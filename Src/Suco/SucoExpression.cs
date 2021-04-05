namespace Zinga.Suco
{
    public abstract class SucoExpression
    {
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }
        public SucoType Type { get; private set; }

        public SucoExpression(int startIndex, int endIndex, SucoType type = null)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            Type = type;
        }

        public abstract SucoExpression WithNewIndexes(int startIndex, int endIndex);
        public abstract SucoExpression WithType(SucoType type);
        //public abstract SucoExpression DeduceTypes();
    }
}
