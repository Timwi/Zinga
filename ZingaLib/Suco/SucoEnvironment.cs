using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoEnvironment
    {
        private readonly List<(string name, object value, IList list, int index)> _variables = new();

        public SucoEnvironment()
        {
        }

        public SucoEnvironment(Dictionary<string, object> variableValues)
        {
            _variables = variableValues.Select(kvp => (name: kvp.Key, value: kvp.Value, list: (IList) null, index: 0)).ToList();
        }

        /// <summary>
        ///     Declares a new variable with a specified value (and returns the new environment). Has no effect on <see
        ///     cref="GetLastValue"/> and <see cref="GetPrevLastValue"/>.</summary>
        public SucoEnvironment DeclareVariable(string name, object value)
        {
            var env = new SucoEnvironment();
            env._variables.AddRange(_variables);
            env._variables.Add((name, value, null, 0));
            return env;
        }

        /// <summary>
        ///     Declares a new variable that is a list comprehension iterator (and returns the new environment). This shifts
        ///     <see cref="GetLastValue"/> and <see cref="GetPrevLastValue"/>.</summary>
        public SucoEnvironment DeclareVariable(string name, IList collection, int index)
        {
            var env = new SucoEnvironment();
            env._variables.AddRange(_variables);
            env._variables.Add((name, collection[index], collection, index));
            return env;
        }

        public object GetValue(string variableName)
        {
            var match = _variables.FirstOrNull(tup => tup.name == variableName);
            if (match == null)
                throw new SucoTempCompileException($"The variable “{variableName}” is not defined.");
            return match.Value.value;
        }

        private int GetLastListVariableIndex(int howMany)
        {
            for (var i = _variables.Count - 1; i >= 0; i--)
            {
                if (_variables[i].list != null)
                {
                    howMany--;
                    if (howMany == 0)
                        return i;
                }
            }
            throw new SucoTempCompileException($"Not enough prior variables in scope from list comprehensions.");
        }

        public object GetLastValue() => _variables[GetLastListVariableIndex(1)].value;
        public IList GetLastList() => _variables[GetLastListVariableIndex(1)].list;
        public int GetLastIndex() => _variables[GetLastListVariableIndex(1)].index;

        public object GetPrevLastValue() => _variables[GetLastListVariableIndex(2)].value;
        public IList GetPrevLastList() => _variables[GetLastListVariableIndex(2)].list;
        public int GetPrevLastIndex() => _variables[GetLastListVariableIndex(2)].index;
    }
}
