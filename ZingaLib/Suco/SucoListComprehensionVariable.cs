using System.Collections;

namespace Zinga.Suco
{
    public struct SucoListComprehensionVariable
    {
        public string Name { get; private set; }
        public object Value { get; private set; }
        public IEnumerable List { get; private set; }
        public int Position { get; private set; }

        public SucoListComprehensionVariable(string name, object value)
            : this(name, value, null, 0)
        {
        }

        public SucoListComprehensionVariable(string name, object value, IEnumerable list, int position)
        {
            Name = name;
            Value = value;
            List = list;
            Position = position;
        }
    }
}
