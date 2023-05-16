using System.Collections.Generic;
using System.Linq;
using RT.Json;
using Zinga.Suco;

namespace Zinga.Lib
{
    public sealed class ConstraintTypeInfo
    {
        public ConstraintTypeInfo(int id, string name, ConstraintKind kind, string variablesJson, string logicSuco, string svgDefsSuco, string svgSuco, bool isPublic)
        {
            ID = id;
            Name = name;
            Kind = kind;
            VariablesJson = variablesJson;
            LogicSuco = string.IsNullOrWhiteSpace(logicSuco) ? null : logicSuco;
            SvgDefsSuco = string.IsNullOrWhiteSpace(svgDefsSuco) ? null : svgDefsSuco;
            SvgSuco = string.IsNullOrWhiteSpace(svgSuco) ? null : svgSuco;
            IsPublic = isPublic;
        }

        public int ID { get; private set; }
        public string Name { get; private set; }
        public ConstraintKind Kind { get; private set; }
        public string VariablesJson { get; private set; }
        public string LogicSuco { get; private set; }
        public string SvgDefsSuco { get; private set; }
        public string SvgSuco { get; private set; }
        public bool IsPublic { get; private set; }

        private SucoExpression _svgDefsCache;
        public IEnumerable<string> GetSvgDefs(SucoEnvironment env, bool ignoreErrors)
        {
            if (SvgDefsSuco == null)
                return Enumerable.Empty<string>();
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
