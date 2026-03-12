using System.Collections.Generic;
using System.Linq;
using RT.Json;
using Zinga.Suco;

namespace Zinga.Lib
{
    public sealed class ConstraintTypeInfo(int id, string name, ConstraintKind kind, string variablesJson, string logicSuco, string svgDefsSuco, string svgSuco, bool isPublic)
    {
        public int ID { get; private set; } = id;
        public string Name { get; private set; } = name;
        public ConstraintKind Kind { get; private set; } = kind;
        public string VariablesJson { get; private set; } = variablesJson;
        public string LogicSuco { get; private set; } = string.IsNullOrWhiteSpace(logicSuco) ? null : logicSuco;
        public string SvgDefsSuco { get; private set; } = string.IsNullOrWhiteSpace(svgDefsSuco) ? null : svgDefsSuco;
        public string SvgSuco { get; private set; } = string.IsNullOrWhiteSpace(svgSuco) ? null : svgSuco;
        public bool IsPublic { get; private set; } = isPublic;

        private SucoExpression _svgDefsCache;
        public IEnumerable<string> GetSvgDefs(SucoEnvironment env, bool ignoreErrors)
        {
            if (SvgDefsSuco == null)
                return [];
            if (ignoreErrors)
            {
                try { _svgDefsCache ??= SucoParser.ParseCode(SvgDefsSuco, SucoTypeEnvironment.From(VariablesJson), SucoContext.Svg, SucoType.String.List()); }
                catch (SucoParseException) { return null; }
                catch (SucoCompileException) { return null; }
            }
            else
                _svgDefsCache ??= SucoParser.ParseCode(SvgDefsSuco, SucoTypeEnvironment.From(VariablesJson), SucoContext.Svg, SucoType.String.List());
            return ((IEnumerable<object>) _svgDefsCache.Interpret(env, null)).Cast<string>();
        }

        private SucoExpression _svgCache;
        public string GetSvg(SucoEnvironment env, bool ignoreErrors)
        {
            if (SvgSuco == null)
                return null;
            if (ignoreErrors)
            {
                try { _svgCache ??= SucoParser.ParseCode(SvgSuco, SucoTypeEnvironment.From(VariablesJson), SucoContext.Svg, SucoType.String); }
                catch (SucoParseException) { return null; }
                catch (SucoCompileException) { return null; }
            }
            else
                _svgCache ??= SucoParser.ParseCode(SvgSuco, SucoTypeEnvironment.From(VariablesJson), SucoContext.Svg, SucoType.String);
            return (string) _svgCache.Interpret(env, null);
        }

        public JsonDict ToJson()
        {
            var dict = new JsonDict()
            {
                ["name"] = Name,
                ["kind"] = Kind.ToString(),
                ["variables"] = new JsonRaw(VariablesJson),
                ["logic"] = LogicSuco,
                ["svg"] = SvgSuco,
                ["svgdefs"] = SvgDefsSuco
            };
            if (IsPublic)
                dict["public"] = true;
            return dict;
        }
    }
}
