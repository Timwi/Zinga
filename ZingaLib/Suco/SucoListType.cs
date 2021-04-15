using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoListType : SucoType
    {
        public SucoType Inner { get; private set; }

        public SucoListType(SucoType inner)
        {
            Inner = inner;
        }

        public override bool Equals(SucoType other) => other is SucoListType list && list.Inner.Equals(Inner);
        public override string ToString() => $"list({Inner})";
        public override int GetHashCode() => Inner.GetHashCode() * 47;

        public override SucoType GetMemberType(string memberName) => (memberName, Inner) switch
        {
            // Lists of cells
            ("sum", SucoCellType) => SucoIntegerType.Instance,
            ("unique", SucoCellType) => SucoBooleanType.Instance,
            ("outline", SucoCellType) => new SucoFunctionType(
                (new[] { SucoDecimalType.Instance, SucoDecimalType.Instance }, SucoStringType.Instance),
                (new[] { SucoDecimalType.Instance, SucoDecimalType.Instance, SucoDecimalType.Instance, SucoDecimalType.Instance }, SucoStringType.Instance)),

            // Lists of booleans
            ("all", SucoBooleanType) => SucoBooleanType.Instance,
            ("any", SucoBooleanType) => SucoBooleanType.Instance,
            ("none", SucoBooleanType) => SucoBooleanType.Instance,

            // Lists of lists of integers
            ("unique", SucoListType { Inner: SucoIntegerType }) => SucoBooleanType.Instance,

            // All lists
            ("count", _) => SucoIntegerType.Instance,

            _ => base.GetMemberType(memberName),
        };

        public override object InterpretMemberAccess(string memberName, object operand) => (memberName, Inner) switch
        {
            // Lists of cells
            ("sum", SucoCellType) => throw new NotImplementedException(),
            ("unique", SucoCellType) => throw new NotImplementedException(),
            ("outline", SucoCellType) => outline(((IEnumerable<object>) operand).Cast<Cell>().Select(c => c.Index).ToArray()),

            // Lists of booleans
            ("all", SucoBooleanType) => ((IEnumerable<object>) operand).All(b => (bool) b),
            ("any", SucoBooleanType) => ((IEnumerable<object>) operand).Any(b => (bool) b),
            ("none", SucoBooleanType) => !((IEnumerable<object>) operand).Any(b => (bool) b),

            // Lists of lists of integers
            ("unique", SucoListType { Inner: SucoIntegerType }) => checkUnique(((IEnumerable<object>) operand).Select(obj => ((IEnumerable<object>) obj).Select(i => (int) i).ToArray()).ToArray()),

            // All lists
            ("count", _) => ((IEnumerable<object>) operand).Count(),

            _ => base.InterpretMemberAccess(memberName, operand)
        };

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand, SucoContext context) => (op, rightOperand) switch
        {
            (BinaryOperator.Plus, SucoListType { Inner: SucoType i }) when i.ImplicitlyConvertibleTo(Inner) || Inner.ImplicitlyConvertibleTo(i)
                => new SucoListType(i.ImplicitlyConvertibleTo(Inner) ? Inner : i),
            _ => base.GetBinaryOperatorType(op, rightOperand, context),
        };

        public override object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => (op, rightType) switch
        {
            (BinaryOperator.Plus, SucoListType { Inner: SucoType i }) when i.ImplicitlyConvertibleTo(Inner) || Inner.ImplicitlyConvertibleTo(i) => concat((IEnumerable<object>) left, (IEnumerable<object>) right, i),
            _ => base.InterpretBinaryOperator(left, op, rightType, right),
        };

        private IEnumerable<object> concat(IEnumerable<object> left, IEnumerable<object> right, SucoType rightInnerType) =>
            rightInnerType.ImplicitlyConvertibleTo(Inner)
                ? left.Concat(right.Select(obj => rightInnerType.InterpretImplicitConversionTo(Inner, obj))).ToArray()
                : left.Select(obj => Inner.InterpretImplicitConversionTo(rightInnerType, obj)).Concat(right).ToArray();

        private enum CellDirection { Up, Right, Down, Left }

        private SucoFunction outline(int[] constraintCells)
        {
            return new SucoFunction(
                (parameters: new[] { SucoDecimalType.Instance, SucoDecimalType.Instance }, returnType: SucoStringType.Instance, interpreter: arr => generateSvgPath(constraintCells, (double) arr[0], (double) arr[1])),
                (parameters: new[] { SucoDecimalType.Instance, SucoDecimalType.Instance, SucoDecimalType.Instance, SucoDecimalType.Instance }, returnType: SucoStringType.Instance, interpreter: arr => generateSvgPath(constraintCells, (double) arr[0], (double) arr[1], (double) arr[2], (double) arr[3])));

            static string generateSvgPath(int[] cells, double marginX, double marginY, double? gapX = null, double? gapY = null)
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
        }

        private bool checkUnique(int[][] lists)
        {
            for (var i = 0; i < lists.Length; i++)
                for (var j = i + 1; j < lists.Length; j++)
                    if (lists[i].SequenceEqual(lists[j]))
                        return false;
            return true;
        }

        public override bool ImplicitlyConvertibleTo(SucoType other) => (Inner, other) switch
        {
            (SucoStringType, SucoStringType) => true,
            (SucoBooleanType, SucoBooleanType) => true,
            (_, SucoStringType) => Inner.ImplicitlyConvertibleTo(SucoStringType.Instance),
            _ => base.ImplicitlyConvertibleTo(other)
        };

        public override object InterpretImplicitConversionTo(SucoType type, object operand) => (Inner, type) switch
        {
            (SucoStringType, SucoStringType) => ((IEnumerable<object>) operand).JoinString(),
            (SucoBooleanType, SucoBooleanType) => ((IEnumerable<object>) operand).All(b => (bool) b),
            (_, SucoStringType) => ((IEnumerable<object>) operand).Select(item => Inner.InterpretImplicitConversionTo(SucoStringType.Instance, item)).JoinString(),
            _ => base.InterpretImplicitConversionTo(type, operand)
        };
    }
}
