﻿using System;
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
                var env = new SucoTypeEnvironment().DeclareVariable("allcells", SucoType.List(SucoType.Cell));
                foreach (var (varName, varValue) in variablesJson)
                    env = env.DeclareVariable(varName, SucoType.Parse(varValue.GetString()));
                _parsedSuco[key] = (SucoParser.ParseCode(suco, env, context, expectedResultType), env, new Dictionary<string, object>());
            }
            return _parsedSuco[key];
        }

        public static string RenderConstraintSvgs(string constraintTypesJson, string stateJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.RenderConstraintSvgs(""{constraintTypesJson.CLiteralEscape()}"", ""{stateJson.CLiteralEscape()}"");");
#endif

            var constraintTypes = JsonDict.Parse(constraintTypesJson);
            var state = JsonDict.Parse(stateJson);

            var customConstraintTypes = state["customConstraintTypes"].GetList();
            var constraints = state["constraints"].GetList();
            var width = state["width"].GetInt();
            var height = state["height"].GetInt();
            var resultSvgDefs = new HashSet<string>();
            var resultSvgs = Enumerable.Range(0, constraints.Count).Select(c => new JsonDict { ["global"] = false, ["svg"] = null }).ToJsonList();
            var resultErrors = new JsonList(Enumerable.Range(0, constraints.Count).Select(_ => new JsonDict()));
            var allCells = Ut.NewArray(width * height, c => new Cell(c, width));

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
                        var (expr, _, cache) = parseSuco(type[parameter].GetString(), type["variables"].GetDict(), SucoContext.Svg, expectedReturnType);
                        var cacheKey = constraint["values"].ToString();
                        if (!cache.ContainsKey(cacheKey))
                        {
                            variableValues ??= ZingaUtil.ConvertVariableValues(type["variables"].GetDict(), constraint["values"].GetDict(), width)
                                .DeclareVariable("allcells", allCells)
                                .DeclareVariable("width", width)
                                .DeclareVariable("height", height);
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

            var puzzleLinesPath =
                Enumerable.Range(1, width - 1).Select(x => $"M{x} 0V{height}").JoinString() +
                Enumerable.Range(1, height - 1).Select(x => $"M0 {x}H{width}").JoinString();

            var regions = state["regions"].GetList().Select(region => region.GetList().Select(v => v.GetInt()).ToArray()).ToArray();
            var segments = new HashSet<Link>();
            // Horizontal segments
            for (var y = 0; y <= height; y++)
                for (var x = 0; x < width; x++)
                    if (y == 0 || y == height || regions.Any(r => r.Contains(x + width * y) != r.Contains(x + width * (y - 1))))
                        segments.Add(new Link(x, y, x + 1, y));
            // Vertical segments
            for (var x = 0; x <= width; x++)
                for (var y = 0; y < height; y++)
                    if (x == 0 || x == width || regions.Any(r => r.Contains(x + width * y) != r.Contains(x - 1 + width * y)))
                        segments.Add(new Link(x, y, x, y + 1));

            var framePathSvg = new StringBuilder();
            for (var y = 0; y <= height; y++)
                for (var x = 0; x <= width; x++)
                    foreach (var doVert in new[] { false, true })
                    {
                        var prevPt = new Xy(x, y);
                        var curPt = doVert ? new Xy(x, y + 1) : new Xy(x + 1, y);
                        if (segments.Remove(new Link(prevPt, curPt)))
                        {
                            var firstPt = prevPt;
                            var pts = new List<Xy> { firstPt };
                            var dir = false;
                            // Trace out a path starting from here
                            while (true)
                            {
                                // Can we continue the path in the same direction?
                                var straightPt = new Xy(2 * curPt.X - prevPt.X, 2 * curPt.Y - prevPt.Y);
                                if (segments.Remove(new Link(curPt, straightPt)))
                                {
                                    prevPt = curPt;
                                    curPt = straightPt;
                                    continue;
                                }

                                // Can we continue the path in a perpendicular direction?
                                var perpendicular1 = prevPt.X == curPt.X ? new Xy(curPt.X - 1, curPt.Y) : new Xy(curPt.X, curPt.Y - 1);
                                var perpendicular2 = prevPt.X == curPt.X ? new Xy(curPt.X + 1, curPt.Y) : new Xy(curPt.X, curPt.Y + 1);
                                var one = segments.Contains(new Link(curPt, perpendicular1));
                                var two = segments.Contains(new Link(curPt, perpendicular2));
                                if (one == two)
                                {
                                    // Check if the path may continue in the opposite direction from where we started
                                    if (!dir && !curPt.Equals(firstPt))
                                    {
                                        pts.Add(curPt);
                                        dir = true;
                                        pts.Reverse();
                                        prevPt = pts[pts.Count - 2];
                                        curPt = pts[pts.Count - 1];
                                        firstPt = pts[0];
                                        pts.RemoveAt(pts.Count - 1);
                                        continue;
                                    }
                                    framePathSvg.Append($"M{pts.Select(pt => $"{pt.X} {pt.Y}").JoinString(" ")}{(curPt.Equals(firstPt) ? "z" : $" {curPt.X} {curPt.Y}")}");
                                    break;
                                }

                                pts.Add(curPt);
                                prevPt = curPt;
                                curPt = one ? perpendicular1 : perpendicular2;
                                segments.Remove(new Link(prevPt, curPt));
                            }
                        }
                    }

            return new JsonDict
            {
                ["svgDefs"] = resultSvgDefs.JoinString(),
                ["svgs"] = resultSvgs,
                ["errors"] = resultErrors,
                ["frame"] = framePathSvg.ToString(),
                ["lines"] = puzzleLinesPath
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
                                    return new SvgError { Index = i, Message = "Letter or ‘#’ expected." };
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

        public static void SetupConstraints(string constraintTypesJson, string stateJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.SetupConstraints(""{constraintTypesJson.CLiteralEscape()}"", ""{stateJson.CLiteralEscape()}"");");
#endif

            var constraintTypes = JsonDict.Parse(constraintTypesJson);
            var state = JsonDict.Parse(stateJson);

            var givens = state["givens"].GetList().Select(v => v?.GetInt()).ToArray();
            var customConstraintTypes = state["customConstraintTypes"].GetList();
            var parsedSucos = new Dictionary<int, (SucoExpression expr, JsonDict variables)>();
            var constraints = state["constraints"].GetList();
            var width = state["width"].GetInt();
            var height = state["height"].GetInt();
            var allCells = Ut.NewArray(width * height, c => new Cell(c, width));

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

                var env = ZingaUtil.ConvertVariableValues(sucoTup.variables, constraints[i]["values"].GetDict(), width).DeclareVariable("allcells", allCells);
                _constraintLogic[i] = (sucoTup.expr.Optimize(env, givens), sucoTup.variables);
            }
        }

        public static string CheckConstraints(string enteredDigitsJson, string stateJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.CheckConstraints(""{enteredDigitsJson.CLiteralEscape()}"", ""{stateJson.CLiteralEscape()}"");");
#endif

            var state = JsonDict.Parse(stateJson);
            var enteredDigits = JsonList.Parse(enteredDigitsJson).Select(v => v?.GetInt()).ToArray();
            var constraints = state["constraints"].GetList();
            var width = state["width"].GetInt();
            var height = state["height"].GetInt();
            var results = new JsonList();
            var allCells = Ut.NewArray(width * height, c => new Cell(c, width));

            for (var cIx = 0; cIx < _constraintLogic.Length; cIx++)
            {
                var variableValues = ZingaUtil.ConvertVariableValues(_constraintLogic[cIx].variables, constraints[cIx]["values"].GetDict(), width).DeclareVariable("allcells", allCells);
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

        public static string GenerateOutline(string regionsJson, int width, int height)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var regions = JsonList.Parse(regionsJson);
            var svg = new StringBuilder();
            foreach (var region in regions)
                svg.Append($"<path d='{ZingaUtil.GenerateSvgPath(region.GetList().Select(v => v.GetInt()).ToArray(), width, height, 0, 0)}' stroke='#2668ff' stroke-width='.15' opacity='.75' fill='none' />");
            return svg.ToString();
        }

        private record struct ButtonInfo(Func<double, string> getLabel, string id, double width) { }
        private static Func<double, string> MakeButtonTextLabel(string label) => width => $"<text class='label' x='{width / 2}' y='.6'>{label}</text>";
        public static string RenderButtonRows(int width, int[] values)
        {
            const double btnHeight = .8;
            const double margin = .135;

            var valueButtons = values.Select(val => new ButtonInfo(MakeButtonTextLabel(val.ToString()), val.ToString(), 1)).ToArray();
            var colorButtons = Enumerable.Range(0, 9).Select(color => new ButtonInfo(width => $@"
                    <rect class='color' x='{width / 2 - .3}' y='{btnHeight / 2 - .3}' width='.6' height='.6' fill='{ZingaUtil.Colors[color]}' stroke='black' stroke-width='.01' />
                    <text class='label' x='{width / 2}' y='.6'>{color + 1}</text>",
                $"color-{color}", 1)).ToArray();
            var modeButtons = Ut.NewArray(
                new ButtonInfo(MakeButtonTextLabel("Normal"), "normal", 1.1),
                new ButtonInfo(MakeButtonTextLabel("Corner"), "corner", 1),
                new ButtonInfo(MakeButtonTextLabel("Center"), "center", 1),
                new ButtonInfo(MakeButtonTextLabel("Color"), "color", .85));
            var cmdButtons = Ut.NewArray(
                new ButtonInfo(MakeButtonTextLabel("Clear"), "clear", 1),
                new ButtonInfo(MakeButtonTextLabel("Undo"), "undo", 1),
                new ButtonInfo(MakeButtonTextLabel("Redo"), "redo", 1),
                new ButtonInfo(MakeButtonTextLabel("More"), "sidebar", 1));

            string renderButton(string id, double x, double y, double width, string labelSvg) => $@"
                <g class='button' id='{id}' transform='translate({x}, {y})'>
                    <rect class='clickable' x='0' y='0' width='{width}' height='{btnHeight}' stroke-width='.025' rx='.08' ry='.08'/>
                    {labelSvg}
                </g>";

            string renderButtonRow(ButtonInfo[] btns, int row)
            {
                var totalButtonWidth = width - margin * (btns.Length - 1);
                var buttonWidthWeight = btns.Sum(r => r.width);
                var widthFactor = totalButtonWidth / buttonWidthWeight;
                return btns.Select((btn, btnIx) => renderButton($"btn-{btn.id}", btns.Take(btnIx).Sum(b => b.width * widthFactor + margin),
                    row, btn.width * widthFactor, btn.getLabel(btn.width * widthFactor))).JoinString();
            }

            return $@"
                <g id='button-row-values'>{renderButtonRow(valueButtons, 0)}</g>
                <g id='button-row-colors'>{renderButtonRow(colorButtons, 0)}</g>
                <g id='button-row-mode'>{renderButtonRow(modeButtons, 1)}</g>
                <g id='button-row-cmds'>{renderButtonRow(cmdButtons, 2)}</g>";
        }
    }
}
