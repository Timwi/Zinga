using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public HttpResponse PuzzlePage(HttpRequest req)
        {
            var url = req.Url.Path.SubstringSafe(1);
            if (url.Length == 0)
                return HttpResponse.Html("<h1>404 — Not Found</h1>", HttpStatusCode._404_NotFound);

            url = url.UrlUnescape();

            var isTest = url == "test";
            Puzzle puzzle = null;
            PuzzleConstraint[] constraints = null;
            Dictionary<int, DbConstraint> constraintTypes;
            if (isTest)
            {
                using var db = new Db();
                constraintTypes = db.Constraints.Where(c => c.Public).AsEnumerable().ToDictionary(c => c.ConstraintID);
            }
            else
            {
                using var db = new Db();
                puzzle = db.Puzzles.FirstOrDefault(p => p.UrlName == url);
                if (puzzle == null)
                    return HttpResponse.Html($"<h1>404 — Puzzle “{url}” Not Found</h1>", HttpStatusCode._404_NotFound);
                constraints = db.PuzzleConstraints.Where(c => c.PuzzleID == puzzle.PuzzleID).ToArray();
                var constraintIds = constraints.Select(c => c.ConstraintID).Distinct().ToArray();
                constraintTypes = db.Constraints.Where(c => constraintIds.Contains(c.ConstraintID)).AsEnumerable().ToDictionary(c => c.ConstraintID);
                puzzle.LastAccessed = DateTime.UtcNow;
                db.SaveChanges();
            }

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

            var hues = new[] { 0, 30, 60, 120, 180, 220, 270, 310, 0 };
            var saturations = new[] { 80, 80, 80, 80, 80, 80, 80, 80, 0 };
            var lightnesses = new[] { 80, 80, 80, 80, 80, 80, 80, 80, 70 };
            var colors = Enumerable.Range(0, 9).Select(i => $"hsl({hues[i]}, {saturations[i]}%, {lightnesses[i]}%)").ToArray();
            string renderButton(string id, double x, double y, double width, string label, bool color, bool isSvg = false) => $@"
                <g class='button' id='{id}' transform='translate({x}, {y})'>
                    <rect class='clickable' x='0' y='0' width='{width}' height='{btnHeight}' stroke-width='.025' rx='.08' ry='.08'/>{(color ? $@"
                    <rect class='color' x='{width / 2 - .3}' y='{btnHeight / 2 - .3}' width='.6' height='.6' fill='{colors[int.Parse(label) - 1]}' stroke='black' stroke-width='.01' />" : null)}
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

            var decodedValues = constraints?.Select(c => c.DecodeValues(constraintTypes[c.ConstraintID].VariablesJson, puzzle.Width)).ToArray();

            var constraintsJson = constraints?.Select(c => c.ToJson()).ToJsonList().ToString();
            var constraintTypesJson = constraintTypes.ToJsonDict(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToJson()).ToString();

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE($"{puzzle?.Title} by {puzzle?.Author}"),
                    new RawTag(@"<script src='/_framework/blazor.webassembly.js' autostart='false'></script>"),
                    new LINK { rel = "stylesheet", href = "/font" },

#if DEBUG
                    new SCRIPTLiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Puzzle.js"))),
                    new STYLELiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Puzzle.css"))),
#else
                    new SCRIPTLiteral(Resources.Js),
                    new STYLELiteral(Resources.Css),
#endif
                    new STYLELiteral(Enumerable.Range(0, 9).Select(i => $@"
                        svg.puzzle-svg .cell.c{i} rect, svg.puzzle-svg .cell path.c{i} {{ fill: {colors[i]}; }}
                        svg.puzzle-svg .cell.highlighted.c{i} rect, svg.puzzle-svg .cell.highlighted path.c{i} {{ fill: hsl({hues[i]}, {saturations[i] * 5 / 8}%, {lightnesses[i] / 2}%); }}
                    ").JoinString()),
                    new LINK { rel = "shortcut icon", type = "image/png", href = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABAAAAAQAAQMAAABF07nAAAAABlBMVEUAAAD///+l2Z/dAAACFElEQVR42u3YsQ2AMBAEwZMIKINS3RplERm38ERvodn4gokvkSRJkiRJ2qHxFrqTXJXhk+SsDCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAJEmSJEmStslFAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8EPA8Q0gSZIkSZLUflD4iAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANoBkiRJkiRJnS37yw5ZFqD7+QAAAABJRU5ErkJggg==" }),
                new BODY { class_ = "is-puzzle" }._(
                    new DIV { id = "topbar" }._(
                        new DIV { class_ = "title" }._(puzzle?.Title),
                        new DIV { class_ = "author" }._("by ", puzzle?.Author)),
                    new DIV { id = "puzzle" }
                        .Data("puzzleid", puzzle?.UrlName ?? "test")
                        .Data("givens", puzzle?.GivensJson)
                        .Data("title", puzzle?.Title)
                        .Data("author", puzzle?.Author)
                        .Data("rules", puzzle?.Rules)
                        .Data("links", puzzle?.Links?.Select(l => new JsonDict { ["text"] = l.Text, ["url"] = l.Url }).ToJsonList())
                        .Data("constrainttypes", constraintTypesJson)
                        .Data("constraints", constraintsJson)
                        ._(
                            new DIV { id = "puzzle-container", tabindex = 0, accesskey = "," }._(new RawTag($@"
                                <svg xmlns='http://www.w3.org/2000/svg' viewBox='-0.5 -0.5 10 13.5' text-anchor='middle' font-family='Bitter' class='puzzle-svg' stroke-width='.1'>
                                    <style></style>
                                    <defs>
                                        <filter id='constraint-invalid-shadow' x='-1' y='-1' width='500%' height='500%' filterUnits='userSpaceOnUse'>
                                            <feMorphology in='SourceGraphic' operator='dilate' radius='.05' result='constraint-selection-shadow-1'></feMorphology>
                                            <feColorMatrix in='constraint-selection-shadow-1' type='matrix' result='constraint-selection-shadow-2' values='0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0 0 2 0'></feColorMatrix>
                                            <feGaussianBlur stdDeviation='.05' in='constraint-selection-shadow-2' result='constraint-selection-shadow-3'></feGaussianBlur>
                                            <feComposite in2='constraint-selection-shadow-3' in='SourceGraphic'></feComposite>
                                        </filter>
                                        <filter id='glow-blur'><feGaussianBlur stdDeviation='.1' /></filter>
                                        <mask id='row-invalid-mask'>
                                            <rect fill='white' x='-1' y='-1' width='11' height='3' />
                                            <rect fill='black' x='0' y='0' width='9' height='1' />
                                        </mask>
                                        <mask id='column-invalid-mask'>
                                            <rect fill='white' x='-1' y='-1' width='3' height='11' />
                                            <rect fill='black' x='0' y='0' width='1' height='9' />
                                        </mask>
                                        <mask id='box-invalid-mask'>
                                            <rect fill='white' x='-1' y='-1' width='5' height='5' />
                                            <rect fill='black' x='0' y='0' width='3' height='3' />
                                        </mask>
                                    </defs>
                                    <defs id='constraint-defs'>{constraints?.SelectMany((c, cIx) => constraintTypes[c.ConstraintID].GetSvgDefs(decodedValues[cIx])).Distinct().JoinString()}</defs>
                                    <g id='bb-everything'>
                                        <g id='bb-buttons' transform='translate(0, 9.5)'>{renderButtonArea(btns, 9)}</g>

                                        <g id='bb-puzzle'>
                                            <g id='constraint-svg-global'>{constraints?
                                                .Select((c, cIx) => (constraint: c, cIx))
                                                .Where(tup => constraintTypes[tup.constraint.ConstraintID].Kind == ConstraintKind.Global)
                                                .Select((tup, ix) => $"<g transform='translate(0, {1.5 * ix})' class='constraint-svg' id='constraint-svg-{tup.cIx}'><rect x='0' y='0' width='1' height='1' rx='.1' ry='.1' fill='white' stroke='black' stroke-width='.03' />{constraintTypes[tup.constraint.ConstraintID].GetSvg(decodedValues[tup.cIx])}</g>")
                                                .JoinString()}</g>

                                            {Enumerable.Range(0, 81).Select(cell => $@"<g class='cell' data-cell='{cell}' font-size='.25' stroke-width='0'>
                                                <rect class='clickable sudoku-cell' data-cell='{cell}' x='{cell % 9}' y='{cell / 9}' width='1' height='1' />
                                                <g id='sudoku-multicolor-{cell}' transform='translate({cell % 9 + .5}, {cell / 9 + .5})'></g>
                                            </g>").JoinString()}

                                            <path d='M0 3H9M0 6H9M3 0V9M6 0V9M0 0H9V9H0z' fill='none' stroke='black' stroke-width='.05' />
                                            <path d='M0 1H9M0 2H9M0 4H9M0 5H9M0 7H9M0 8H9M1 0V9M2 0V9M4 0V9M5 0V9M7 0V9M8 0V9' fill='none' stroke='black' stroke-width='.01' />

                                            <rect class='region-invalid' id='row-invalid-0' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 0)' />
                                            <rect class='region-invalid' id='row-invalid-1' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 1)' />
                                            <rect class='region-invalid' id='row-invalid-2' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 2)' />
                                            <rect class='region-invalid' id='row-invalid-3' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 3)' />
                                            <rect class='region-invalid' id='row-invalid-4' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 4)' />
                                            <rect class='region-invalid' id='row-invalid-5' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 5)' />
                                            <rect class='region-invalid' id='row-invalid-6' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 6)' />
                                            <rect class='region-invalid' id='row-invalid-7' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 7)' />
                                            <rect class='region-invalid' id='row-invalid-8' x='0' y='0' width='9' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, 8)' />
                                            <rect class='region-invalid' id='column-invalid-0' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(0, 0)' />
                                            <rect class='region-invalid' id='column-invalid-1' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(1, 0)' />
                                            <rect class='region-invalid' id='column-invalid-2' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(2, 0)' />
                                            <rect class='region-invalid' id='column-invalid-3' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(3, 0)' />
                                            <rect class='region-invalid' id='column-invalid-4' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(4, 0)' />
                                            <rect class='region-invalid' id='column-invalid-5' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(5, 0)' />
                                            <rect class='region-invalid' id='column-invalid-6' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(6, 0)' />
                                            <rect class='region-invalid' id='column-invalid-7' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(7, 0)' />
                                            <rect class='region-invalid' id='column-invalid-8' x='0' y='0' width='1' height='9' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate(8, 0)' />
                                            <rect class='region-invalid' id='box-invalid-0' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(0, 0)' />
                                            <rect class='region-invalid' id='box-invalid-1' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(3, 0)' />
                                            <rect class='region-invalid' id='box-invalid-2' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(6, 0)' />
                                            <rect class='region-invalid' id='box-invalid-3' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(0, 3)' />
                                            <rect class='region-invalid' id='box-invalid-4' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(3, 3)' />
                                            <rect class='region-invalid' id='box-invalid-5' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(6, 3)' />
                                            <rect class='region-invalid' id='box-invalid-6' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(0, 6)' />
                                            <rect class='region-invalid' id='box-invalid-7' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(3, 6)' />
                                            <rect class='region-invalid' id='box-invalid-8' x='0' y='0' width='3' height='3' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#box-invalid-mask)' transform='translate(6, 6)' />

                                            <g id='constraint-svg'>{constraints?.Select((c, cIx) => constraintTypes[c.ConstraintID].Kind == ConstraintKind.Global ? null : $"<g class='constraint-svg' id='constraint-svg-{cIx}'>{constraintTypes[c.ConstraintID].GetSvg(decodedValues[cIx])}</g>").JoinString()}</g>

                                            {Enumerable.Range(0, 81).Select(cell => $@"<g class='cell' data-cell='{cell}' font-size='.25' stroke-width='0'>
                                                <text id='sudoku-text-{cell}' x='{cell % 9 + .5}' y='{cell / 9 + .725}' font-size='.65'></text>
                                                <text class='notation' id='sudoku-center-text-{cell}' font-size='.3'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-0' x='{cell % 9 + .1}' y='{cell / 9 + .3}' text-anchor='start'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-1' x='{cell % 9 + .9}' y='{cell / 9 + .3}' text-anchor='end'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-2' x='{cell % 9 + .1}' y='{cell / 9 + .875}' text-anchor='start'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-3' x='{cell % 9 + .9}' y='{cell / 9 + .875}' text-anchor='end'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-4' x='{cell % 9 + .5}' y='{cell / 9 + .3}'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-5' x='{cell % 9 + .9}' y='{cell / 9 + .6125}' text-anchor='end'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-6' x='{cell % 9 + .5}' y='{cell / 9 + .875}'></text>
                                                <text class='notation' id='sudoku-corner-text-{cell}-7' x='{cell % 9 + .1}' y='{cell / 9 + .6125}' text-anchor='start'></text>
                                            </g>").JoinString()}
                                        </g>
                                    </g>
                                </svg>")),
                            new DIV { id = "sidebar" }._(
                                new DIV { id = "sidebar-content" }._(
                                    new DIV { class_ = "rules" }._(new DIV { id = "rules-text" }._(
                                        puzzle?.Rules.NullOr(r => Regex.Split(r, @"\r?\n").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => new P(s)))
                                            ?? (object) "Normal Sudoku rules apply: place the digits 1–9 in every row, every column and every 3×3 box.")),
                                    isTest ? new UL { class_ = "links" } : (puzzle?.Links == null || puzzle.Links.Length == 0) ? null : new UL { class_ = "links" }._(puzzle.Links.Select(link => new LI(new A { href = link.Url }._(link.Text)))),
                                    new DIV { class_ = "options" }._(
                                        new DIV { class_ = "opt-minor" }._(new BUTTON { type = btype.button, id = "opt-screenshot", accesskey = "s" }._("Screenshot".Accel('S')), new BUTTON { type = btype.button, id = "opt-edit", accesskey = "e" }._("Edit this puzzle".Accel('E'))),
                                        new DIV(new INPUT { type = itype.checkbox, id = "opt-show-errors" }, new LABEL { for_ = "opt-show-errors", accesskey = "c" }._(" Show conflicts".Accel('c'))),
                                        new DIV(new INPUT { type = itype.checkbox, id = "opt-multi-color" }, new LABEL { for_ = "opt-multi-color", accesskey = "m" }._(" Multi-color mode".Accel('M'))))))))));
        }
    }
}