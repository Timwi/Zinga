namespace Zinga.Suco
{
    public class SucoVariable
    {
        public string Name { get; private set; }
        public SucoType Type { get; private set; }
        public SucoFunction Function { get; private set; }  // Null if the variable is not a function

        public SucoVariable(string name, SucoType type, SucoFunction function = null)
        {
            Name = name;
            Type = type;
            Function = function;
        }
    }
}