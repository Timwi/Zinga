using System.Collections.Generic;
using System.Linq;
using RT.Json;
using System.Xml.Linq;
using Zinga.Suco;

namespace Zinga.Lib
{
    public sealed class ConstraintTypeInfo
    {
        public ConstraintTypeInfo(int id, ConstraintKind kind, string variablesJson, string logicSuco, string svgDefsSuco, string svgSuco)
        {
            ID = id;
            Kind = kind;
            VariablesJson = variablesJson;
            LogicSuco = logicSuco;
            SvgDefsSuco = svgDefsSuco;
            SvgSuco = svgSuco;
        }

        public int ID { get; set; }
        public ConstraintKind Kind { get; set; }
        public string VariablesJson { get; set; }
        public string LogicSuco { get; set; }
        public string SvgDefsSuco { get; set; }
        public string SvgSuco { get; set; }

        private SucoExpression _svgDefsCache;
        public IEnumerable<string> GetSvgDefs(SucoEnvironment env)
        {
            if (SvgDefsSuco == null)
                return Enumerable.Empty<string>();
            _svgDefsCache ??= SucoParser.ParseCode(SvgDefsSuco, SucoTypeEnvironment.FromVariablesJson(VariablesJson), SucoContext.Svg, SucoType.String.List());
            return ((IEnumerable<object>) _svgDefsCache.Interpret(env, null)).Cast<string>();
        }

        private SucoExpression _svgCache;
        public string GetSvg(SucoEnvironment env)
        {
            if (SvgSuco == null)
                return null;
            _svgCache ??= SucoParser.ParseCode(SvgSuco, SucoTypeEnvironment.FromVariablesJson(VariablesJson), SucoContext.Svg, SucoType.String);
            return (string) _svgCache.Interpret(env, null);
        }

        public JsonDict ToJson() => new()
        {
            ["kind"] = Kind.ToString(),
            ["variables"] = new JsonRaw(VariablesJson),
            ["logic"] = LogicSuco,
            ["svg"] = SvgSuco,
            ["svgdefs"] = SvgDefsSuco
        };
    }
}
