using System.Collections;
using System.Collections.Generic;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoEnvironment
    {
        private readonly Dictionary<string, object> _variableValues = new();

        private readonly (string name, object value, IList list, int index)? _lastDeclared;
        private readonly (string name, object value, IList list, int index)? _prevLastDeclared;

        public SucoEnvironment()
        {
        }

        public SucoEnvironment(Dictionary<string, object> variableValues)
        {
            _variableValues = variableValues;
        }

        private SucoEnvironment(Dictionary<string, object> newValues, (string name, object value, IList list, int index)? prevLastDeclared, (string name, object value, IList list, int index)? lastDeclared)
        {
            _variableValues = newValues;
            _prevLastDeclared = prevLastDeclared;
            _lastDeclared = lastDeclared;
        }

        /// <summary>
        ///     Declares a new variable with a specified value (and returns the new environment). Has no effect on <see
        ///     cref="GetLastValue"/> and <see cref="GetPrevLastValue"/>.</summary>
        public SucoEnvironment DeclareVariable(string name, object value)
        {
            var copy = _variableValues.ToDictionary();
            copy[name] = value;
            return new SucoEnvironment(copy, _prevLastDeclared, _lastDeclared);
        }

        /// <summary>
        ///     Declares a new variable that is a list comprehension iterator (and returns the new environment). This shifts
        ///     <see cref="GetLastValue"/> and <see cref="GetPrevLastValue"/>.</summary>
        public SucoEnvironment DeclareVariable(string name, IList collection, int index)
        {
            var copy = _variableValues.ToDictionary();
            copy[name] = collection[index];
            return new SucoEnvironment(copy, _lastDeclared, (name, collection[index], collection, index));
        }

        public object GetValue(string variableName) => _variableValues.TryGetValue(variableName, out var value) ? value : throw new SucoTempCompileException($"The variable “{variableName}” is not defined.");

        public object GetLastValue() => _lastDeclared == null ? throw new SucoTempCompileException($"No current variable in scope from a list comprehension.") : _lastDeclared.Value.value;
        public IList GetLastList() => _lastDeclared == null ? throw new SucoTempCompileException($"No current variable in scope from a list comprehension.") : _lastDeclared.Value.list;
        public int GetLastIndex() => _lastDeclared == null ? throw new SucoTempCompileException($"No current variable in scope from a list comprehension.") : _lastDeclared.Value.index;

        public object GetPrevLastValue() => _prevLastDeclared == null ? throw new SucoTempCompileException($"No previous variable in scope from a list comprehension.") : _prevLastDeclared.Value.value;
        public IList GetPrevLastList() => _prevLastDeclared == null ? throw new SucoTempCompileException($"No previous variable in scope from a list comprehension.") : _prevLastDeclared.Value.list;
        public int GetPrevLastIndex() => _prevLastDeclared == null ? throw new SucoTempCompileException($"No previous variable in scope from a list comprehension.") : _prevLastDeclared.Value.index;
    }
}
