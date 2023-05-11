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
using Zinga.Lib;
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

            var pzInf = puzzle?.Info ?? PuzzleInfo.Default;
            var w = pzInf.Width;
            var h = pzInf.Height;
            var vs = pzInf.Values;
            var decodedValues = constraints?.Select(c => c.DecodeValues(constraintTypes[c.ConstraintID].VariablesJson, pzInf.Width)).ToArray();
            var constraintsJson = constraints?.Select(c => c.ToJson()).ToJsonList().ToString();
            var constraintTypesJson = constraintTypes.ToJsonDict(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToJson()).ToString();

            var regionInfos = pzInf.Regions.Select((region, rgIx) =>
            {
                var outlines = ZingaUtil.GetRegionOutlines(region, w, h).ToArray();
                var rgX = outlines.Min(ol => ol.Min(p => p.x)) - 1;
                var rgY = outlines.Min(ol => ol.Min(p => p.y)) - 1;
                var rgW = outlines.Max(ol => ol.Max(p => p.x)) - rgX + 1;
                var rgH = outlines.Max(ol => ol.Max(p => p.y)) - rgY + 1;
                var svgPath = ZingaUtil.GenerateSvgPath(outlines, w, 0, 0);
                return (
                    mask: $@"
                        <mask id='region-invalid-mask-{rgIx}'>
                            <rect fill='white' x='{rgX}' y='{rgY}' width='{rgW}' height='{rgH}' />
                            <path fill='black' d='{svgPath}' />
                        </mask>",
                    highlight: $"<path class='region-invalid' id='region-invalid-{rgIx}' d='{svgPath}' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#region-invalid-mask-{rgIx})' />");
            }).ToArray();

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
                        svg.puzzle-svg .cell.c{i} rect, svg.puzzle-svg .cell path.c{i} {{ fill: {ZingaUtil.Colors[i]}; }}
                        svg.puzzle-svg .cell.highlighted.c{i} rect, svg.puzzle-svg .cell.highlighted path.c{i} {{ fill: hsl({ZingaUtil.Hues[i]}, {ZingaUtil.Saturations[i] * 5 / 8}%, {ZingaUtil.Lightnesses[i] / 2}%); }}
                    ").JoinString()),
                    new LINK { rel = "shortcut icon", type = "image/png", href = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABAAAAAQAAQMAAABF07nAAAAABlBMVEUAAAD///+l2Z/dAAACFElEQVR42u3YsQ2AMBAEwZMIKINS3RplERm38ERvodn4gokvkSRJkiRJ2qHxFrqTXJXhk+SsDCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAJEmSJEmStslFAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8EPA8Q0gSZIkSZLUflD4iAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANoBkiRJkiRJnS37yw5ZFqD7+QAAAABJRU5ErkJggg==" }),
                new BODY { class_ = "is-puzzle" }._(
                    new DIV { id = "topbar" }._(
                        new DIV { class_ = "title" }._(puzzle?.Title),
                        new DIV { class_ = "author" }._("by ", puzzle?.Author)),
                    new DIV { id = "puzzle" }
                        .Data("puzzleid", puzzle?.UrlName ?? "test")
                        .Data("givens", puzzle?.GivensJson)
                        .Data("width", puzzle?.Info.Width)
                        .Data("height", puzzle?.Info.Height)
                        .Data("regions", puzzle?.Info.Regions.Select(region => region.ToJsonList()).ToJsonList())
                        .Data("rowsuniq", puzzle?.Info.RowsUnique)
                        .Data("colsuniq", puzzle?.Info.ColumnsUnique)
                        .Data("values", puzzle?.Info.Values.ToJsonList())
                        .Data("title", puzzle?.Title)
                        .Data("author", puzzle?.Author)
                        .Data("rules", puzzle?.Rules)
                        .Data("links", puzzle?.Links?.Select(l => new JsonDict { ["text"] = l.Text, ["url"] = l.Url }).ToJsonList())
                        .Data("constrainttypes", constraintTypesJson)
                        .Data("constraints", constraintsJson)
                        ._(
                            new DIV { id = "puzzle-container", tabindex = 0, accesskey = "," }._(new RawTag($@"
                                <svg xmlns='http://www.w3.org/2000/svg' viewBox='-0.5 -0.5 10 13.5' text-anchor='middle' font-family='Bitter' class='puzzle-svg' stroke-width='.1'>
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
                                        {regionInfos.Select(tup => tup.mask).JoinString()}
                                    </defs>
                                    <defs id='constraint-defs'>{constraints?.SelectMany((c, cIx) => constraintTypes[c.ConstraintID].GetSvgDefs(decodedValues[cIx])).Distinct().JoinString()}</defs>
                                    <g id='bb-everything'>
                                        <g id='bb-buttons' font-size='.55' text-anchor='middle' transform='translate(0, {h + .5})'>{Commands.RenderButtonRows(9, vs)}</g>

                                        <g id='bb-puzzle'>
                                            <g id='constraint-svg-global'>{constraints?
                                                .Select((c, cIx) => (constraint: c, cIx))
                                                .Where(tup => constraintTypes[tup.constraint.ConstraintID].Kind == ConstraintKind.Global)
                                                .Select((tup, ix) => $"<g transform='translate(0, {1.5 * ix})' class='constraint-svg' id='constraint-svg-{tup.cIx}'><rect x='0' y='0' width='1' height='1' rx='.1' ry='.1' fill='white' stroke='black' stroke-width='.03' />{constraintTypes[tup.constraint.ConstraintID].GetSvg(decodedValues[tup.cIx])}</g>")
                                                .JoinString()}</g>

                                            <g id='puzzle-cells'>{Enumerable.Range(0, w * h).Select(cell => $@"<g class='cell' data-cell='{cell}' font-size='.25' stroke-width='0'>
                                                <rect class='clickable sudoku-cell' data-cell='{cell}' x='{cell % w}' y='{cell / w}' width='1' height='1' />
                                                <g id='sudoku-multicolor-{cell}' transform='translate({cell % w + .5}, {cell / w + .5})'></g>
                                            </g>").JoinString()}</g>

                                            <path id='puzzle-frame' d='M0 3H9M0 6H9M3 0V9M6 0V9M0 0H9V9H0z' fill='none' stroke='black' stroke-width='.05' />
                                            <path id='puzzle-lines' d='M0 1H9M0 2H9M0 4H9M0 5H9M0 7H9M0 8H9M1 0V9M2 0V9M4 0V9M5 0V9M7 0V9M8 0V9' fill='none' stroke='black' stroke-width='.01' />

                                            {(pzInf.RowsUnique ? Enumerable.Range(0, h).Select(row => $"<rect class='region-invalid' id='row-invalid-{row}' x='0' y='0' width='{w}' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, {row})' />").JoinString() : "")}
                                            {(pzInf.ColumnsUnique ? Enumerable.Range(0, w).Select(col => $"<rect class='region-invalid' id='column-invalid-{col}' x='0' y='0' width='1' height='{h}' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate({col}, 0)' />").JoinString() : "")}
                                            {regionInfos.Select(tup => tup.highlight).JoinString()}

                                            <g id='constraint-svg'>{constraints?.Select((c, cIx) => constraintTypes[c.ConstraintID].Kind == ConstraintKind.Global ? null : $"<g class='constraint-svg' id='constraint-svg-{cIx}'>{constraintTypes[c.ConstraintID].GetSvg(decodedValues[cIx])}</g>").JoinString()}</g>

                                            <g id='cell-text'>{Enumerable.Range(0, w * h).Select(cell => $@"<g class='cell' data-cell='{cell}' font-size='.25' stroke-width='0'>
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
                                            </g>").JoinString()}</g>
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