using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        private Dictionary<string, object> _valuesCache;
        [ClassifyIgnore]
        private SucoVariable[] _valuesCacheVariables;
        public Dictionary<string, object> DecodeValues(SucoVariable[] variables)
        {
            if (_valuesCache == null || _valuesCacheVariables != variables)
            {
                _valuesCacheVariables = variables;
                _valuesCache = ZingaUtil.ConvertVariableValues(JsonDict.Parse(ValuesJson), variables);
            }
            return _valuesCache;
        }
    }
}
