using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using RT.Json;
using RT.Serialization;
using Zinga.Suco;

namespace Zinga.Database
{
    public sealed class PuzzleConstraint
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PuzzleConstraintID { get; set; }

        public int PuzzleID { get; set; }
        public int ConstraintID { get; set; }
        public string ValuesJson { get; set; }

        [ClassifyIgnore]
        private Dictionary<string, object> _valuesCache;
        [ClassifyIgnore]
        private SucoVariable[] _valuesCacheVariables;
        public Dictionary<string, object> DecodeValues(SucoVariable[] variables)
        {
            if (_valuesCache == null || _valuesCacheVariables != variables)
            {
                _valuesCache = new Dictionary<string, object>();
                _valuesCacheVariables = variables;
                var json = JsonDict.Parse(ValuesJson);
                foreach (var variable in variables)
                {
                    var jsonValue = json.Safe[variable.Name];
                    static object getValue(SucoType type, JsonValue j, int? position) => type switch
                    {
                        SucoBooleanType => j.GetBoolLenientSafe() ?? false,
                        SucoCellType => new Cell(j.GetIntLenientSafe() ?? 0, position),
                        SucoDecimalType => j.GetDoubleLenientSafe() ?? 0d,
                        SucoIntegerType => j.GetIntLenientSafe() ?? 0,
                        SucoStringType => j.GetStringLenientSafe() ?? "",
                        SucoListType lst => (j.GetListSafe() ?? new JsonList()).Select((v, ix) => getValue(lst.Inner, v, ix + 1)).ToArray(),
                        _ => throw new NotImplementedException($"Programmer has neglected to include code to deserialize “{type}”.")
                    };
                    _valuesCache[variable.Name] = getValue(variable.Type, jsonValue, null);
                }
            }
            return _valuesCache;
        }
    }
}
