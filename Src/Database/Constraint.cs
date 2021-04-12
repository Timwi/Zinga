using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RT.Serialization;
using RT.Util;
using Zinga.Suco;

namespace Zinga.Database
{
    public sealed class Constraint
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConstraintID { get; set; }

        public bool Public { get; set; }
        public string Name { get; set; }
        public string VariablesJson { get; set; }
        public string LogicSuco { get; set; }
        public string SvgDefsSuco { get; set; }
        public string SvgSuco { get; set; }
        public string PreviewSvg { get; set; }

        [ClassifyIgnore]
        private SucoVariable[] _variablesCache;
        public SucoVariable[] Variables
        {
            get => _variablesCache ??= VariablesJson.NullOr(v => ClassifyJson.Deserialize<SucoVariable[]>(v));
            set { VariablesJson = ClassifyJson.Serialize(value).ToString(); _variablesCache = value; }
        }
    }
}