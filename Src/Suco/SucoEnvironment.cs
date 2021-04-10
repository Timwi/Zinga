using System.Collections.Generic;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoEnvironment
    {
        private readonly Dictionary<string, SucoVariable> _variables = new Dictionary<string, SucoVariable>();
        private readonly string _prevVar;
        private readonly string _curVar;

        public SucoEnvironment()
        {
        }

        private SucoEnvironment(Dictionary<string, SucoVariable> newVars, string prevVar, string curVar)
        {
            _variables = newVars;
            _prevVar = prevVar;
            _curVar = curVar;
        }

        public SucoEnvironment DeclareVariable(string name, SucoType type)
        {
            var copy = _variables.ToDictionary();
            copy[name] = new SucoVariable(name, type);
            return new SucoEnvironment(copy, _curVar, name);
        }

        public SucoEnvironment DeclareVariable(string name, SucoFunction function)
        {
            var copy = _variables.ToDictionary();
            copy[name] = new SucoVariable(name, function.Type, function);
            return new SucoEnvironment(copy, _curVar, name);
        }

        public SucoVariable GetVariable(string name) => _variables.Get(name, null);
        public SucoVariable GetCurVariable() => _variables.Get(_curVar ?? throw new SucoTempCompileException($"The context does not have a current variable at this point."), null);
        public SucoVariable GetPrevVariable() => _variables.Get(_prevVar ?? throw new SucoTempCompileException($"The context does not have a previous variable at this point."), null);
    }
}
