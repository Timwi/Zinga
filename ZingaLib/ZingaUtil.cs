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
        public static Dictionary<string, object> ConvertVariableValues(JsonDict valuesJson, SucoVariable[] variables)
        {
            var dic = new Dictionary<string, object>();
            foreach (var variable in variables)
            {
                var jsonValue = valuesJson.Safe[variable.Name];
                dic[variable.Name] = convertVariableValue(variable.Type, jsonValue, null);
            }

            return dic;
        }

        private static object convertVariableValue(SucoType type, JsonValue j, int? position) => type switch
        {
            SucoBooleanType => j.GetBoolLenientSafe() ?? false,
            SucoCellType => new Cell(j.GetIntLenientSafe() ?? 0, position),
            SucoDecimalType => j.GetDoubleLenientSafe() ?? 0d,
            SucoIntegerType => j.GetIntLenientSafe() ?? 0,
            SucoStringType => j.GetStringLenientSafe() ?? "",
            SucoListType lst => (j.GetListSafe() ?? new JsonList()).Select((v, ix) => convertVariableValue(lst.Inner, v, ix + 1)).ToArray(),
            _ => throw new NotImplementedException($"Programmer has neglected to include code to deserialize “{type}”.")
        };

        #region Algorithm to generate outlines around cells
        private enum CellDirection { Up, Right, Down, Left }

        public static string GenerateSvgPath(int[] cells, double marginX, double marginY, double? gapX = null, double? gapY = null)
        {
            var outlines = new List<(int x, int y)[]>();
            var visitedUpArrow = Ut.NewArray<bool>(9, 9);

            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    // every region must have at least one up arrow (left edge)
                    if (!visitedUpArrow[i][j] && get(cells, i, j) && !get(cells, i - 1, j))
                        outlines.Add(tracePolygon(cells, i, j, visitedUpArrow));

            var path = new StringBuilder();
            double textX = 0;
            double textY = 0;

            foreach (var outline in outlines)
            {
                path.Append("M");
                var offset = outline.MinIndex(c => c.x + 9 * c.y) + outline.Length - 1;
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
                        path.Append($" {x + marginX + (j == 0 ? gapX ?? 0 : 0) } {y + marginY + (j == outline.Length ? gapY ?? 0 : 0)}");
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

        static CellDirection getDir((int x, int y) from, (int x, int y) to) => from.x == to.x
                        ? (from.y > to.y ? CellDirection.Up : CellDirection.Down)
                        : (from.x > to.x ? CellDirection.Left : CellDirection.Right);

        static bool get(int[] cells, int x, int y) => x >= 0 && x < 9 && y >= 0 && y < 9 && cells.Contains(x + 9 * y);

        static (int x, int y)[] tracePolygon(int[] cells, int i, int j, bool[][] visitedUpArrow)
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

                        if (!get(cells, i, j - 1))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Right;
                        }
                        else if (get(cells, i - 1, j - 1))
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
                        if (!get(cells, i - 1, j))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Left;
                            i--;
                        }
                        else if (get(cells, i, j))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Right;
                        }
                        break;

                    case CellDirection.Left:
                        if (!get(cells, i - 1, j - 1))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Up;
                            j--;
                        }
                        else if (get(cells, i - 1, j))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Down;
                        }
                        else
                            i--;
                        break;

                    case CellDirection.Right:
                        i++;
                        if (!get(cells, i, j))
                        {
                            result.Add((i, j));
                            dir = CellDirection.Down;
                        }
                        else if (get(cells, i, j - 1))
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
