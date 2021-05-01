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
        private static readonly Dictionary<(string suco, string variablesJson, SucoContext context, string expectedResultType), (SucoExpression expr, SucoTypeEnvironment env, Dictionary<string, object> interpreted)> _parsedSuco = new();

        private static (SucoExpression expr, SucoTypeEnvironment env, Dictionary<string, object> interpreted) parseSuco(string suco, JsonDict variablesJson, SucoContext context, SucoType expectedResultType)
        {
            var expectedResultTypeStr = expectedResultType.ToString();
            var key = (suco, variablesJson.ToString(), context, expectedResultTypeStr);
            if (!_parsedSuco.ContainsKey(key))
            {
                var env = new SucoTypeEnvironment();
                foreach (var (varName, varValue) in variablesJson.ToTuples())
                    env = env.DeclareVariable(varName, SucoType.Parse(varValue.GetString()));
                _parsedSuco[key] = (SucoParser.ParseCode(suco, env, context, expectedResultType), env, new Dictionary<string, object>());
            }
            return _parsedSuco[key];
        }

        public static string CheckConstraints(string enteredDigitsJson, string constraintTypesJson, string customConstraintTypesJson, string constraintsJson)
        {
            var enteredDigits = JsonList.Parse(enteredDigitsJson).Select(v => v?.GetInt()).ToArray();
            var constraintTypes = JsonDict.Parse(constraintTypesJson);
            var customConstraintTypes = JsonList.Parse(customConstraintTypesJson);
            var constraints = JsonList.Parse(constraintsJson);
            var results = new JsonList();

            for (var cIx = 0; cIx < constraints.Count; cIx++)
            {
                var constraint = constraints[cIx];
                var typeId = constraint["type"].GetInt();
                var type = typeId < 0 ? customConstraintTypes[~typeId] : constraintTypes[typeId.ToString()].GetDict();
                var (expr, env, _) = parseSuco(type["logic"].GetString(), type["variables"].GetDict(), SucoContext.Constraint, SucoType.Boolean);
                var variableValues = ZingaUtil.ConvertVariableValues(constraint["values"].GetDict(), env.GetVariables(), enteredDigits)
                    .DeclareVariable("allcells", Ut.NewArray(81, cIx => new Cell(cIx, null, enteredDigits[cIx])));
                if ((bool?) expr.Interpret(variableValues) == false)
                    results.Add(cIx);
            }

            return results.ToString();
        }

        public static string RenderConstraintSvgs(string constraintTypesJson, string customConstraintTypesJson, string constraintsJson, int? editingConstraintTypeId, string editingConstraintTypeParameter)
        {
            var constraintTypes = JsonDict.Parse(constraintTypesJson);
            var customConstraintTypes = JsonList.Parse(customConstraintTypesJson);
            var constraints = JsonList.Parse(constraintsJson);
            var resultSvgDefs = new HashSet<string>();
            var resultSvg = new StringBuilder();
            JsonValue editingResult = null;

            for (var cIx = 0; cIx < constraints.Count; cIx++)
            {
                var constraint = constraints[cIx];
                var typeId = constraint["type"].GetInt();
                string svgCode = null;
                var type = (typeId < 0 ? customConstraintTypes[~typeId] : constraintTypes[typeId.ToString()]).GetDict();
                SucoEnvironment variableValues = null;

                void process(string parameter, SucoType expectedReturnType, Action<object> callback)
                {
                    if (type[parameter] == null)
                        return;
                    try
                    {
                        var (expr, env, interpreted) = parseSuco(type[parameter].GetString(), type["variables"].GetDict(), SucoContext.Svg, expectedReturnType);
                        var cache = interpreted;
                        var cacheKey = constraint["values"].ToString();
                        if (!cache.ContainsKey(cacheKey))
                        {
                            variableValues ??= ZingaUtil.ConvertVariableValues(constraint["values"].GetDict(), env.GetVariables());
                            cache[cacheKey] = expr.Interpret(variableValues);
                        }
                        callback(cache[cacheKey]);
                    }
                    catch (SucoCompileException sce)
                    {
                        if (typeId == editingConstraintTypeId && editingConstraintTypeParameter == parameter)
                            editingResult = new JsonDict { ["start"] = sce.StartIndex, ["end"] = sce.EndIndex, ["msg"] = sce.Message };
                    }
                    catch (SucoParseException spe)
                    {
                        if (typeId == editingConstraintTypeId && editingConstraintTypeParameter == parameter)
                            editingResult = new JsonDict { ["ix"] = spe.Index, ["highlights"] = new JsonList(spe.Highlights.Select(h => new JsonDict { ["start"] = h.StartIndex, ["end"] = h.EndIndex })), ["msg"] = spe.Message };
                    }
                }

                process("svgdefs", SucoType.String.List(), str => { resultSvgDefs.AddRange(((IEnumerable<object>) str).Cast<string>()); });
                process("svg", SucoType.String, str => { svgCode = (string) str; });

                if (typeId == editingConstraintTypeId && editingConstraintTypeParameter == "logic" && type["logic"] != null)
                {
                    try
                    {
                        var (expr, env, interpreted) = parseSuco(type["logic"].GetString(), type["variables"].GetDict(), SucoContext.Constraint, SucoType.Boolean);
                    }
                    catch (SucoCompileException sce)
                    {
                        editingResult = new JsonDict { ["start"] = sce.StartIndex, ["end"] = sce.EndIndex, ["msg"] = sce.Message };
                    }
                    catch (SucoParseException spe)
                    {
                        editingResult = new JsonDict { ["ix"] = spe.Index, ["highlights"] = new JsonList(spe.Highlights.Select(h => new JsonDict { ["start"] = h.StartIndex, ["end"] = h.EndIndex })), ["msg"] = spe.Message };
                    }
                }

                // Make sure to add the <g> tag even if no SVG code was generated because the JS code relies on it being there
                resultSvg.Append($"<g id='constraint-svg-{cIx}'>{svgCode}</g>");
            }

            return new JsonList { resultSvgDefs.JoinString(), resultSvg.ToString(), editingResult }.ToString();
        }

        public static string CompileSuco(string suco, string variableTypesJson)
        {
            try
            {
                var env = new SucoTypeEnvironment();
                var variableTypes = JsonDict.Parse(variableTypesJson);
                foreach (var (key, value) in variableTypes.ToTuples())
                    env = env.DeclareVariable(key, SucoType.Parse(value.GetString()));
                var parseTree = SucoParser.ParseCode(suco, env, SucoContext.Svg, SucoType.String);
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

        public static string GenerateOutline(string regionsJson)
        {
            var regions = JsonList.Parse(regionsJson);
            var svg = new StringBuilder();
            foreach (var region in regions)
                svg.Append($"<path d='{ZingaUtil.GenerateSvgPath(region.GetList().Select(v => v.GetInt()).ToArray(), 0, 0)}' stroke='#2668ff' stroke-width='.15' opacity='.75' fill='none' />");
            return svg.ToString();
        }

        public static string GetVersion() => "1.0";
    }
}
