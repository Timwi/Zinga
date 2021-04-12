namespace Zinga.Suco
{
    public class SucoVariable
    {
        public string Name { get; private set; }
        public SucoType Type { get; private set; }

        public SucoVariable(string name, SucoType type)
        {
            Name = name;
            Type = type;
        }
    }
}