namespace Zinga.Suco
{
    internal class SucoListShortcutCondition : SucoListCondition
    {
        public string Name { get; private set; }

        public SucoListShortcutCondition(int startIndex, int endIndex, string name, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Name = name;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoListShortcutCondition(startIndex, endIndex, Name);
        public override SucoExpression WithType(SucoType type) => new SucoListShortcutCondition(StartIndex, EndIndex, Name, type);
    }
}