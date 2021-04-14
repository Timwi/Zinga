using RT.Serialization;

namespace Zinga.Suco
{
    public class SucoVariable
    {
        public string Name { get; private set; }

        [ClassifySubstitute(typeof(SucoTypeClassifySubstitute))]
        public SucoType Type { get; private set; }

        public SucoVariable(string name, SucoType type)
        {
            Name = name;
            Type = type;
        }

        private SucoVariable() { }  // for Classify
    }
}