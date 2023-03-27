using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoEnvironment
    {
        private readonly List<SucoListComprehensionVariable> _variables = new();

        public SucoEnvironment()
        {
        }

        public SucoEnvironment(IEnumerable<(string name, object value)> variables)
        {
            _variables.AddRange(variables.Select(tup => new SucoListComprehensionVariable(tup.name, tup.value)));
        }

        /// <summary>
        ///     Declares a new variable with a specified value (and returns the new environment). Has no effect on <see
        ///     cref="GetLastValue"/> and <see cref="GetPrevLastValue"/>.</summary>
        public SucoEnvironment DeclareVariable(string name, object value)
        {
            if (_variables.Any(v => v.Name == name))
                throw new SucoTempCompileException($"A variable with the name “{name}” is already defined. Please use a unique variable name.");
            var env = new SucoEnvironment();
            env._variables.AddRange(_variables);
            env._variables.Add(new SucoListComprehensionVariable(name, value));
            return env;
        }

        /// <summary>
        ///     Declares new variables with specified values (and returns the new environment). Has no effect on <see
        ///     cref="GetLastValue"/> and <see cref="GetPrevLastValue"/>.</summary>
        public SucoEnvironment DeclareListComprehensionVariables(params SucoListComprehensionVariable[] variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));
            if (variables.FirstOrNull(v1 => _variables.Any(v2 => v2.Name == v1.Name)) is SucoListComprehensionVariable v)
                throw new SucoTempCompileException($"A variable with the name “{v.Name}” is already defined. Please use a unique variable name.");
            var env = new SucoEnvironment();
            env._variables.AddRange(_variables);
            env._variables.AddRange(variables);
            return env;
        }

        /// <summary>
        ///     Declares a new variable that is a list comprehension iterator (and returns the new environment). This shifts
        ///     <see cref="GetLastValue"/> and <see cref="GetPrevLastValue"/>.</summary>
        public SucoEnvironment DeclareVariable(string name, object value, IEnumerable list, int position)
        {
            if (_variables.Any(v => v.Name == name))
                throw new SucoTempCompileException($"A variable with the name “{name}” is already defined. Please use a unique variable name.");
            var env = new SucoEnvironment();
            env._variables.AddRange(_variables);
            env._variables.Add(new SucoListComprehensionVariable(name, value, list, position));
            return env;
        }

        public object GetValue(string variableName)
        {
            var match = _variables.FirstOrNull(v => v.Name == variableName);
            if (match == null)
                throw new SucoTempCompileException($"The variable “{variableName}” is not defined.");
            return match.Value.Value;
        }

        public int GetPosition(string variableName)
        {
            var match = _variables.FirstOrNull(v => v.Name == variableName);
            if (match == null)
                throw new SucoTempCompileException($"The variable “{variableName}” is not defined.");
            if (match.Value.List == null)
                throw new SucoTempCompileException($"The variable “{variableName}” is not a list comprehension variable.");
            return match.Value.Position;
        }

        private int GetLastListVariableIndex(int howMany)
        {
            for (var i = _variables.Count - 1; i >= 0; i--)
            {
                if (_variables[i].List != null)
                {
                    howMany--;
                    if (howMany == 0)
                        return i;
                }
            }
            throw new SucoTempCompileException($"Not enough prior variables in scope from list comprehensions.");
        }

        public object GetLastValue() => _variables[GetLastListVariableIndex(1)].Value;
        public IEnumerable GetLastList() => _variables[GetLastListVariableIndex(1)].List;
        public int GetLastPosition() => _variables[GetLastListVariableIndex(1)].Position;

        public object GetPrevLastValue() => _variables[GetLastListVariableIndex(2)].Value;
        public IEnumerable GetPrevLastList() => _variables[GetLastListVariableIndex(2)].List;
        public int GetPrevLastPosition() => _variables[GetLastListVariableIndex(2)].Position;

        public string GetDebugString(Func<SucoListComprehensionVariable, string> selector = null) =>
            _variables
                .Select(v => (variable: v, text: selector(v)))
                .Where(v => v.text != null)
                .Select(v => $"{v.variable.Name}={v.text}")
                .JoinString(" | ");
    }
}
