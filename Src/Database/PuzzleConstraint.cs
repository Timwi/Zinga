using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RT.Json;
using RT.Serialization;
using Zinga.Lib;
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
        private SucoEnvironment _valuesCache;
        [ClassifyIgnore]
        private string _valuesCacheVariables;
        public SucoEnvironment DecodeValues(string variablesJson)
        {
            if (_valuesCache == null || _valuesCacheVariables != variablesJson)
            {
                _valuesCacheVariables = variablesJson;
                _valuesCache = ZingaUtil.ConvertVariableValues(JsonDict.Parse(variablesJson), JsonDict.Parse(ValuesJson));
            }
            return _valuesCache;
        }

        public JsonDict ToJson() => new()
        {
            ["type"] = ConstraintID,
            ["values"] = new JsonRaw(ValuesJson)
        };
    }
}
