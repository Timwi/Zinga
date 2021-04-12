namespace Zinga.Suco
{
    public class SucoListShortcutCondition : SucoListCondition
    {
        public string Name { get; private set; }

        public SucoListShortcutCondition(int startIndex, int endIndex, string name)
            : base(startIndex, endIndex)
        {
            Name = name;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoListShortcutCondition(startIndex, endIndex, Name);
    }
}