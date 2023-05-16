using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using RT.Json;
using RT.Util;
using Zinga.Lib;

namespace Zinga.Database
{
    public sealed class Constraint
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConstraintID { get; set; }

        public bool Public { get; set; }
        public string Name { get; set; }
        public string AkasJson { get; set; }
        public string Description { get; set; }
        public ConstraintKind Kind { get; set; }
        public string VariablesJson { get; set; }
        public string LogicSuco { get; set; }
        public string SvgDefsSuco { get; set; }
        public string SvgSuco { get; set; }
        public string Shortcut { get; set; }

        [NotMapped]
        public string[] Akas
        {
            get => AkasJson.NullOr(j => JsonList.Parse(j).Select(v => v.GetString()).ToArray());
            set { AkasJson = value.NullOr(v => v.ToJsonList().ToString()); }
        }

        public JsonDict ToJson()
        {
            var dic = new JsonDict
            {
                ["name"] = Name,
                ["description"] = Description,
                ["kind"] = Kind.ToString(),
                ["variables"] = new JsonRaw(VariablesJson),
                ["logic"] = LogicSuco,
                ["svg"] = SvgSuco,
                ["svgdefs"] = SvgDefsSuco,
                ["public"] = Public
            };
            if (Shortcut != null)
                dic["shortcut"] = Shortcut;
            if (AkasJson != null)
                dic["akas"] = new JsonRaw(AkasJson);
            return dic;
        }

        public ConstraintTypeInfo ToInfo() => new(ConstraintID, Name, Kind, VariablesJson, LogicSuco, SvgDefsSuco, SvgSuco, Public);
    }
}