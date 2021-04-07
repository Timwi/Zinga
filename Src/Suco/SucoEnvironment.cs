using System.Collections.Generic;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoEnvironment
    {
        private readonly Dictionary<string, SucoVariable> _variables = new Dictionary<string, SucoVariable>();

        public SucoEnvironment()
        {
        }

        private SucoEnvironment(Dictionary<string, SucoVariable> newVars)
        {
            _variables = newVars;
        }

        public SucoEnvironment DeclareVariable(string name, SucoType type)
        {
            var copy = _variables.ToDictionary();
            copy[name] = new SucoVariable(name, type);
            return new SucoEnvironment(copy);
        }

        public SucoEnvironment DeclareVariable(string name, SucoFunction function)
        {
            var copy = _variables.ToDictionary();
            copy[name] = new SucoVariable(name, function.Type, function);
            return new SucoEnvironment(copy);
        }

        public SucoVariable GetVariable(string name) => _variables.Get(name, null);
    }
}
