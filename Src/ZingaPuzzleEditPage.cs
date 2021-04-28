using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using RT.Json;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Database;

using DbConstraint = Zinga.Database.Constraint;

namespace Zinga
{
    public partial class ZingaPropellerModule
    {
        private static DbConstraint[] _constraintsWithShortcuts;

        public HttpResponse PuzzleEditPage(HttpRequest req)
        {
            var constraintsWithShortcuts = _constraintsWithShortcuts;
            if (constraintsWithShortcuts == null)
                lock (this)
                {
                    constraintsWithShortcuts = _constraintsWithShortcuts;
                    if (constraintsWithShortcuts == null)
                        using (var db = new Db())
                            constraintsWithShortcuts = _constraintsWithShortcuts = db.Constraints.Where(c => c.Shortcut != null).OrderBy(c => c.Shortcut).ToArray();
                }

            Puzzle puzzle;
            Dictionary<int, DbConstraint> constraintTypes;
            PuzzleConstraint[] constraints;

            var url = req.Url.Path.SubstringSafe(1);
            if (url.Length == 0)
            {
                puzzle = new Puzzle { Title = "Sudoku", Author = "unknown", Rules = "" };
                constraintTypes = new Dictionary<int, DbConstraint>();
                constraints = new PuzzleConstraint[0];
            }
            else
            {
                using var db = new Db();
                url = url.UrlUnescape();
                puzzle = db.Puzzles.FirstOrDefault(p => p.UrlName == url);
                if (puzzle == null)
                    return HttpResponse.Html("<h1>404 — Not Found</h1>", HttpStatusCode._404_NotFound);
                constraints = db.PuzzleConstraints.Where(c => c.PuzzleID == puzzle.PuzzleID).ToArray();
                var constraintIds = constraints.Select(c => c.ConstraintID).Distinct().ToArray();
                constraintTypes = db.Constraints.Where(c => constraintIds.Contains(c.ConstraintID)).AsEnumerable().ToDictionary(c => c.ConstraintID);
            }

            foreach (var cws in _constraintsWithShortcuts)
                constraintTypes[cws.ConstraintID] = cws;

            const double btnHeight = .8;
            const double margin = .135;

            var btns = Ut.NewArray<(string label, bool isSvg, string id, double width, int row, bool color)>(
                ("Delete", false, "clear", 1, 0, false),
                ("Undo", false, "undo", 1, 0, false),
                ("Redo", false, "redo", 1, 0, false));

            string renderButton(string id, double x, double y, double width, string label, bool color, bool isSvg = false) => $@"
                <g class='button' id='{id}' transform='translate({x}, {y})'>
                    <rect class='clickable' x='0' y='0' width='{width}' height='{btnHeight}' stroke-width='.025' rx='.08' ry='.08'/>
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

            var constraintsJson = constraints.Select(c => new JsonDict
            {
                ["type"] = c.ConstraintID,
                ["values"] = new JsonRaw(c.ValuesJson)
            }).ToJsonList().ToString();
            var constraintTypesJson = constraintTypes.ToJsonDict(kvp => kvp.Key.ToString(), kvp =>
            {
                var dic = new JsonDict
                {
                    ["public"] = kvp.Value.Public,
                    ["name"] = kvp.Value.Name,
                    ["kind"] = kvp.Value.Kind.ToString(),
                    ["logic"] = kvp.Value.LogicSuco?.Replace("\r", ""),
                    ["svgdefs"] = kvp.Value.SvgDefsSuco?.Replace("\r", ""),
                    ["svg"] = kvp.Value.SvgSuco?.Replace("\r", ""),
                    ["preview"] = kvp.Value.PreviewSvg,
                    ["variables"] = new JsonRaw(kvp.Value.VariablesJson)
                };
                if (kvp.Value.Shortcut != null)
                    dic["shortcut"] = kvp.Value.Shortcut;
                return dic;
            }).ToString();

            const bool autoCss = true;
            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE($"Editing: {puzzle.Title} by {puzzle.Author}"),
                    new RawTag(@"<script src='/_framework/blazor.webassembly.js' autostart='false'></script>"),

#if DEBUG
                    new SCRIPTLiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "EditPuzzle.js"))),
                    new STYLELiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Font.css"))),
                    autoCss ? Ut.NewArray<object>(
                        new STYLE { id = "auto-css" },
                        new SCRIPTLiteral(@"
                            (function() {
                                let socket = new WebSocket('ws://localhost:8990/css-websocket');
                                socket.onopen = function()
                                {
                                    socket.send('css');
                                    window.setInterval(function() { socket.send('css'); }, 500);
                                };
                                socket.onclose = function()
                                {
                                    console.log('Socket closed.');
                                };
                                socket.onmessage = function(msg)
                                {
                                    document.getElementById('auto-css').innerText = msg.data.replace(/\r|\n/g, ' ');
                                };
                            })();
                        ")
                    ) : new STYLELiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Puzzle.css"))),
#else
                    new SCRIPTLiteral(Resources.EditJs),
                    new STYLELiteral(Resources.Css),
#endif
                    new LINK { rel = "shortcut icon", type = "image/png", href = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABAAAAAQAAQMAAABF07nAAAAABlBMVEUAAAD///+l2Z/dAAACFElEQVR42u3YsQ2AMBAEwZMIKINS3RplERm38ERvodn4gokvkSRJkiRJ2qHxFrqTXJXhk+SsDCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAJEmSJEmStslFAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8EPA8Q0gSZIkSZLUflD4iAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANoBkiRJkiRJnS37yw5ZFqD7+QAAAABJRU5ErkJggg==" }),
                new BODY { class_ = "is-puzzle" }._(
                    new DIV { id = "topbar" }._(
                        new DIV { class_ = "title" }._(puzzle.Title),
                        puzzle.Author == null ? null : new DIV { class_ = "author" }._("by ", puzzle.Author)),
                    new DIV { class_ = "puzzle" }
                        .Data("constrainttypes", constraintTypesJson)
                        .Data("constraints", constraintsJson)
                        .Data("title", puzzle.Title)
                        .Data("author", puzzle.Author)
                        .Data("rules", puzzle.Rules)
                        ._(
                            new DIV { class_ = "puzzle-container", tabindex = 0, accesskey = "," }._(new RawTag($@"
                                <svg viewBox='-0.5 -0.5 10 11.2' text-anchor='middle' font-family='Bitter' class='puzzle-svg'>
                                    <defs>
                                        <filter id='constraint-selection-shadow' x='-1' y='-1' width='500%' height='500%'>
                                            <feMorphology in='SourceGraphic' operator='dilate' radius='.05' result='constraint-selection-shadow-1' />
                                            <feColorMatrix in='constraint-selection-shadow-1' type='matrix' values='0 0 0 0 .24 0 0 0 0 .47 0 0 0 0 .96 0 0 0 5 0' result='constraint-selection-shadow-2' />
                                            <feGaussianBlur in='constraint-selection-shadow-2' stdDeviation='.05' result='constraint-selection-shadow-3' />
                                            <feColorMatrix in='SourceGraphic' type='matrix' values='1 0 0 0 0 0 1 0 0 0 0 0 1 0 0 0 0 0 5 0' result='constraint-selection-shadow-4' />
                                            <feComposite in='constraint-selection-shadow-4' in2='constraint-selection-shadow-3' />
                                        </filter>
                                    </defs>
                                    <defs id='constraint-defs'></defs>
                                    <g class='full-puzzle'>
                                        <g transform='translate(0, 9.5)' class='button-row'>{renderButtonArea(btns, 9)}</g>
                                        <g class='global-constraints'></g>

                                        <g class='sudoku'>
                                            <filter id='glow-blur'><feGaussianBlur stdDeviation='.1' /></filter>
                                            <rect class='frame' id='sudoku-frame' x='0' y='0' width='9' height='9' stroke-width='.2' fill='none' filter='url(#glow-blur)'></rect>

                                            {Enumerable.Range(0, 81).Select(cell => $@"<g class='cell' id='sudoku-{cell}' font-size='.25' stroke-width='0'>
                                                <rect class='clickable sudoku-cell' data-cell='{cell}' x='{cell % 9}' y='{cell / 9}' width='1' height='1' />
                                                <text id='sudoku-text-{cell}' x='{cell % 9 + .5}' y='{cell / 9 + .725}' font-size='.65'></text>
                                            </g>").JoinString()}

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

                                            <g id='constraint-svg'></g>
                                            <g id='over-svg'></g>
                                            <g id='temp-svg'></g>

                                            {Enumerable.Range(0, 9).Select(col => $"<path class='multi-select' data-what='n' data-offset='{col}' d='m {col + .3} 9.3 .2 -.2 .2 .2z' fill='black' />").JoinString()}
                                            {Enumerable.Range(0, 9).Select(row => $"<path class='multi-select' data-what='e' data-offset='{row}' d='m -.3 {row + .3} .2 .2 -.2 .2z' fill='black' />").JoinString()}
                                            {Enumerable.Range(0, 9).Select(row => $"<path class='multi-select' data-what='w' data-offset='{row}' d='m 9.3 {row + .3} -.2 .2 .2 .2z' fill='black' />").JoinString()}
                                            {Enumerable.Range(0, 9).Select(col => $"<path class='multi-select' data-what='s' data-offset='{col}' d='m {col + .3} -.3 .2 .2 .2 -.2z' fill='black' />").JoinString()}

                                            {Enumerable.Range(0, 17).Select(offset => $"<path class='multi-select' data-what='se' data-offset='{offset - 8}' d='m {(offset < 8 ? 0 : offset - 8) - .1} {(offset > 8 ? 0 : 8 - offset) - .1} -.2 0 .2 -.2 z' fill='black' />").JoinString()}
                                            {Enumerable.Range(0, 17).Select(offset => $"<path class='multi-select' data-what='sw' data-offset='{offset}' d='m {(offset < 8 ? offset + 1 : 9) + .1} {(offset > 8 ? offset - 8 : 0) - .1} 0 -.2 .2 .2 z' fill='black' />").JoinString()}
                                            {Enumerable.Range(0, 17).Select(offset => $"<path class='multi-select' data-what='nw' data-offset='{8 - offset}' d='m {(offset < 8 ? 9 : 17 - offset) + .1} {(offset > 8 ? 9 : offset + 1) + .1} .2 0 -.2 .2 z' fill='black' />").JoinString()}
                                            {Enumerable.Range(0, 17).Select(offset => $"<path class='multi-select' data-what='ne' data-offset='{16 - offset}' d='m {(offset < 8 ? 8 - offset : 0) - .1} {(offset > 8 ? 17 - offset : 9) + .1} -.2 0 .2 .2 z' fill='black' />").JoinString()}
                                        </g>
                                    </g>
                                </svg>")),
                            new DIV { class_ = "sidebar", tabindex = 0, accesskey = "." }._(
                                new DIV { class_ = "tabs" }._(
                                    new DIV { class_ = "tab tab-puzzle", accesskey = "p", tabindex = -1 }.Data("tab", "puzzle")._("Puzzle".Accel('P')),
                                    new DIV { class_ = "tab tab-constraints", accesskey = "c", tabindex = -1 }.Data("tab", "constraints")._("Constraints".Accel('C'))),
                                new DIV { class_ = "tabc", id = "tab-puzzle" }._(
                                    new SECTION(
                                        new DIV { class_ = "label" }._("Title"),
                                        new DIV(new INPUT { type = itype.text, id = "puzzle-title-input", value = puzzle.Title })),
                                    new SECTION(
                                        new DIV { class_ = "label" }._("Author(s)"),
                                        new DIV(new INPUT { type = itype.text, id = "puzzle-author-input", value = puzzle.Author })),
                                    new SECTION(
                                        new DIV { class_ = "label" }._("Rules"),
                                        new DIV(new TEXTAREA { id = "puzzle-rules-input", accesskey = "/" }._(puzzle.Rules))),
                                    new SECTION(
                                        new DIV { class_ = "label" }._("Givens"),
                                        new DIV { id = "givens" }._(
                                            Enumerable.Range(1, 9).Select(n => new BUTTON { type = btype.button, id = $"given-{n}", class_ = "btn given-btn" }.Data("given", n)._(new SPAN(n))),
                                            new DIV { class_ = "list" })),
                                    new SECTION(
                                        new DIV { class_ = "label" }._("Save"),
                                        new DIV { class_ = "save-section" }._(
                                            new BUTTON { id = "puzzle-test", accesskey = "t" }._("Test puzzle".Accel('T')),
                                            new BUTTON { id = "puzzle-save", accesskey = "s" }._("Publish puzzle".Accel('s')),
                                            new DIV("Publishing...")))),
                                new DIV { class_ = "tabc", id = "tab-constraints" }._(
                                    new SECTION { id = "constraints-section" }._(
                                        new BUTTON { id = "constraint-select-similar", class_ = "mini-btn", title = "Select constraints of the same type" },
                                        new DIV { class_ = "label" }._("Constraints"),
                                        new DIV { id = "constraint-list" }),
                                    new SECTION { id = "constraint-code-section" }._(
                                        new DIV { class_ = "label" }._("Edit constraint code", new DIV { class_ = "expand" }),
                                        new DIV { class_ = "constraint-code" }._(
                                            new DIV { class_ = "label" }._("Name"),
                                            new DIV { class_ = "code" }._(new INPUT { type = itype.text, id = "constraint-code-name" }),
                                            new DIV { class_ = "label" }._("Kind"),
                                            new DIV { class_ = "code" }._(new SELECT { id = "constraint-code-kind" }._(EnumStrong.GetValues<ConstraintKind>().Select(v => new OPTION { value = v.ToString() }._(v.ToString())))),
                                            new DIV { class_ = "label" }._("Logic (Suco)"),
                                            new DIV { class_ = "code" }._(new TEXTAREA { id = "constraint-code-logic" }, new DIV { id = "reporting-logic" }),
                                            new DIV { class_ = "label" }._("Code to generate SVG (Suco)"),
                                            new DIV { class_ = "code" }._(new TEXTAREA { id = "constraint-code-svg" }, new DIV { id = "reporting-svg" }),
                                            new DIV { class_ = "label" }._("Code to generate SVG definitions (Suco)"),
                                            new DIV { class_ = "code" }._(new TEXTAREA { id = "constraint-code-svgdefs" }, new DIV { id = "reporting-svgdefs" }),
                                            new DIV { class_ = "label" }._("Preview SVG"),
                                            new DIV { class_ = "code" }._(new TEXTAREA { id = "constraint-code-preview" }))),
                                    new SECTION { id = "constraint-add-section" }._(
                                        new DIV { class_ = "label" }._("Add a constraint"),
                                        new DIV { class_ = "main" }._(
                                            new UL(
                                                new LI("Select a set of cells in the grid."),
                                                new LI("Press a lower-case letter to add one of the common constraints listed below."),
                                                new LI("Press Shift with a letter to search for more constraints.")),
                                            new HR(),
                                            new P("Common constraint shortcuts:"),
                                            new TABLE(_constraintsWithShortcuts.Select(c => new TR(new TH(c.Shortcut), new TD(c.Name))))))))))));
        }
    }
}