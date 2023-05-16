using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Json;
using RT.Util;
using RT.Util.ExtensionMethods;
using Zinga.Suco;

namespace Zinga.Lib
{
    public static class ZingaUtil
    {
        public static SucoType List(this SucoType elementType) => SucoType.List(elementType);

        public static Array CreateArray(this SucoType elementType, int length) => Array.CreateInstance(elementType.CsType, length);

        public static readonly int[] Hues = new[] { 0, 30, 60, 120, 180, 220, 270, 310, 0 };
        public static readonly int[] Saturations = new[] { 80, 80, 80, 80, 80, 80, 80, 80, 0 };
        public static readonly int[] Lightnesses = new[] { 80, 80, 80, 80, 80, 80, 80, 80, 80 };
        public static readonly string[] Colors = Enumerable.Range(0, 9).Select(i => $"hsl({Hues[i]}, {Saturations[i]}%, {Lightnesses[i]}%)").ToArray();

        public static (string puzzleLinesPath, string framePathSvg) RenderGridLines(int[][] regions, int width, int height)
        {
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

            var puzzleLinesPath = Enumerable.Range(1, width - 1).Select(x => $"M{x} 0V{height}").JoinString() +
                Enumerable.Range(1, height - 1).Select(x => $"M0 {x}H{width}").JoinString();
            return (puzzleLinesPath, framePathSvg.ToString());
        }
        public static (string defs, string objects) RenderRegionGlow(int width, int height, bool rowsUnique, bool columnsUnique, int[][] regions)
        {
            var regionInfos = regions.Select((region, rgIx) =>
            {
                var outlines = GetRegionOutlines(region, width, height).ToArray();
                var rgX = outlines.Min(ol => ol.Min(p => p.x)) - 1;
                var rgY = outlines.Min(ol => ol.Min(p => p.y)) - 1;
                var rgW = outlines.Max(ol => ol.Max(p => p.x)) - rgX + 1;
                var rgH = outlines.Max(ol => ol.Max(p => p.y)) - rgY + 1;
                var svgPath = GenerateSvgPath(outlines, width, 0, 0);
                return (
                    mask: $@"
                        <mask id='region-invalid-mask-{rgIx}'>
                            <rect fill='white' x='{rgX}' y='{rgY}' width='{rgW}' height='{rgH}' />
                            <path fill='black' d='{svgPath}' />
                        </mask>",
                    highlight: $"<path class='region-invalid' id='region-invalid-{rgIx}' d='{svgPath}' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#region-invalid-mask-{rgIx})' opacity='0' />");
            }).ToArray();

            return (
                defs: regionInfos.Select(tup => tup.mask).JoinString(),
                objects: $@"
                    {(rowsUnique ? Enumerable.Range(0, height).Select(row => $"<rect class='region-invalid' id='row-invalid-{row}' x='0' y='0' width='{width}' height='1' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#row-invalid-mask)' transform='translate(0, {row})' opacity='0' />").JoinString() : "")}
                    {(columnsUnique ? Enumerable.Range(0, width).Select(col => $"<rect class='region-invalid' id='column-invalid-{col}' x='0' y='0' width='1' height='{height}' fill='black' filter='url(#constraint-invalid-shadow)' mask='url(#column-invalid-mask)' transform='translate({col}, 0)' opacity='0' />").JoinString() : "")}
                    {regionInfos.Select(tup => tup.highlight).JoinString()}
                ");
        }

        #region Algorithm to generate outlines around cells
        private enum CellDirection { Up, Right, Down, Left }

        public static string GenerateSvgPath(int[] cells, int width, int height, double marginX, double marginY, double? gapX = null, double? gapY = null) =>
            GenerateSvgPath(GetRegionOutlines(cells, width, height), width, marginX, marginY, gapX, gapY);

        public static string GenerateSvgPath(IEnumerable<(int x, int y)[]> paths, int width, double marginX, double marginY, double? gapX = null, double? gapY = null)
        {
            var path = new StringBuilder();
            double textX = 0;
            double textY = 0;

            foreach (var outline in paths)
            {
                path.Append("M");
                var offset = outline.MinIndex(c => c.x + width * c.y) + outline.Length - 1;
                textX = outline[(offset + 1) % outline.Length].x + .03;
                textY = outline[(offset + 1) % outline.Length].y + .25;
                for (int j = 0; j <= outline.Length; j++)
                {
                    if (j == outline.Length && gapX == null && gapY == null)
                    {
                        path.Append("z");
                        continue;
                    }

                    var point1 = outline[(j + offset) % outline.Length];
                    var point2 = outline[(j + offset + 1) % outline.Length];
                    var point3 = outline[(j + offset + 2) % outline.Length];
                    var x = point2.x;
                    var y = point2.y;

                    var dir1 = getDir(point1, point2);
                    var dir2 = getDir(point2, point3);

                    // “Outer” corners
                    if (dir1 == CellDirection.Up && dir2 == CellDirection.Right) // top left corner
                        path.Append($" {x + marginX + (j == 0 ? gapX ?? 0 : 0)} {y + marginY + (j == outline.Length ? gapY ?? 0 : 0)}");
                    else if (dir1 == CellDirection.Right && dir2 == CellDirection.Down)  // top right corner
                        path.Append($" {x - marginX} {y + marginY}");
                    else if (dir1 == CellDirection.Down && dir2 == CellDirection.Left) // bottom right corner
                        path.Append($" {x - marginX} {y - marginY}");
                    else if (dir1 == CellDirection.Left && dir2 == CellDirection.Up) // bottom left corner
                        path.Append($" {x + marginX} {y - marginY}");

                    // “Inner” corners
                    else if (dir1 == CellDirection.Left && dir2 == CellDirection.Down) // top left corner
                        path.Append($" {x - marginX} {y - marginY}");
                    else if (dir1 == CellDirection.Up && dir2 == CellDirection.Left) // top right corner
                        path.Append($" {x + marginX} {y - marginY}");
                    else if (dir1 == CellDirection.Right && dir2 == CellDirection.Up) // bottom right corner
                        path.Append($" {x + marginX} {y + marginY}");
                    else if (dir1 == CellDirection.Down && dir2 == CellDirection.Right) // bottom left corner
                        path.Append($" {x - marginX} {y + marginY}");
                }
            }

            return path.ToString();
        }

        public static IEnumerable<(int x, int y)[]> GetRegionOutlines(int[] cells, int width, int height)
        {
            var visitedUpArrow = Ut.NewArray<bool>(width, height);
            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                    // every region must have at least one up arrow (left edge)
                    if (!visitedUpArrow[i][j] && get(cells, i, j, width, height) && !get(cells, i - 1, j, width, height))
                        yield return tracePolygon(cells, i, j, width, height, visitedUpArrow);
        }

        static CellDirection getDir((int x, int y) from, (int x, int y) to) => from.x == to.x
                        ? (from.y > to.y ? CellDirection.Up : CellDirection.Down)
                        : (from.x > to.x ? CellDirection.Left : CellDirection.Right);

        static bool get(int[] cells, int x, int y, int width, int height) => x >= 0 && x < width && y >= 0 && y < height && cells.Contains(x + width * y);

        static (int x, int y)[] tracePolygon(int[] cells, int i, int j, int width, int height, bool[][] visitedUpArrow)
        {
            var result = new List<(int x, int y)>();
            var dir = CellDirection.Up;

            while (true)
            {
                // In each iteration of this loop, we move from the current edge to the next one.
                // We have to prioritise right-turns so that the diagonal-adjacent case is handled correctly.
                // Every time we take a 90° turn, we add the corner coordinate to the result list.
                // When we get back to the original edge, the polygon is complete.
                switch (dir)
                {
                    case CellDirection.Up:
                        // If we’re back at the beginning, we’re done with this polygon
                        if (visitedUpArrow[i][j])
                            return result.ToArray();

                        visitedUpArrow[i][j] = true;

                        if (!get(cells, i, j - 1, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Right;
                        }
                        else if (get(cells, i - 1, j - 1, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Left;
                            i--;
                        }
                        else
                            j--;
                        break;

                    case CellDirection.Down:
                        j++;
                        if (!get(cells, i - 1, j, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Left;
                            i--;
                        }
                        else if (get(cells, i, j, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Right;
                        }
                        break;

                    case CellDirection.Left:
                        if (!get(cells, i - 1, j - 1, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Up;
                            j--;
                        }
                        else if (get(cells, i - 1, j, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Down;
                        }
                        else
                            i--;
                        break;

                    case CellDirection.Right:
                        i++;
                        if (!get(cells, i, j, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Down;
                        }
                        else if (get(cells, i, j - 1, width, height))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Up;
                            j--;
                        }
                        break;
                }
            }
        }
        #endregion
    }
}
