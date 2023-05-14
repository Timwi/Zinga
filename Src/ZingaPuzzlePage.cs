using System;
using System.Collections.Generic;
using System.Data;
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
            PuzzleInfo puzzleInfo = PuzzleInfo.Default;
            ConstraintInfo[] constraints = null;
            Dictionary<int, ConstraintTypeInfo> constraintTypes;
            if (isTest)
            {
                using var db = new Db();
                constraintTypes = db.Constraints.Where(c => c.Public).AsEnumerable().ToDictionary(c => c.ConstraintID, c => c.ToInfo());
            }
            else
            {
                using var db = new Db();
                puzzle = db.Puzzles.FirstOrDefault(p => p.UrlName == url);
                if (puzzle == null)
                    return HttpResponse.Html($"<h1>404 — Puzzle “{url}” Not Found</h1>", HttpStatusCode._404_NotFound);
                puzzleInfo = puzzle.Info;
                constraints = db.PuzzleConstraints.Where(pc => pc.PuzzleID == puzzle.PuzzleID).AsEnumerable().Select(pc => pc.ToInfo()).ToArray();
                var constraintIds = constraints.Select(c => c.ID).Distinct().ToArray();
                constraintTypes = db.Constraints.Where(c => constraintIds.Contains(c.ConstraintID)).ToDictionary(c => c.ConstraintID, c => c.ToInfo());
                puzzle.LastAccessed = DateTime.UtcNow;
                db.SaveChanges();
            }

            var w = puzzleInfo.Width;
            var h = puzzleInfo.Height;
            var vs = puzzleInfo.Values;
            var constraintsJson = constraints?.Select(c => c.ToJson()).ToJsonList().ToString();
            var constraintTypesJson = constraintTypes.ToJsonDict(kvp => kvp.Key.ToString(), kvp => kvp.Value.ToJson()).ToString();

            var (regionDefs, regionObjects) = Commands.RenderRegionGlowC(w, h, puzzleInfo.RowsUnique, puzzleInfo.ColumnsUnique, puzzleInfo.Regions);

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
                        svg#puzzle-svg .cell.c{i} rect, svg#puzzle-svg .cell path.c{i} {{ fill: {ZingaUtil.Colors[i]}; }}
                        svg#puzzle-svg .cell.highlighted.c{i} rect, svg#puzzle-svg .cell.highlighted path.c{i} {{ fill: hsl({ZingaUtil.Hues[i]}, {ZingaUtil.Saturations[i] * 5 / 8}%, {ZingaUtil.Lightnesses[i] / 2}%); }}
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
                            new DIV { id = "puzzle-container", tabindex = 0, accesskey = "," }._(new RawTag(Commands.RenderPuzzleSvgC(puzzleInfo, constraintTypes, constraints, fullSvgTag: true))),
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