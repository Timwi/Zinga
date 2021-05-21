using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using RT.Json;
using RT.Serialization;
using RT.Util;
using Zinga.Lib;
using Zinga.Suco;

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
        public string PreviewSvg { get; set; }
        public string Shortcut { get; set; }

        public string[] Akas
        {
            get => AkasJson.NullOr(j => JsonList.Parse(j).Select(v => v.GetString()).ToArray());
            set { AkasJson = value.NullOr(v => v.ToJsonList().ToString()); }
        }

        [ClassifyIgnore]
        private SucoExpression _svgDefsCache;
        public IEnumerable<string> GetSvgDefs(SucoEnvironment env)
        {
            if (SvgDefsSuco == null)
                return Enumerable.Empty<string>();
            _svgDefsCache ??= SucoParser.ParseCode(SvgDefsSuco, SucoTypeEnvironment.FromVariablesJson(VariablesJson), SucoContext.Svg, SucoType.String.List());
            return ((IEnumerable<object>) _svgDefsCache.Interpret(env, null)).Cast<string>();
        }

        [ClassifyIgnore]
        private SucoExpression _svgCache;
        public string GetSvg(SucoEnvironment env)
        {
            if (SvgSuco == null)
                return null;
            _svgCache ??= SucoParser.ParseCode(SvgSuco, SucoTypeEnvironment.FromVariablesJson(VariablesJson), SucoContext.Svg, SucoType.String);
            return (string) _svgCache.Interpret(env, null);
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
            if (PreviewSvg != null)
                dic["preview"] = PreviewSvg;
            if (AkasJson != null)
                dic["akas"] = new JsonRaw(AkasJson);
            return dic;
        }
    }
}