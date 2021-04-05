namespace Zinga.Suco
{
    public class SucoIdentifierExpression : SucoExpression
    {
        public string Name { get; private set; }

        public SucoIdentifierExpression(int startIndex, int endIndex, string name, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Name = name;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoIdentifierExpression(startIndex, endIndex, Name);
        public override SucoExpression WithType(SucoType type) => new SucoIdentifierExpression(StartIndex, EndIndex, Name, type);
    }
}