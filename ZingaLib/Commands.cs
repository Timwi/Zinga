using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Json;
using RT.Serialization;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Suco;

namespace Zinga.Lib
{
    public static class Commands
    {
        /*
        CONSTRAINTS
            {
                ["type"] = (int),
                ["values"] = (obj: string → value)
            }

        CONSTRAINTTYPES
            {
                ["global"] = kvp.Value.Global,
                ["logic"] = kvp.Value.LogicSuco,
                ["name"] = kvp.Value.Name,
                ["preview"] = kvp.Value.PreviewSvg,
                ["public"] = kvp.Value.Public,
                ["svgdefs"] = kvp.Value.SvgDefsSuco,
                ["svg"] = kvp.Value.SvgSuco,
                ["variables"] = new JsonRaw(kvp.Value.VariablesJson)
            }
        */

        private static readonly Dictionary<(string suco, string variablesJson, SucoContext context, string expectedResultType), (SucoExpression expr, SucoEnvironment env, Dictionary<string, object> interpreted)> _parsedSuco = new();

        private static (SucoExpression expr, SucoEnvironment env, Dictionary<string, object> interpreted) parseSuco(string suco, JsonDict variablesJson, SucoContext context, SucoType expectedResultType)
        {
            var expectedResultTypeStr = expectedResultType.ToString();
            var key = (suco, variablesJson.ToString(), context, expectedResultTypeStr);
            if (!_parsedSuco.ContainsKey(key))
            {
                var env = new SucoEnvironment();
                foreach (var (varName, varValue) in variablesJson.ToTuples())
                    env = env.DeclareVariable(varName, SucoType.Parse(varValue.GetString()));
                _parsedSuco[key] = (SucoParser.ParseCode(suco, env, context, expectedResultType), env, new Dictionary<string, object>());
            }
            return _parsedSuco[key];
        }

        public static string RenderConstraintSvgs(string constraintTypesJson, string constraintsJson)
        {
            var constraintTypes = JsonDict.Parse(constraintTypesJson);
            var constraints = JsonList.Parse(constraintsJson);
            var resultSvgDefs = new StringBuilder();
            var resultSvg = new StringBuilder();

            for (var cIx = 0; cIx < constraints.Count; cIx++)
            {
                var constraint = constraints[cIx];
                var typeId = constraint["type"].GetInt();
                var type = constraintTypes[typeId.ToString()].GetDict();
                var svgSuco = type["svg"].NullOr(svg => parseSuco(svg.GetString(), type["variables"].GetDict(), SucoContext.Svg, SucoStringType.Instance));
                var svgDefsSuco = type["svgdefs"].NullOr(svgDefs => parseSuco(svgDefs.GetString(), type["variables"].GetDict(), SucoContext.Svg, SucoStringType.Instance));
                Dictionary<string, object> variableValues = null;

                if (svgDefsSuco != null)
                {
                    var cache = svgDefsSuco.Value.interpreted;
                    var cacheKey = constraint["values"].ToString();
                    if (!cache.ContainsKey(cacheKey))
                    {
                        variableValues ??= ZingaUtil.ConvertVariableValues(constraint["values"].GetDict(), svgDefsSuco.Value.env.GetVariables());
                        cache[cacheKey] = svgDefsSuco.Value.expr.Interpret(variableValues);
                    }
                    resultSvgDefs.Append(cache[cacheKey]);
                }
                if (svgSuco != null)
                {
                    var cache = svgSuco.Value.interpreted;
                    var cacheKey = constraint["values"].ToString();
                    if (!cache.ContainsKey(cacheKey))
                    {
                        variableValues ??= ZingaUtil.ConvertVariableValues(constraint["values"].GetDict(), svgSuco.Value.env.GetVariables());
                        cache[cacheKey] = svgSuco.Value.expr.Interpret(variableValues);
                    }
                    resultSvg.Append($"<g id='constraint-svg-{cIx}'>{cache[cacheKey]}</g>");
                }
            }

            return new[] { resultSvgDefs.ToString(), resultSvg.ToString() }.ToJsonList().ToString();
        }

        public static string CompileSuco(string suco, string variableTypesJson)
        {
            try
            {
                var env = new SucoEnvironment();
                var variableTypes = JsonDict.Parse(variableTypesJson);
                foreach (var (key, value) in variableTypes.ToTuples())
                    env = env.DeclareVariable(key, SucoType.Parse(value.GetString()));
                var parseTree = SucoParser.ParseCode(suco, env, SucoContext.Svg, SucoStringType.Instance);
                return new JsonDict { ["status"] = "ok" }.ToString();
            }
            catch (SucoParseException e)
            {
                var html = new StringBuilder();
                var ix = 0;
                foreach (var item in (e.Highlights ?? Enumerable.Empty<SucoParseExceptionHighlight>()).Concat(new SucoParseExceptionHighlight[] { e.Index }))
                {
                    html.Append(suco.Substring(ix, item.StartIndex - ix).HtmlEscape());
                    html.Append(item.EndIndex == null
                        ? @"<span class='marker'></span>"
                        : $@"<span class='mark'>{suco.Substring(item.StartIndex, item.EndIndex.Value - item.StartIndex).HtmlEscape()}</span>");
                    ix = item.EndIndex ?? item.StartIndex;
                }
                html.Append(suco.Substring(ix).HtmlEscape());

                return new JsonDict { ["status"] = "error", ["type"] = "parse", ["message"] = e.Message, ["ix"] = e.Index, ["highlights"] = ClassifyJson.Serialize(e.Highlights), ["html"] = html.ToString() }.ToString();
            }
            catch (SucoCompileException e)
            {
                var html = new StringBuilder();
                html.Append(suco.Substring(0, e.StartIndex).HtmlEscape());
                html.Append($@"<span class='mark'>{suco.Substring(e.StartIndex, e.EndIndex - e.StartIndex).HtmlEscape()}</span>");
                html.Append(suco.Substring(e.EndIndex).HtmlEscape());

                return new JsonDict { ["status"] = "error", ["type"] = "compile", ["message"] = e.Message, ["start"] = e.StartIndex, ["end"] = e.EndIndex, ["html"] = html.ToString() }.ToString();
            }
            catch (Exception e)
            {
                return new JsonDict { ["status"] = "error", ["message"] = e.Message, ["type"] = e.GetType().FullName }.ToString();
            }
        }
    }
}
