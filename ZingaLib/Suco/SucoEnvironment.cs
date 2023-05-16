using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RT.Json;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Lib;

namespace Zinga.Suco
{
    public class SucoEnvironment
    {
        public static SucoEnvironment From(JsonDict variablesJson, JsonDict valuesJson, int width, int height, Cell[] allCells, int[][] regions) =>
            From(variablesJson, valuesJson, width, height, allCells, regions.Select(region => region.Select(ix => new Cell(ix, width)).ToArray()).ToArray());
        public static SucoEnvironment From(JsonDict variablesJson, JsonDict valuesJson, int width, int height, Cell[] allCells, Cell[][] regions)
        {
            object convertList(JsonList list, SucoType elementType, int width)
            {
                var result = elementType.CreateArray(list.Count);
                for (var i = 0; i < list.Count; i++)
                    result.SetValue(convertVariableValue(elementType, list[i], width), i);
                return result;
            }

            object convertVariableValue(SucoType type, JsonValue j, int width) => type switch
            {
                SucoBooleanType => j.GetBoolLenientSafe() ?? false,
                SucoCellType => (j.GetIntLenientSafe() ?? 0).Apply(cell => new Cell(cell, width)),
                SucoDecimalType => j.GetDoubleLenientSafe() ?? 0d,
                SucoIntegerType => j.GetIntLenientSafe() ?? 0,
                SucoStringType => j.GetStringLenientSafe() ?? "",
                SucoListType lst => convertList(j.GetListSafe() ?? new JsonList(), lst.ElementType, width),
                _ => throw new NotImplementedException($"Programmer has neglected to include code to deserialize “{type}”.")
            };

            var list = new List<(string name, object value)>();
            foreach (var (varName, varType) in variablesJson)
                list.Add((varName, convertVariableValue(SucoType.Parse(varType.GetString()), valuesJson[varName], width)));
            return new SucoEnvironment(list)
                .DeclareVariable("allcells", allCells)
                .DeclareVariable("width", width)
                .DeclareVariable("height", height)
                .DeclareVariable("regions", regions);
        }

        private readonly List<SucoListComprehensionVariable> _variables = new();

        public int Width => (int) GetValue("width");
        public int Height => (int) GetValue("height");

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
