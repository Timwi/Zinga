namespace Zinga.Suco
{
    public class SucoIdentifierExpression : SucoExpression
    {
        public string Name { get; private set; }

        public SucoIdentifierExpression(int startIndex, int endIndex, string name)
            : base(startIndex, endIndex)
        {
            Name = name;
        }
    }
}