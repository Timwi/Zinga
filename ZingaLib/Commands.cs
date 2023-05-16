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
        private static readonly Dictionary<
            /* TKey */ (string suco, string variablesJson, SucoContext context, string expectedResultType),
            /* TValue */ (SucoExpression expr, SucoTypeEnvironment env, Dictionary<string, object> interpreted)
        > _parsedSuco = new();

        private static (SucoExpression expr, SucoTypeEnvironment env, Dictionary<string, object> interpreted) parseSuco(string suco, JsonDict variablesJson, SucoContext context, SucoType expectedResultType)
        {
            var expectedResultTypeStr = expectedResultType.ToString();
            var key = (suco, variablesJson.ToString(), context, expectedResultTypeStr);
            if (!_parsedSuco.ContainsKey(key))
            {
                var env = SucoTypeEnvironment.From(variablesJson);
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
            var regions = state["regions"].GetList().Select(region => region.GetList().Select(v => v.GetInt()).ToArray()).ToArray();
            var keyExtra = $"-{regions.Select(r => r.JoinString("/")).JoinString(";")}-{width}-{height}";

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
                        var cacheKey = constraint["values"].ToString() + keyExtra;
                        if (!cache.ContainsKey(cacheKey))
                        {
                            variableValues ??= SucoEnvironment.From(type["variables"].GetDict(), constraint["values"].GetDict(), width, height, allCells, regions);
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

            var (puzzleLinesPath, framePathSvg) = ZingaUtil.RenderGridLines(regions, width, height);

            return new JsonDict
            {
                ["svgDefs"] = resultSvgDefs.JoinString(),
                ["svgs"] = resultSvgs,
                ["errors"] = resultErrors,
                ["frame"] = framePathSvg,
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

        public static (SucoExpression expr, SucoEnvironment env)[] _constraintLogic;

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
            var regions = state["regions"].GetList().Select(region => region.GetList().Select(v => v.GetInt()).ToArray()).ToArray();

            _constraintLogic = new (SucoExpression expr, SucoEnvironment env)[constraints.Count];
            for (var i = 0; i < constraints.Count; i++)
            {
                var cTypeId = constraints[i]["type"].GetInt();
                var cType = cTypeId < 0 ? customConstraintTypes[~cTypeId] : constraintTypes[cTypeId.ToString()];
                if (!parsedSucos.TryGetValue(cTypeId, out var sucoTup))
                    try
                    {
                        sucoTup = parsedSucos[cTypeId] = (
                            expr: SucoParser.ParseCode(cType["logic"].GetString(), SucoTypeEnvironment.From(cType["variables"].GetDict()), SucoContext.Constraint, SucoType.Boolean),
                            variables: cType["variables"].GetDict());
                    }
                    catch (SucoParseException spe)
                    {
                        Console.Error.WriteLine($"Constraint “{cType.Safe["name"].GetStringSafe()}” has invalid Suco and will not be enfored: {spe.StartIndex}:{spe.EndIndex}: {spe.Message}");
                        sucoTup = parsedSucos[cTypeId] = (new SucoConstant(0, 4, SucoType.Boolean, true), new JsonDict());
                    }
                    catch (SucoCompileException sce)
                    {
                        Console.Error.WriteLine($"Constraint “{cType.Safe["name"].GetStringSafe()}” has invalid Suco and will not be enfored: {sce.StartIndex}:{sce.EndIndex}: {sce.Message}");
                        sucoTup = parsedSucos[cTypeId] = (new SucoConstant(0, 4, SucoType.Boolean, true), new JsonDict());
                    }

                var env = SucoEnvironment.From(sucoTup.variables, constraints[i]["values"].GetDict(), width, height, allCells, regions);
                _constraintLogic[i] = (sucoTup.expr.Optimize(env, givens), env);
            }
        }

        public static string CheckConstraints(string enteredDigitsJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.CheckConstraints(""{enteredDigitsJson.CLiteralEscape()}"");");
#endif

            var enteredDigits = JsonList.Parse(enteredDigitsJson).Select(v => v?.GetInt()).ToArray();
            var results = new JsonList();
            for (var cIx = 0; cIx < _constraintLogic.Length; cIx++)
                if ((bool?) _constraintLogic[cIx].expr.Interpret(_constraintLogic[cIx].env, enteredDigits) == false)
                    results.Add(cIx);
            return results.ToString();
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

        private static string renderButtonRows(int width, int[] values)
        {
            const double btnHeight = .8;
            const double margin = .135;

            var valueButtons = values.Select(val => (new Func<double, string>(width => $@"
                <rect x='-.3' y='{btnHeight / 2 - .3}' width='.6' height='.6' fill='white' opacity='0' stroke='none' />
                <text y='.6'>{val}</text>"), val.ToString(), 1d)).ToArray();
            var colorButtons = Enumerable.Range(0, 9).Select(color => (new Func<double, string>(width => $@"
                <rect class='color' x='-.3' y='{btnHeight / 2 - .3}' width='.6' height='.6' fill='{ZingaUtil.Colors[color]}' stroke='black' stroke-width='.01' />
                <text y='.6'>{color + 1}</text>"), $"color-{color}", 1d)).ToArray();
            var modeButtons = Ut.NewArray<(Func<double, string> getLabel, string id, double width)>(
                (width => $"<text y='.6'>Normal</text>", "normal", 1.1),
                (width => $"<text y='.6'>Corner</text>", "corner", 1),
                (width => $"<text y='.6'>Center</text>", "center", 1),
                (width => $"<text y='.6'>Color</text>", "color", .85));
            var cmdButtons = Ut.NewArray<(Func<double, string> getLabel, string id, double width)>(
                (width => $"<text y='.6'>Clear</text>", "clear", 1d),
                (width => $"<text y='.6'>Undo</text>", "undo", 1d),
                (width => $"<text y='.6'>Redo</text>", "redo", 1d),
                (width => $"<text y='.6'>More</text>", "sidebar", 1d));

            string renderButton(string id, double x, double y, double width, string labelSvg) => $@"
                <g id='{id}' class='button'>
                    <rect class='clickable' x='{x}' y='{y}' width='{width}' height='{btnHeight}' stroke-width='.025' rx='.08' ry='.08'/>
                    <g class='label' transform='translate({width / 2 + x}, {y})'>{labelSvg}</g>
                </g>";

            string renderButtonRow((Func<double, string> getLabel, string id, double width)[] btns, int row)
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

        public static string RenderPuzzleSvg(int width, int height, string regionsJson, bool rowsUnique, bool columnsUnique, string valuesJson, string constraintTypesJson, string customConstraintTypesJson, string constraintsJson)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#if DEBUG
            Console.WriteLine($@"Zinga.Lib.Commands.RenderPuzzleSvg({width}, {height}, ""{regionsJson.CLiteralEscape()}"", {rowsUnique.ToString().ToLowerInvariant()}, {columnsUnique.ToString().ToLowerInvariant()}, ""{valuesJson.CLiteralEscape()}"", ""{constraintTypesJson.CLiteralEscape()}"", ""{customConstraintTypesJson.CLiteralEscape()}"", ""{constraintsJson.CLiteralEscape()}"");");
#endif
            ConstraintTypeInfo makeInfo(int id, JsonValue j) => new(id, j["name"].GetString(), ExactConvert.To<ConstraintKind>(j["kind"].GetString()), j["variables"].ToString(), j["logic"].GetString(), j["svgdefs"]?.GetString(), j["svg"]?.GetString(), j.Safe["public"].GetBoolLenientSafe() ?? false);
            var constraintTypes = JsonDict.Parse(constraintTypesJson)
                    .Select(kvp => makeInfo(int.Parse(kvp.Key), kvp.Value))
                    .ToDictionary(ct => ct.ID);
            var customConstraintTypes = JsonList.Parse(customConstraintTypesJson);
            for (var i = 0; i < customConstraintTypes.Count; i++)
                if (customConstraintTypes[i] != null)
                    constraintTypes[~i] = makeInfo(~i, customConstraintTypes[i]);

            return RenderPuzzleSvg(
                new PuzzleInfo(width, height, JsonList.Parse(regionsJson).Select(r => r.GetList().Select(v => v.GetInt()).ToArray()).ToArray(), rowsUnique, columnsUnique, JsonList.Parse(valuesJson).Select(v => v.GetInt()).ToArray()),
                constraintTypes,
                JsonList.Parse(constraintsJson).Select(j => new ConstraintInfo(j["type"].GetInt(), j["values"].ToString())).ToArray());
        }

        public static string RenderPuzzleSvg(PuzzleInfo puzzleInfo, Dictionary<int, ConstraintTypeInfo> constraintTypes, ConstraintInfo[] constraints, bool fullSvgTag = false)
        {
            var w = puzzleInfo.Width;
            var h = puzzleInfo.Height;
            var (regionDefs, regionObjects) = ZingaUtil.RenderRegionGlow(w, h, puzzleInfo.RowsUnique, puzzleInfo.ColumnsUnique, puzzleInfo.Regions);
            var allCells = Ut.NewArray(w * h, ix => new Cell(ix, w));
            var constraintEnvs = constraints?.Select(c => SucoEnvironment.From(JsonDict.Parse(constraintTypes[c.ID].VariablesJson), JsonDict.Parse(c.ValuesJson), w, h, allCells, puzzleInfo.Regions)).ToArray();
            var (puzzleLinesPath, framePathSvg) = ZingaUtil.RenderGridLines(puzzleInfo.Regions, w, h);

            var innerSvg = $@"
                <defs>
                    <filter id='constraint-invalid-shadow' x='-1' y='-1' width='500%' height='500%' filterUnits='userSpaceOnUse'>
                        <feMorphology in='SourceGraphic' operator='dilate' radius='.05' result='constraint-selection-shadow-1'></feMorphology>
                        <feColorMatrix in='constraint-selection-shadow-1' type='matrix' result='constraint-selection-shadow-2' values='0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0 0 2 0'></feColorMatrix>
                        <feGaussianBlur stdDeviation='.05' in='constraint-selection-shadow-2' result='constraint-selection-shadow-3'></feGaussianBlur>
                        <feComposite in2='constraint-selection-shadow-3' in='SourceGraphic'></feComposite>
                    </filter>
                    <filter id='glow-blur'><feGaussianBlur stdDeviation='.1' /></filter>
                    <mask id='row-invalid-mask'>
                        <rect fill='white' x='-1' y='-1' width='{w + 2}' height='3' />
                        <rect fill='black' x='0' y='0' width='{w}' height='1' />
                    </mask>
                    <mask id='column-invalid-mask'>
                        <rect fill='white' x='-1' y='-1' width='3' height='{h + 2}' />
                        <rect fill='black' x='0' y='0' width='1' height='{h}' />
                    </mask>
                    {regionDefs}
                    {constraints?.SelectMany((c, cIx) => constraintTypes[c.ID].GetSvgDefs(constraintEnvs[cIx], ignoreErrors: true)).Distinct().JoinString()}
                </defs>
                <g id='bb-everything'>
                    <g id='bb-buttons' transform='translate(0, {h + .4})'><g id='bb-buttons-scaler' font-size='.55' text-anchor='middle'>{renderButtonRows(w, puzzleInfo.Values)}</g></g>

                    <g id='bb-puzzle'>
                        <g id='constraint-svg-global'>{constraints?
                            .Select((c, cIx) => (constraint: c, cIx))
                            .Where(tup => constraintTypes[tup.constraint.ID].Kind == ConstraintKind.Global)
                            .Select((tup, ix) => $"<g transform='translate(0, {1.5 * ix})' class='constraint-svg' id='constraint-svg-{tup.cIx}'><rect x='0' y='0' width='1' height='1' rx='.1' ry='.1' fill='white' stroke='black' stroke-width='.03' />{constraintTypes[tup.constraint.ID].GetSvg(constraintEnvs[tup.cIx], ignoreErrors: true)}</g>")
                            .JoinString()}</g>

                        {Enumerable.Range(0, w * h).Select(cell => $@"<g class='cell' data-cell='{cell}' font-size='.25' stroke-width='0'>
                            <rect class='clickable sudoku-cell' data-cell='{cell}' x='{cell % w}' y='{cell / w}' width='1' height='1' />
                            <g id='sudoku-multicolor-{cell}' transform='translate({cell % w + .5}, {cell / w + .5})'></g>
                        </g>").JoinString()}

                        <path d='{puzzleLinesPath}' fill='none' stroke='black' stroke-width='.01' />
                        <path d='{framePathSvg}' fill='none' stroke='black' stroke-width='.05' />

                        {regionObjects}

                        <g id='constraint-svg'>{constraints?.Select((c, cIx) => constraintTypes[c.ID].Kind == ConstraintKind.Global ? null : $"<g class='constraint-svg' id='constraint-svg-{cIx}'>{constraintTypes[c.ID].GetSvg(constraintEnvs[cIx], ignoreErrors: true)}</g>").JoinString()}</g>

                        {Enumerable.Range(0, w * h).Select(cell => $@"<g class='cell' data-cell='{cell}' font-size='.25' stroke-width='0'>
                            <text id='sudoku-text-{cell}' x='{cell % w + .5}' y='{cell / w + .725}' font-size='.65'></text>
                            <text class='notation' id='sudoku-center-text-{cell}' font-size='.3'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-0' x='{cell % w + .1}' y='{cell / w + .3}' text-anchor='start'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-1' x='{cell % w + .9}' y='{cell / w + .3}' text-anchor='end'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-2' x='{cell % w + .1}' y='{cell / w + .875}' text-anchor='start'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-3' x='{cell % w + .9}' y='{cell / w + .875}' text-anchor='end'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-4' x='{cell % w + .5}' y='{cell / w + .3}'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-5' x='{cell % w + .9}' y='{cell / w + .6125}' text-anchor='end'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-6' x='{cell % w + .5}' y='{cell / w + .875}'></text>
                            <text class='notation' id='sudoku-corner-text-{cell}-7' x='{cell % w + .1}' y='{cell / w + .6125}' text-anchor='start'></text>
                        </g>").JoinString()}
                    </g>
                </g>";
            return fullSvgTag ? $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='-0.4 -0.4 {w + .8} {h + 3.8}' text-anchor='middle' font-family='Bitter' id='puzzle-svg' stroke-width='.1'>{innerSvg}</svg>" : innerSvg;
        }
    }
}
