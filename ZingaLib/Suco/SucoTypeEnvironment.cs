using System.Collections.Generic;
using System.Linq;
using RT.Json;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoTypeEnvironment
    {
        private readonly List<(string name, SucoType type, bool isInListComprehension)> _variables = new();

        public SucoTypeEnvironment()
        {
        }

        public SucoTypeEnvironment DeclareVariable(string name, SucoType type, bool isInListComprehension = false)
        {
            if (_variables.Any(v => v.name == name))
                throw new SucoTempCompileException($"A variable with the name “{name}” is already defined. Please use a unique variable name.");

            var ne = new SucoTypeEnvironment();
            ne._variables.AddRange(_variables);
            ne._variables.Add((name, type, isInListComprehension));
            return ne;
        }

        public SucoType GetVariableType(string name) => (_variables.FirstOrNull(tup => tup.name == name) ?? throw new SucoTempCompileException($"Unknown variable “{name}”.")).type;
        public bool IsVariableInListComprehension(string name) => (_variables.FirstOrNull(tup => tup.name == name) ?? throw new SucoTempCompileException($"Unknown variable “{name}”.")).isInListComprehension;

        public static SucoTypeEnvironment FromVariablesJson(string variablesJson) => FromVariablesJson(JsonDict.Parse(variablesJson));
        public static SucoTypeEnvironment FromVariablesJson(JsonDict variables)
        {
            var env = new SucoTypeEnvironment();
            foreach (var (name, type) in variables)
                env._variables.Add((name, SucoType.Parse(type.GetString()), false));
            return env;
        }
    }
}
