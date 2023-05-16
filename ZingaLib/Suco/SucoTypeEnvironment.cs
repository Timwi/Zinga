using System.Collections.Generic;
using System.Linq;
using RT.Json;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoTypeEnvironment
    {
        public static readonly SucoTypeEnvironment Default = new SucoTypeEnvironment()
            .DeclareVariable("allcells", SucoType.List(SucoType.Cell))
            .DeclareVariable("width", SucoType.Integer)
            .DeclareVariable("height", SucoType.Integer)
            .DeclareVariable("regions", SucoType.List(SucoType.List(SucoType.Cell)));

        public static SucoTypeEnvironment From(string variableTypesJson) => From(JsonDict.Parse(variableTypesJson));
        public static SucoTypeEnvironment From(JsonDict variableTypes)
        {
            var env = SucoTypeEnvironment.Default;
            foreach (var (key, value) in variableTypes)
                env = env.DeclareVariable(key, SucoType.Parse(value.GetString()));
            return env;
        }

        private readonly List<(string name, SucoType type, bool isInListComprehension)> _variables = new();

        private SucoTypeEnvironment() { }

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
    }
}
