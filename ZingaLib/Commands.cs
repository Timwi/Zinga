using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
                foreach (var (varName, varValue) in variablesJson)
                    env = env.DeclareVariable(varName, SucoType.Parse(varValue.GetString()));
                _parsedSuco[key] = (SucoParser.ParseCode(suco, env, context, expectedResultType), env, new Dictionary<string, object>());
            }
            return _parsedSuco[key];
        }

        public static string RenderConstraintSvgs(string constraintTypesJson, string customConstraintTypesJson, string constraintsJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.RenderConstraintSvgs(""{constraintTypesJson.CLiteralEscape()}"", ""{customConstraintTypesJson.CLiteralEscape()}"", ""{constraintsJson.CLiteralEscape()}"");");
#endif
            var constraintTypes = JsonDict.Parse(constraintTypesJson);
            var customConstraintTypes = JsonList.Parse(customConstraintTypesJson);
            var constraints = JsonList.Parse(constraintsJson);
            var resultSvgDefs = new HashSet<string>();
            var resultSvgs = Enumerable.Range(0, constraints.Count).Select(c => new JsonDict { ["global"] = false, ["svg"] = null }).ToJsonList();
            var resultErrors = new JsonList(Enumerable.Range(0, constraints.Count).Select(_ => new JsonDict()));

            for (var cIx = 0; cIx < constraints.Count; cIx++)
            {
                var constraint = constraints[cIx];
                var typeId = constraint["type"].GetInt();
                string svgCode = null;
                var type = (typeId < 0 ? customConstraintTypes[~typeId] : constraintTypes[typeId.ToString()]).GetDict();
                SucoEnvironment variableValues = null;

                void process(string parameter, SucoType expectedReturnType, Action<object> callback)
                {
                    if (type[parameter] == null || string.IsNullOrWhiteSpace(type[parameter].GetString()))
                        return;
                    try
                    {
                        var (expr, env, cache) = parseSuco(type[parameter].GetString(), type["variables"].GetDict(), SucoContext.Svg, expectedReturnType);
                        var cacheKey = constraint["values"].ToString();
                        if (!cache.ContainsKey(cacheKey))
                        {
                            variableValues ??= ZingaUtil.ConvertVariableValues(type["variables"].GetDict(), constraint["values"].GetDict());
                            cache[cacheKey] = expr.Interpret(variableValues, null);
                        }
                        callback(cache[cacheKey]);
                    }
                    catch (SucoCompileException sce)
                    {
                        resultErrors[cIx].Add(parameter, new JsonDict { ["start"] = sce.StartIndex, ["end"] = sce.EndIndex, ["msg"] = sce.Message });
                    }
                    catch (SucoParseException spe)
                    {
                        resultErrors[cIx].Add(parameter, new JsonDict { ["start"] = spe.StartIndex, ["end"] = spe.EndIndex, ["highlights"] = new JsonList(spe.Highlights.Select(h => new JsonDict { ["start"] = h.StartIndex, ["end"] = h.EndIndex })), ["msg"] = spe.Message });
                    }
                }

                process("svgdefs", SucoType.String.List(), sucoResult =>
                {
                    foreach (var result in ((IEnumerable<object>) sucoResult).Cast<string>())
                    {
                        var svgError = checkSvg(result);
                        if (svgError == null)
                            resultSvgDefs.Add(result);
                        else
                            resultErrors[cIx].Add("svgdefs", svgError.ToJson());
                    }
                });
                process("svg", SucoType.String, sucoResult =>
                {
                    svgCode = (string) sucoResult;
                    var svgError = checkSvg(svgCode);
                    if (svgError != null)
                    {
                        resultErrors[cIx].Add("svg", svgError.ToJson());
                        svgCode = null;
                    }
                });

                try
                {
                    parseSuco(type["logic"].GetString(), type["variables"].GetDict(), SucoContext.Constraint, SucoType.Boolean);
                }
                catch (SucoCompileException sce)
                {
                    resultErrors[cIx].Add("logic", new JsonDict { ["start"] = sce.StartIndex, ["end"] = sce.EndIndex, ["msg"] = sce.Message });
                }
                catch (SucoParseException spe)
                {
                    resultErrors[cIx].Add("logic", new JsonDict { ["start"] = spe.StartIndex, ["end"] = spe.EndIndex, ["highlights"] = new JsonList(spe.Highlights.Select(h => new JsonDict { ["start"] = h.StartIndex, ["end"] = h.EndIndex })), ["msg"] = spe.Message });
                }

                if (svgCode != null)
                {
                    if (type["kind"].GetString() == "Global")
                        resultSvgs[cIx] = new JsonDict { ["global"] = true, ["svg"] = $"<rect x='0' y='0' width='1' height='1' rx='.1' ry='.1' fill='white' stroke='black' stroke-width='.03' />{svgCode}" };
                    else
                        resultSvgs[cIx] = new JsonDict { ["global"] = false, ["svg"] = svgCode };
                }
            }

            return new JsonDict
            {
                ["svgDefs"] = resultSvgDefs.JoinString(),
                ["svgs"] = resultSvgs,
                ["errors"] = resultErrors
            }.ToString();
        }

        enum SvgCheckerState
        {
            Text,
            Escape,
            TagName,
            AttributeName,
            AttributeValue
        }

        sealed class SvgError
        {
            public string Message;
            public int Index;
            public JsonValue ToJson() => new JsonDict { ["msg"] = Message, ["ix"] = Index, ["type"] = "svg" };
        }

        private static SvgError checkSvg(string svg)
        {
            char? s(int ix) => ix < svg.Length ? svg[ix] : null;

            var state = SvgCheckerState.Text;
            var tags = new Stack<string>();

            // Shared
            var isStart = false;
            // Escapes
            var hasHash = false;
            var hasHexa = false;
            var wasAttr = false;
            // Attribute values
            var hasApo = false;
            var hasDq = false;
            // Tag names
            var tagNameStartIx = 0;

            for (var i = 0; i < svg.Length; i++)
            {
                switch (state)
                {
                    case SvgCheckerState.Text:
                        switch (svg[i])
                        {
                            case '&':
                                state = SvgCheckerState.Escape;
                                isStart = true;
                                wasAttr = false;
                                if (hasHash = s(i + 1) == '#')
                                    i++;
                                if (hasHexa = hasHash && s(i + 1) == 'x')
                                    i++;
                                break;

                            case '<':
                                if (s(i + 1) == '/')
                                {
                                    i += 2;
                                    if (tags.Count == 0)
                                        return new SvgError { Index = i, Message = "Unmatched end tag." };
                                    var expectedName = tags.Pop();
                                    if (i + expectedName.Length >= svg.Length || !svg.Substring(i, expectedName.Length + 1).Equals(expectedName + ">", StringComparison.InvariantCultureIgnoreCase))
                                        return new SvgError { Index = i, Message = $"Unmatched end tag: expected ‘</{expectedName}>’." };
                                    i += expectedName.Length;
                                }
                                else
                                {
                                    if (i + 7 < svg.Length && svg.Substring(i + 1, 6).Equals("script", StringComparison.InvariantCultureIgnoreCase))
                                        return new SvgError { Index = i, Message = "Tag name cannot start with “script”." };
                                    state = SvgCheckerState.TagName;
                                    tagNameStartIx = i + 1;
                                }
                                break;

                            case '>':
                                return new SvgError { Index = i, Message = "Stray ‘>’ character outside of a tag." };
                        }
                        break;

                    case SvgCheckerState.Escape:
                        switch ((svg[i], isStart, hasHash, hasHexa))
                        {
                            case (';', true, _, _):
                                return new SvgError { Index = i, Message = "Empty escape sequence." };
                            case (';', false, _, _):
                                state = wasAttr ? SvgCheckerState.AttributeValue : SvgCheckerState.Text;
                                break;
                            case (((< '0') or (> '9')) and ((< 'a') or (> 'f')) and ((< 'A') or (> 'F')), _, true, true):
                                return new SvgError { Index = i, Message = "Hexadecimal digit 0–9/a–f/A–F or ‘;’ expected." };
                            case ((< '0') or (> '9'), _, true, false):
                                return new SvgError { Index = i, Message = "Digit 0–9 or ‘;’ expected." };
                            case (_, true, false, false):
                                if (!char.IsLetter(svg[i]))
                                    return new SvgError { Index = i, Message = "Letter or ‘;’ expected." };
                                isStart = false;
                                break;
                            case (_, false, false, false):
                                if (svg[i] != '_' && !char.IsLetterOrDigit(svg[i]))
                                    return new SvgError { Index = i, Message = "Letter, digit, ‘_’ or ‘;’ expected." };
                                break;
                            default:
                                isStart = false;
                                break;
                        }
                        break;

                    case SvgCheckerState.TagName:
                        if (isStart && !char.IsLetter(svg, i))
                            return new SvgError { Index = i, Message = "Start of tag name must be a letter." };
                        else if (char.IsWhiteSpace(svg, i))
                        {
                            tags.Push(svg.Substring(tagNameStartIx, i - tagNameStartIx));
                            state = SvgCheckerState.AttributeName;
                            isStart = true;
                            while (i < svg.Length && char.IsWhiteSpace(svg, i))
                                i++;
                            if (i + 2 < svg.Length && svg.Substring(i, 2).Equals("on", StringComparison.InvariantCultureIgnoreCase))
                                return new SvgError { Index = i, Message = "Attribute name cannot start with “on”." };
                            i--;
                        }
                        else if (svg[i] == '>')
                        {
                            tags.Push(svg.Substring(tagNameStartIx, i - tagNameStartIx));
                            state = SvgCheckerState.Text;
                        }
                        else if (!char.IsLetterOrDigit(svg, i) && svg[i] != '-' && svg[i] != '_' && svg[i] != ':')
                            return new SvgError { Index = i, Message = "Tag names can only contain letters, digits, ‘-’, ‘_’, or ‘:’." };
                        else
                            isStart = false;
                        break;

                    case SvgCheckerState.AttributeName:
                        if (isStart && svg[i] == '>')
                            state = SvgCheckerState.Text;
                        else if (isStart && svg[i] == '/' && s(i + 1) == '>')
                        {
                            tags.Pop();
                            state = SvgCheckerState.Text;
                            i++;
                        }
                        else if (isStart && !char.IsLetter(svg, i))
                            return new SvgError { Index = i, Message = "Start of attribute name must be a letter." };
                        else if (char.IsWhiteSpace(svg, i) || svg[i] == '=')
                        {
                            while (i < svg.Length && char.IsWhiteSpace(svg, i))
                                i++;
                            if (i == svg.Length || svg[i] != '=')
                                return new SvgError { Index = i, Message = "Attribute requires ‘=’ after attribute name." };
                            i++;
                            while (i < svg.Length && char.IsWhiteSpace(svg, i))
                                i++;
                            if (i == svg.Length)
                                return new SvgError { Index = i, Message = "Attribute requires value after ‘=’." };
                            state = SvgCheckerState.AttributeValue;
                            hasApo = svg[i] == '\'';
                            hasDq = svg[i] == '"';
                            if (!hasApo && !hasDq)
                                i--;
                        }
                        else if (!char.IsLetterOrDigit(svg, i) && svg[i] != '-' && svg[i] != '_' && svg[i] != ':')
                            return new SvgError { Index = i, Message = "Attribute names can only contain letters, digits, ‘-’, ‘_’, or ‘:’." };
                        else
                            isStart = false;
                        break;

                    case SvgCheckerState.AttributeValue:
                        if ((!hasDq && !hasApo && char.IsWhiteSpace(svg, i)) || (hasDq && svg[i] == '"') || (hasApo && svg[i] == '\''))
                        {
                            state = SvgCheckerState.AttributeName;
                            isStart = true;
                            while (i + 1 < svg.Length && char.IsWhiteSpace(svg, i + 1))
                                i++;
                        }
                        else if (!hasDq && !hasApo && svg[i] == '>')
                            state = SvgCheckerState.Text;
                        else if (svg[i] == '&')
                        {
                            state = SvgCheckerState.Escape;
                            wasAttr = true;
                        }
                        else
                            isStart = false;
                        break;
                }
            }
            if (state != SvgCheckerState.Text)
                return new SvgError
                {
                    Index = svg.Length,
                    Message = $@"SVG code abruptly ends in the middle of {state switch
                    {
                        SvgCheckerState.AttributeName => "an attribute name",
                        SvgCheckerState.AttributeValue => "an attribute value",
                        SvgCheckerState.Escape => "an escape sequence",
                        _ => "a tag name"
                    }}."
                };
            if (tags.Count > 0)
                return new SvgError { Index = svg.Length, Message = $"Unclosed ‘{tags.Peek()}’ tag." };
            return null;
        }

        public static (SucoExpression expr, JsonDict variables)[] _constraintLogic;

        public static void SetupConstraints(string givensJson, string constraintTypesJson, string customConstraintTypesJson, string constraintsJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.SetupConstraints(""{givensJson.CLiteralEscape()}"", ""{constraintTypesJson.CLiteralEscape()}"", ""{customConstraintTypesJson.CLiteralEscape()}"", ""{constraintsJson.CLiteralEscape()}"");");
#endif

            var givens = JsonList.Parse(givensJson).Select(v => v?.GetInt()).ToArray();
            var constraintTypes = JsonDict.Parse(constraintTypesJson);
            var customConstraintTypes = JsonList.Parse(customConstraintTypesJson);
            var parsedSucos = new Dictionary<int, (SucoExpression expr, JsonDict variables)>();
            var constraints = JsonList.Parse(constraintsJson);
            var allCells = Ut.NewArray(81, c => new Cell(c));

            _constraintLogic = new (SucoExpression expr, JsonDict variables)[constraints.Count];
            for (var i = 0; i < constraints.Count; i++)
            {
                var cTypeId = constraints[i]["type"].GetInt();
                var cType = cTypeId < 0 ? customConstraintTypes[~cTypeId] : constraintTypes[cTypeId.ToString()];
                if (!parsedSucos.TryGetValue(cTypeId, out var sucoTup))
                {
                    var tEnv = SucoTypeEnvironment.FromVariablesJson(cType["variables"].GetDict())
                        .DeclareVariable("allcells", SucoType.List(SucoType.Cell));
                    sucoTup = parsedSucos[cTypeId] = (SucoParser.ParseCode(cType["logic"].GetString(), tEnv, SucoContext.Constraint, SucoType.Boolean), cType["variables"].GetDict());
                }

                var env = ZingaUtil.ConvertVariableValues(sucoTup.variables, constraints[i]["values"].GetDict()).DeclareVariable("allcells", allCells);
                _constraintLogic[i] = (sucoTup.expr.Optimize(env, givens), sucoTup.variables);
            }
        }

        public static string CheckConstraints(string enteredDigitsJson, string constraintsJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.CheckConstraints(""{enteredDigitsJson.CLiteralEscape()}"", ""{constraintsJson.CLiteralEscape()}"");");
#endif

            var enteredDigits = JsonList.Parse(enteredDigitsJson).Select(v => v?.GetInt()).ToArray();
            var constraints = JsonList.Parse(constraintsJson);
            var results = new JsonList();
            var allCells = Ut.NewArray(81, c => new Cell(c));

            for (var cIx = 0; cIx < _constraintLogic.Length; cIx++)
            {
                var variableValues = ZingaUtil.ConvertVariableValues(_constraintLogic[cIx].variables, constraints[cIx]["values"].GetDict()).DeclareVariable("allcells", allCells);
                if ((bool?) _constraintLogic[cIx].expr.Interpret(variableValues, enteredDigits) == false)
                    results.Add(cIx);
            }

            return results.ToString();
        }

        public static string CompileSuco(string suco, string variableTypesJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                var env = new SucoTypeEnvironment();
                var variableTypes = JsonDict.Parse(variableTypesJson);
                foreach (var (key, value) in variableTypes)
                    env = env.DeclareVariable(key, SucoType.Parse(value.GetString()));
                var parseTree = SucoParser.ParseCode(suco, env, SucoContext.Svg, SucoType.String);
                return new JsonDict { ["status"] = "ok" }.ToString();
            }
            catch (SucoParseException e)
            {
                var html = new StringBuilder();
                var ix = 0;
                foreach (var item in (e.Highlights ?? Enumerable.Empty<SucoParseExceptionHighlight>()).Concat(new SucoParseExceptionHighlight[] { new SucoToken(0, e.StartIndex, e.EndIndex) }))
                {
                    html.Append(suco.Substring(ix, item.StartIndex - ix).HtmlEscape());
                    html.Append(item.EndIndex == null
                        ? @"<span class='marker'></span>"
                        : $@"<span class='mark'>{suco.Substring(item.StartIndex, item.EndIndex.Value - item.StartIndex).HtmlEscape()}</span>");
                    ix = item.EndIndex ?? item.StartIndex;
                }
                html.Append(suco.Substring(ix).HtmlEscape());

                return new JsonDict { ["status"] = "error", ["type"] = "parse", ["message"] = e.Message, ["start"] = e.StartIndex, ["end"] = e.EndIndex, ["highlights"] = ClassifyJson.Serialize(e.Highlights), ["html"] = html.ToString() }.ToString();
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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var regions = JsonList.Parse(regionsJson);
            var svg = new StringBuilder();
            foreach (var region in regions)
                svg.Append($"<path d='{ZingaUtil.GenerateSvgPath(region.GetList().Select(v => v.GetInt()).ToArray(), 0, 0)}' stroke='#2668ff' stroke-width='.15' opacity='.75' fill='none' />");
            return svg.ToString();
        }
    }
}
