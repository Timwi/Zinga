using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoTypeEnvironment
    {
        private readonly Dictionary<string, SucoVariable> _variables = new();

        public SucoTypeEnvironment()
        {
        }

        private SucoTypeEnvironment(Dictionary<string, SucoVariable> newVars)
        {
            _variables = newVars;
        }

        public SucoTypeEnvironment DeclareVariable(string name, SucoType type)
        {
            var copy = _variables.ToDictionary();
            copy[name] = new SucoVariable(name, type);
            return new SucoTypeEnvironment(copy);
        }

        public SucoVariable GetVariable(string name) => _variables.TryGetValue(name, out var result) ? result : throw new SucoTempCompileException($"Unknown variable “{name}”.");
        public SucoVariable[] GetVariables() => _variables.Values.ToArray();
    }
}
