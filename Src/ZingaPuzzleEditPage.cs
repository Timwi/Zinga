﻿using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Serialization;
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
        public HttpResponse PuzzleEditPage(HttpRequest req)
        {
            Puzzle puzzle;
            Dictionary<int, DbConstraint> constraintTypes;
            PuzzleConstraint[] constraints;

            var url = req.Url.Path.SubstringSafe(1);
            if (url.Length == 0)
            {
                puzzle = new Puzzle { Title = "Sudoku" };
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

            const double btnHeight = .8;
            const double margin = .135;

            var btns = Ut.NewArray<(string label, bool isSvg, string id, double width, int row, bool color)>(9, btn => ((btn + 1).ToString(), false, (btn + 1).ToString(), .8, 0, true))
                .Concat(Ut.NewArray<(string label, bool isSvg, string id, double width, int row, bool color)>(
                    ("Givens", false, "normal", 1.1, 1, false),
                    ("Constraints", false, "center", 1, 1, false),

                    ("Delete", false, "clear", 1, 2, false),
                    ("Undo", false, "undo", 1, 2, false),
                    ("Redo", false, "redo", 1, 2, false),
                    ("More", false, "sidebar", 1, 2, false)));

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

            var constraintTypesJson = ClassifyJson.Serialize(constraintTypes);
            // Avoid transmitting the SVG code as we don’t need that and it can be a bit much
            foreach (var kvp in constraintTypesJson.GetDict())
                foreach (var removable in new[] { "SvgDefsSuco", "SvgSuco", "PreviewSvg" })
                    if (kvp.Value.ContainsKey(removable))
                        kvp.Value.Remove(removable);
            var decodedValues = constraints.Select(c => c.DecodeValues(constraintTypes[c.ConstraintID].Variables)).ToArray();
            var constraintsJson = ClassifyJson.Serialize(constraints);

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new META { httpEquiv = "content-type", content = "text/html; charset=UTF-8" },
                    new META { name = "viewport", content = "width=device-width,initial-scale=1.0" },
                    new TITLE($"Editing: {puzzle.Title} by {puzzle.Author}"),

#if DEBUG
                    new SCRIPTLiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "EditPuzzle.js"))),
                    new STYLELiteral(File.ReadAllText(Path.Combine(Settings.ResourcesDir, "Puzzle.css"))),
#else
                    new SCRIPTLiteral(Resources.EditJs),
                    new STYLELiteral(Resources.Css),
#endif
                    new LINK { rel = "shortcut icon", type = "image/png", href = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABAAAAAQAAQMAAABF07nAAAAABlBMVEUAAAD///+l2Z/dAAACFElEQVR42u3YsQ2AMBAEwZMIKINS3RplERm38ERvodn4gokvkSRJkiRJ2qHxFrqTXJXhk+SsDCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAJEmSJEmStslFAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8EPA8Q0gSZIkSZLUflD4iAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAANoBkiRJkiRJnS37yw5ZFqD7+QAAAABJRU5ErkJggg==" }),
                new BODY { class_ = "is-puzzle" }._(
                    new DIV { id = "topbar" }._(
                        new DIV { class_ = "title" }._(puzzle.Title),
                        puzzle.Author == null ? null : new DIV { class_ = "author" }._("by ", puzzle.Author)),
                    new DIV { class_ = "puzzle" }.Data("constrainttypes", constraintTypesJson).Data("constraints", ClassifyJson.Serialize(constraints)).Data("puzzle", ClassifyJson.Serialize(puzzle))._(
                        new DIV { class_ = "puzzle-container", tabindex = 0 }._(new RawTag($@"
                            <svg viewBox='-0.5 -0.5 10 13.5' text-anchor='middle' font-family='Bitter' class='puzzle-svg'>
                                <defs></defs>
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

                                        <g class='over-svg'></g>
                                    </g>
                                </g>
                            </svg>")),
                        new DIV { class_ = "sidebar" }._(
                            new DIV { class_ = "sidebar-content" }._(
                                new DIV { class_ = "rules" }._(new DIV { class_ = "rules-text" }._(
                                    puzzle.Rules.NullOr(r => Regex.Split(r, @"\r?\n").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => new P(s)))
                                        ?? (object) "Normal Sudoku rules apply: place the digits 1–9 in every row, every column and every 3×3 box.")),
                                puzzle.Links == null || puzzle.Links.Length == 0 ? null : new UL { class_ = "links" }._(puzzle.Links.Select(link => new LI(new A { href = link.Url }._(link.Text))))))))));
        }
    }
}