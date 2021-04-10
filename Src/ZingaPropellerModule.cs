using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RT.PropellerApi;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using SvgPuzzleConstraints;
using Zinga.Database;
using Zinga.Suco;

namespace Zinga
{
    public class ZingaPropellerModule : PropellerModuleBase<ZingaSettings>
    {
        public override string Name => "Zinga";

        public override void Init()
        {
            System.Data.Entity.Database.SetInitializer(new MigrateDatabaseToLatestVersion<Db, Configuration>());
            Db.ConnectionString = Settings.ConnectionString;

            // This also triggers any pending migrations. Without doing some DB stuff here, transactions that don’t commit mess up the migrations.
            using var db = new Db();
            Log.Info($"Zinga: Number of puzzles in the database: {db.Puzzles.Count()}");
        }

        public override HttpResponse Handle(HttpRequest req)
        {
            if (req.Url.Path == "/tmp")
                return PlayWithSuco(req);

            var url = req.Url.Path.SubstringSafe(1);
            if (url.Length == 0)
                return HttpResponse.Html("<h1>404 — Not Found</h1>", HttpStatusCode._404_NotFound);

            using var db = new Db();
            url = url.UrlUnescape();
            var puzzle = db.Puzzles.FirstOrDefault(p => p.UrlName == url);
            if (puzzle == null)
                return HttpResponse.Html("<h1>404 — Not Found</h1>", HttpStatusCode._404_NotFound);

            puzzle.LastAccessed = DateTime.UtcNow;
            db.SaveChanges();

            const double btnHeight = .8;
            const double margin = .135;

            var btns = Ut.NewArray<(string label, bool isSvg, string id, double width, int row, bool color)>(9, btn => ((btn + 1).ToString(), false, (btn + 1).ToString(), .8, 0, true))
                .Concat(Ut.NewArray<(string label, bool isSvg, string id, double width, int row, bool color)>(
                    ("Normal", false, "normal", 1.1, 1, false),
                    ("Corner", false, "corner", 1, 1, false),
                    ("Center", false, "center", 1, 1, false),
                    ("Color", false, "color", .85, 1, false),

                    ("Clear", false, "clear", 1, 2, false),
                    ("Undo", false, "undo", 1, 2, false),
                    ("Redo", false, "redo", 1, 2, false),
                    ("More", false, "sidebar", 1, 2, false)));

            var hsls = new[] { 0, 30, 60, 120, 180, 210, 240, 280, 310 };
            string renderButton(string id, double x, double y, double width, string label, bool color, bool isSvg = false) => $@"
                <g class='button' id='{id}' transform='translate({x}, {y})'>
                    <rect class='clickable' x='0' y='0' width='{width}' height='{btnHeight}' stroke-width='.025' rx='.08' ry='.08'/>{(color ? $@"
                    <rect class='color' x='{width / 2 - .3}' y='{btnHeight / 2 - .3}' width='.6' height='.6' fill='hsl({hsls[int.Parse(label) - 1]}, 80%, 80%)' stroke='black' stroke-width='.01' />" : null)}
                    {(isSvg ? label : $"<text class='label' x='{width / 2}' y='.6' font-size='.55' text-anchor='middle'>{label}</text>")}
                </g>";

            string renderButtonArea((string label, bool isSvg, string id, double width, int row, bool color)[] btns, double totalWidth) => btns.GroupBy(g => g.row).SelectMany(row =>
            {
                var totalButtonWidth = totalWidth - margin * (row.Count() - 1);
                var buttonWidthWeight = row.Sum(r => r.width);
                var widthFactor = totalButtonWidth / buttonWidthWeight;

                return row.Select((btn, btnIx) =>
                    renderButton($"btn-{btn.id}", row.Take(btnIx).Sum(b => b.width * widthFactor + margin), (btnHeight + margin) * btn.row, btn.width * widthFactor, btn.label, btn.color, btn.isSvg));
            }).JoinString();

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE($"{puzzle.Title} by {puzzle.Author}"),

#if DEBUG
                    new SCRIPTLiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Puzzle.js"))),
                    new STYLELiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Puzzle.css"))),
#else
                    new SCRIPTLiteral(Resources.Js),
                    new STYLELiteral(Resources.Css),
#endif
                    new LINK { rel = "shortcut icon", type = "image/png", href = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABAAAAAQAAQMAAABF07nAAAAABlBMVEUAAAD///+l2Z/dAAACFElEQVR42u3YsQ2AMBAEwZMIKINS3RplERm38ERvodn4gokvkSRJkiRJ2qHxFrqTXJXhk+SsDCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAJEmSJEmStslFAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8EPA8Q0gSZIkSZLUflD4iAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANoBkiRJkiRJnS37yw5ZFqD7+QAAAABJRU5ErkJggg==" }),
                new BODY { class_ = "is-puzzle" }._(
                    new DIV { id = "topbar" }._(
                        new DIV { class_ = "title" }._(puzzle.Title),
                        puzzle.Author == null ? null : new DIV { class_ = "author" }._("by ", puzzle.Author)),
                    new DIV { class_ = "puzzle" }.Data("constraints", puzzle.ConstraintsJson).Data("givens", puzzle.GivensJson).Data("puzzleid", puzzle.UrlName)._(
                        new DIV { class_ = "puzzle-container", tabindex = 0 }._(new RawTag($@"
                            <svg viewBox='-0.5 -0.5 10 13.5' stroke-width='0' text-anchor='middle' font-family='Bitter' class='puzzle-svg'>
                                <defs>{puzzle.Constraints?.SelectMany(c => c.SvgDefs).Distinct().JoinString()}</defs>
                                <g class='full-puzzle'>
                                    <g transform='translate(0, 9.5)' class='button-row'>{renderButtonArea(btns, 9)}</g>
                                    <g class='global-constraints'>{puzzle.Constraints.OfType<SvgGlobalConstraint>().Select(c => c.Svg).JoinString()}</g>

                                    <g class='sudoku'>
                                        <filter id='glow-blur'><feGaussianBlur stdDeviation='.1' /></filter>
                                        <rect class='frame' id='sudoku-frame' x='0' y='0' width='9' height='9' stroke-width='.2' fill='none' filter='url(#glow-blur)'></rect>

                                        {Enumerable.Range(0, 81).Select(cell => $@"<g class='cell' id='sudoku-{cell}' font-size='.25'>
                                            <rect class='clickable sudoku-cell' data-cell='{cell}' x='{cell % 9}' y='{cell / 9}' width='1' height='1' />
                                            <g id='sudoku-multicolor-{cell}' transform='translate({cell % 9 + .5}, {cell / 9 + .5})'></g>
                                            <text id='sudoku-text-{cell}' x='{cell % 9 + .5}' y='{cell / 9 + .725}' font-size='.65'></text>

                                            <text class='notation' id='sudoku-center-text-{cell}' x='{cell % 9 + .5}' y='{cell / 9 + .62}' font-size='.3'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-0' x='{cell % 9 + .1}' y='{cell / 9 + .3}' text-anchor='start'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-1' x='{cell % 9 + .9}' y='{cell / 9 + .3}' text-anchor='end'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-2' x='{cell % 9 + .1}' y='{cell / 9 + .875}' text-anchor='start'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-3' x='{cell % 9 + .9}' y='{cell / 9 + .875}' text-anchor='end'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-4' x='{cell % 9 + .5}' y='{cell / 9 + .3}'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-5' x='{cell % 9 + .9}' y='{cell / 9 + .6125}' text-anchor='end'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-6' x='{cell % 9 + .5}' y='{cell / 9 + .875}'></text>
                                            <text class='notation' id='sudoku-corner-text-{cell}-7' x='{cell % 9 + .1}' y='{cell / 9 + .6125}' text-anchor='start'></text>
                                        </g>").JoinString()}

                                        <g>{puzzle.UnderSvg}{puzzle.Constraints?.Where(c => !(c is SvgGlobalConstraint) && !c.SvgAboveLines).Select(c => c.Svg).JoinString()}</g>

                                        <line x1='1' y1='0' x2='1' y2='9' stroke='black' stroke-width='.01' />
                                        <line x1='2' y1='0' x2='2' y2='9' stroke='black' stroke-width='.01' />
                                        <line x1='3' y1='0' x2='3' y2='9' stroke='black' stroke-width='.05' />
                                        <line x1='4' y1='0' x2='4' y2='9' stroke='black' stroke-width='.01' />
                                        <line x1='5' y1='0' x2='5' y2='9' stroke='black' stroke-width='.01' />
                                        <line x1='6' y1='0' x2='6' y2='9' stroke='black' stroke-width='.05' />
                                        <line x1='7' y1='0' x2='7' y2='9' stroke='black' stroke-width='.01' />
                                        <line x1='8' y1='0' x2='8' y2='9' stroke='black' stroke-width='.01' />
                                        <line x1='0' y1='1' x2='9' y2='1' stroke='black' stroke-width='.01' />
                                        <line x1='0' y1='2' x2='9' y2='2' stroke='black' stroke-width='.01' />
                                        <line x1='0' y1='3' x2='9' y2='3' stroke='black' stroke-width='.05' />
                                        <line x1='0' y1='4' x2='9' y2='4' stroke='black' stroke-width='.01' />
                                        <line x1='0' y1='5' x2='9' y2='5' stroke='black' stroke-width='.01' />
                                        <line x1='0' y1='6' x2='9' y2='6' stroke='black' stroke-width='.05' />
                                        <line x1='0' y1='7' x2='9' y2='7' stroke='black' stroke-width='.01' />
                                        <line x1='0' y1='8' x2='9' y2='8' stroke='black' stroke-width='.01' />
                                        <rect x='0' y='0' width='9' height='9' stroke='black' stroke-width='.05' fill='none' />

                                        <g>{puzzle.OverSvg}{puzzle.Constraints?.Where(c => !(c is SvgGlobalConstraint) && c.SvgAboveLines).Select(c => c.Svg).JoinString()}</g>
                                    </g>
                                </g>
                            </svg>")),
                        new DIV { class_ = "sidebar" }._(
                            new DIV { class_ = "sidebar-content" }._(
                                new DIV { class_ = "rules" }._(new DIV { class_ = "rules-text" }._(
                                    puzzle.Rules.NullOr(r => Regex.Split(r, @"\r?\n").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => new P(s)))
                                        ?? (object) "Normal Sudoku rules apply: place the digits 1–9 in every row, every column and every 3×3 box.")),
                                puzzle.Links == null || puzzle.Links.Length == 0 ? null : new UL { class_ = "links" }._(puzzle.Links.Select(link => new LI(new A { href = link.Url }._(link.Text)))),
                                new DIV { class_ = "options" }._(
                                    new DIV(new INPUT { type = itype.checkbox, id = "opt-show-errors" }, new LABEL { for_ = "opt-show-errors" }._(" Show conflicts")),
                                    new DIV(new INPUT { type = itype.checkbox, id = "opt-multi-color" }, new LABEL { for_ = "opt-multi-color" }._(" Multi-color mode")))))))));
        }

        private HttpResponse PlayWithSuco(HttpRequest req)
        {
            List<object> htmlBlocks = null;
            var code = req.Post["code"].Value;
            var environment = new SucoEnvironment()
                // built-ins
                .DeclareVariable("cells", new SucoListType(SucoCellType.Instance))
                .DeclareVariable("between", new SucoFunction(
                    (parameters: new[] { SucoCellType.Instance, SucoCellType.Instance },
                    returnType: new SucoListType(SucoCellType.Instance),
                    generator: (exprs, env) => $@"(function($a, $b) {{ return cells.filter((_, $i) => ($i > $a && $i < $b) || ($i > $b && $i < $a)); }})(cells.indexOf({exprs[0].GetJavaScript(env).Code}), cells.indexOf({exprs[1].GetJavaScript(env).Code}))")))
                .DeclareVariable("outside", new SucoFunction(
                    (parameters: new[] { SucoCellType.Instance, SucoCellType.Instance },
                    returnType: new SucoListType(SucoCellType.Instance),
                    generator: (exprs, env) => $@"(function($a, $b) {{ return cells.filter((_, $i) => ($i < $a || $i > $b) && ($i < $b || $i > $a)); }})(cells.indexOf({exprs[0].GetJavaScript(env).Code}), cells.indexOf({exprs[1].GetJavaScript(env).Code}))")))

                // sandwich constraint
                .DeclareVariable("crust1", SucoIntegerType.Instance)
                .DeclareVariable("crust2", SucoIntegerType.Instance)
                .DeclareVariable("sum", SucoIntegerType.Instance);

            if (code != null)
            {
                htmlBlocks = new List<object>();

                object exceptionBox(Exception exc, (int start, int? end)[] highlights)
                {
                    var pieces = new List<object>();
                    var ix = 0;
                    for (var i = 0; i < highlights.Length; i++)
                    {
                        if (highlights[i].start > ix)
                            pieces.Add(code.Substring(ix, highlights[i].start - ix));
                        if (highlights[i].end == null)
                            pieces.Add(new STRONG { class_ = "single" });
                        else
                            pieces.Add(new STRONG(code.Substring(highlights[i].start, highlights[i].end.Value - highlights[i].start)));
                        ix = highlights[i].end ?? highlights[i].start;
                    }
                    if (code.Length > ix)
                        pieces.Add(code.Substring(ix));
                    return new DIV { class_ = "exception" }._(
                        new DIV { class_ = "message" }._(exc.Message),
                        new DIV { class_ = "type" }._(exc.GetType().Name),
                        new DIV { class_ = "code" }._(new PRE(pieces)));
                }

                object parseExceptionBox(SucoParseException exc) => exceptionBox(exc, (exc.Highlights?.OrderBy(h => h.StartIndex).Select(h => (start: h.StartIndex, end: h.EndIndex)).ToArray() ?? Enumerable.Empty<(int start, int? end)>()).Concat((start: exc.Index, end: null)).ToArray());
                object compileExceptionBox(SucoCompileException exc) => exceptionBox(exc, new[] { (start: exc.StartIndex, end: exc.EndIndex.Nullable()) });

                // Parse tree
                try
                {
                    var parseTree = Parser.ParseConstraint(code).DeduceTypes(environment);

                    object span(SucoNode node) => new SPAN { class_ = "node" }.Data("type", $"{Regex.Replace(node.GetType().Name, @"^Suco|Expression$", "")}{(node is SucoExpression expr ? $" — {expr.Type}" : null)}")._(visit(node));
                    IEnumerable<object> visit(SucoNode expr)
                    {
                        var properties = expr.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var ix = expr.StartIndex;
                        foreach (var inner in properties.Where(p => typeof(SucoNode).IsAssignableFrom(p.PropertyType)).Select(p => (SucoNode) p.GetValue(expr))
                            .Concat(properties.Where(p => typeof(IEnumerable<SucoNode>).IsAssignableFrom(p.PropertyType)).SelectMany(p => (IEnumerable<SucoNode>) p.GetValue(expr)))
                            .Where(expr => expr != null)
                            .OrderBy(expr => expr.StartIndex))
                        {
                            yield return code.Substring(ix, inner.StartIndex - ix);
                            yield return span(inner);
                            ix = inner.EndIndex;
                        }
                        yield return code.Substring(ix, expr.EndIndex - ix);
                    }
                    htmlBlocks.Add(new PRE { class_ = "parse-tree" }._(span(parseTree)));

                    try
                    {
                        htmlBlocks.Add(new PRE { class_ = "javascript" }._(parseTree.GetJavaScript(environment).Code));
                    }
                    catch (SucoCompileException ce) { htmlBlocks.Add(compileExceptionBox(ce)); }
                }
                catch (SucoParseException pe) { htmlBlocks.Add(parseExceptionBox(pe)); }
            }

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE("Suco parse tree"),
                    new STYLELiteral(@"
                        body, * {
                            font-family: 'Roboto';
                        }
                        .parse-tree {
                            font-size: 18pt;
                            font-weight: 400;
                        }
                        .node {
                            display: inline-block;
                            padding: .5cm .1cm .1cm;
                            margin: 0 .1cm;
                            border: 1px solid transparent;
                            position: relative;
                            box-sizing: border-box;
                            background: white;
                        }
                        .node:hover {
                            border-color: black;
                            background: #def;
                        }
                        .node:hover::after {
                            content: attr(data-type);
                            position: absolute;
                            left: 100%;
                            top: -1px;
                            font-size: 9pt;
                            font-weight: 300;
                            background: #bdf;
                            padding: 1px 4px 1px 1px;
                            border: 1px solid black;
                            border-left: none;
                            border-top-right-radius: .1cm;
                            border-bottom-right-radius: .1cm;
                            box-sizing: border-box;
                            z-index: 1;
                        }
                        textarea {
                            width: 100%;
                            height: 15em;
                        }
                        .exception {
                            border: 2px solid black;
                            border-radius: .25cm;
                            overflow: hidden;
                            margin-bottom: 1cm;
                        }
                            .exception .message {
                                background: #fdd;
                                padding: .1cm .25cm;
                                font-weight: bold;
                                text-align: center;
                            }
                            .exception .type {
                                background: #ffd;
                                font-size: 9pt;
                                padding: .05cm .1cm;
                                text-align: center;
                            }
                            .exception .code {
                                padding: .1cm .25cm;
                            }
                        strong {
                            background: #fdd;
                            padding: 0 .05cm;
                        }
                        pre.javascript {
                            white-space: pre-wrap;
                        }
                    ")),
                new BODY(
                    htmlBlocks,
                    new DIV(new FORM { method = method.post, action = "/tmp" }._(
                        new DIV(new TEXTAREA { accesskey = ",", name = "code" }._(code)),
                        new DIV(new BUTTON { type = btype.submit, accesskey = "p" }._("Parse")))))));
        }
    }
}