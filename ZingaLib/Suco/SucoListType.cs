using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;
using Zinga.Lib;

namespace Zinga.Suco
{
    public class SucoListType : SucoType
    {
        public SucoType ElementType { get; private set; }

        public SucoListType(SucoType elementType)
        {
            ElementType = elementType;
        }

        public override bool Equals(SucoType other) => other is SucoListType list && list.ElementType.Equals(ElementType);
        public override string ToString() => $"list({ElementType})";
        public override int GetHashCode() => ElementType.GetHashCode() * 47;
        public override Type CsType => typeof(IEnumerable<>).MakeGenericType(ElementType.CsType);

        public override SucoType GetMemberType(string memberName, SucoContext context) => (memberName, ElementType) switch
        {
            // Lists of cells
            ("contains", SucoCellType) => new SucoFunctionType(
                (new[] { SucoType.Cell }, SucoType.Boolean),
                (new[] { SucoType.Cell.List() }, SucoType.Boolean)),
            ("sum", SucoCellType) => SucoType.Integer,
            ("product", SucoCellType) => SucoType.Integer,
            ("min", SucoCellType) => SucoType.Integer,
            ("max", SucoCellType) => SucoType.Integer,
            ("unique", SucoCellType) => SucoType.Boolean,
            ("outline", SucoCellType) => new SucoFunctionType(
                (new[] { SucoType.Decimal, SucoType.Decimal }, SucoType.String),
                (new[] { SucoType.Decimal, SucoType.Decimal, SucoType.Decimal, SucoType.Decimal }, SucoType.String)),

            // Lists of integers
            ("contains", SucoIntegerType) => new SucoFunctionType(
                (new[] { SucoType.Integer }, SucoType.Boolean),
                (new[] { SucoType.Integer.List() }, SucoType.Boolean)),
            ("same", SucoIntegerType) => SucoType.Boolean,
            ("sum", SucoIntegerType) => SucoType.Integer,
            ("product", SucoIntegerType) => SucoType.Integer,
            ("min", SucoIntegerType) => SucoType.Integer,
            ("max", SucoIntegerType) => SucoType.Integer,

            // Lists of booleans
            ("all", SucoBooleanType) => SucoType.Boolean,
            ("any", SucoBooleanType) => SucoType.Boolean,
            ("none", SucoBooleanType) => SucoType.Boolean,

            // Lists of lists of integers
            ("unique", SucoListType { ElementType: SucoIntegerType }) => SucoType.Boolean,

            // All lists
            ("count", _) => SucoType.Integer,

            _ => base.GetMemberType(memberName, context),
        };

        public override object InterpretMemberAccess(string memberName, object operand, SucoEnvironment env, int?[] grid) => (memberName, ElementType) switch
        {
            // Lists of cells
            ("contains", SucoCellType) => operand == null ? null : contains(((IEnumerable<Cell>) operand).ToArray()),
            ("sum", SucoCellType) => ((IEnumerable<Cell>) operand)?.Aggregate((int?) 0, (prev, next) => prev == null || next == null || grid[next.Index] == null ? null : prev.Value + grid[next.Index].Value),
            ("product", SucoCellType) => ((IEnumerable<Cell>) operand)?.Aggregate((int?) 1, (prev, next) => prev == null || next == null || grid[next.Index] == null ? null : prev.Value * grid[next.Index].Value),
            ("min", SucoCellType) => minMax((IEnumerable<Cell>) operand, grid, min: true),
            ("max", SucoCellType) => minMax((IEnumerable<Cell>) operand, grid, min: false),
            ("unique", SucoCellType) => operand == null ? null : checkUnique(((IEnumerable<Cell>) operand).Select(c => grid[c.Index])),
            ("outline", SucoCellType) => operand == null ? null : outline(((IEnumerable<object>) operand).Cast<Cell>().Select(c => c.Index).ToArray(), env.Width, env.Height),

            // Lists of integers
            ("contains", SucoIntegerType) => operand == null ? null : contains(((IEnumerable<int?>) operand).ToArray()),
            ("same", SucoIntegerType) => operand == null ? null : same(((IEnumerable<int?>) operand)),
            ("count", SucoIntegerType) => count<int?>(operand),
            ("sum", SucoIntegerType) => ((IEnumerable<int?>) operand)?.Aggregate((int?) 0, (prev, next) => prev == null || next == null ? null : prev.Value + next.Value),
            ("product", SucoIntegerType) => ((IEnumerable<int?>) operand)?.Aggregate((int?) 1, (prev, next) => prev == null || next == null ? null : prev.Value * next.Value),
            ("min", SucoIntegerType) => minMax((IEnumerable<int?>) operand, min: true),
            ("max", SucoIntegerType) => minMax((IEnumerable<int?>) operand, min: false),

            // Lists of booleans
            ("all", SucoBooleanType) => all((IEnumerable<bool?>) operand),
            ("any", SucoBooleanType) => any((IEnumerable<bool?>) operand),
            ("none", SucoBooleanType) => none((IEnumerable<bool?>) operand),
            ("count", SucoBooleanType) => count<bool?>(operand),

            // Lists of lists of integers
            ("unique", SucoListType { ElementType: SucoIntegerType }) => checkUnique(((IEnumerable<object>) operand)?.Select(obj => ((IEnumerable<int?>) obj)?.ToArray()).ToArray()),

            // All lists
            ("count", _) => count<object>(operand),

            _ => base.InterpretMemberAccess(memberName, operand, env, grid)
        };

        private bool? all(IEnumerable<bool?> operand)
        {
            var hasNull = false;
            foreach (var v in operand)
                if (v == false)
                    return false;
                else if (v == null)
                    hasNull = true;
            return hasNull ? null : true;
        }

        private bool? any(IEnumerable<bool?> operand)
        {
            foreach (var v in operand)
                if (v == true)
                    return true;
                else if (v == null)
                    return null;
            return false;
        }

        private bool? none(IEnumerable<bool?> operand)
        {
            foreach (var v in operand)
                if (v == true)
                    return false;
                else if (v == null)
                    return null;
            return true;
        }

        private int? minMax(IEnumerable<Cell> cells, int?[] grid, bool min)
        {
            using var e = cells.GetEnumerator();
            if (!e.MoveNext() || grid[e.Current.Index] == null)
                return null;
            var result = grid[e.Current.Index].Value;
            while (e.MoveNext())
            {
                if (e.Current == null || grid[e.Current.Index] == null)
                    return null;
                result = min ? Math.Min(result, grid[e.Current.Index].Value) : Math.Max(result, grid[e.Current.Index].Value);
            }
            return result;
        }

        private int? minMax(IEnumerable<int?> cells, bool min)
        {
            using var e = cells.GetEnumerator();
            if (!e.MoveNext() || e.Current == null)
                return null;
            var result = e.Current.Value;
            while (e.MoveNext())
            {
                if (e.Current == null)
                    return null;
                result = min ? Math.Min(result, e.Current.Value) : Math.Max(result, e.Current.Value);
            }
            return result;
        }

        private int? count<T>(object operand)
        {
            if (operand == null)
                return null;
            var c = 0;
            foreach (var item in (IEnumerable<T>) operand)
            {
                if (item == null)
                    return null;
                c++;
            }
            return c;
        }

        private bool? same(IEnumerable<int?> values)
        {
            var anyNull = false;
            int? digit = null;
            foreach (var value in values)
                if (value == null)
                    anyNull = true;
                else if (digit == null)
                    digit = value.Value;
                else if (digit != value.Value)
                    return false;
            return anyNull ? null : true;
        }

        private bool? checkUnique(IEnumerable<int?> cellValues)
        {
            var anyNull = false;
            var digits = new HashSet<int>();
            foreach (var cellValue in cellValues)
            {
                if (cellValue == null)
                    anyNull = true;
                else if (!digits.Add(cellValue.Value))
                    return false;
            }
            return anyNull ? null : true;
        }

        private SucoFunction contains(IEnumerable<int?> operand) => new(
            (parameters: new[] { SucoType.Integer }, returnType: SucoType.Boolean, interpreter: arg => containsInt(operand, (int?) arg[0])),
            (parameters: new[] { SucoType.Integer.List() }, returnType: SucoType.Boolean, interpreter: arg => containsIntList(operand, ((IEnumerable<int?>) arg[0]).ToArray())));

        private SucoFunction contains(IEnumerable<Cell> operand) => new(
            (parameters: new[] { SucoType.Cell }, returnType: SucoType.Boolean, interpreter: arg => containsCell(operand, (Cell) arg[0])),
            (parameters: new[] { SucoType.Cell.List() }, returnType: SucoType.Boolean, interpreter: arg => containsCellList(operand, ((IEnumerable<Cell>) arg[0]).ToArray())));

        private static bool? containsInt(IEnumerable<int?> listToSearch, int? find)
        {
            if (find == null || listToSearch == null)
                return null;
            var anyNull = false;
            foreach (var value in listToSearch)
            {
                if (value == null)
                    anyNull = true;
                else if (value.Value == find.Value)
                    return true;
            }
            return anyNull ? null : false;
        }

        private static bool? containsCell(IEnumerable<Cell> listToSearch, Cell find)
        {
            if (find == null || listToSearch == null)
                return null;
            var anyNull = false;
            foreach (var value in listToSearch)
            {
                if (value == null)
                    anyNull = true;
                else if (value.Index == find.Index)
                    return true;
            }
            return anyNull ? null : false;
        }

        private static bool? containsIntList(IEnumerable<int?> listToSearch, int?[] find)
        {
            var list = listToSearch.ToArray();
            if (find == null || find.Contains(null))
                return null;
            if (find.All(f => list.Count(v => v.Value == f.Value) >= find.Count(v => v.Value == f.Value)))
                return true;
            return list.Contains(null) ? null : false;
        }

        private static bool? containsCellList(IEnumerable<Cell> listToSearch, Cell[] find)
        {
            var list = listToSearch.ToArray();
            if (find == null || find.Contains(null))
                return null;
            if (find.All(f => list.Count(v => v.Index == f.Index) >= find.Count(v => v.Index == f.Index)))
                return true;
            return list.Contains(null) ? null : false;
        }

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => (op, rightType) switch
        {
            (BinaryOperator.Plus, SucoListType { ElementType: SucoType i }) when i.ImplicitlyConvertibleTo(ElementType) || ElementType.ImplicitlyConvertibleTo(i)
                => (i.ImplicitlyConvertibleTo(ElementType) ? ElementType : i).List(),
            _ => base.GetBinaryOperatorType(op, rightType, context),
        };

        public override object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => (op, rightType) switch
        {
            (BinaryOperator.Plus, SucoListType { ElementType: SucoType i }) when i.ImplicitlyConvertibleTo(ElementType) || ElementType.ImplicitlyConvertibleTo(i) => concat((IEnumerable<object>) left, (IEnumerable<object>) right, i),
            _ => base.InterpretBinaryOperator(left, op, rightType, right),
        };

        private IEnumerable<object> concat(IEnumerable<object> left, IEnumerable<object> right, SucoType rightElementType) =>
            rightElementType.ImplicitlyConvertibleTo(ElementType)
                ? left.Concat(right.Select(obj => rightElementType.InterpretImplicitConversionTo(ElementType, obj))).ToArray()
                : left.Select(obj => ElementType.InterpretImplicitConversionTo(rightElementType, obj)).Concat(right).ToArray();

        private SucoFunction outline(int[] constraintCells, int width, int height) => new(
            (parameters: new[] { SucoType.Decimal, SucoType.Decimal }, returnType: SucoType.String, interpreter: arr => ZingaUtil.GenerateSvgPath(constraintCells, width, height, (double) arr[0], (double) arr[1])),
            (parameters: new[] { SucoType.Decimal, SucoType.Decimal, SucoType.Decimal, SucoType.Decimal }, returnType: SucoType.String, interpreter: arr => ZingaUtil.GenerateSvgPath(constraintCells, width, height, (double) arr[0], (double) arr[1], (double) arr[2], (double) arr[3])));

        private bool? checkUnique(int?[][] lists)
        {
            if (lists == null)
                return null;
            var anyNull = false;
            for (var i = 0; i < lists.Length; i++)
            {
                if (lists[i] == null || lists[i].Contains(null))
                {
                    anyNull = true;
                    continue;
                }
                for (var j = i + 1; j < lists.Length; j++)
                    if (lists[j] != null && lists[i].SequenceEqual(lists[j]))
                        return false;
            }
            return anyNull ? null : true;
        }

        public override bool ImplicitlyConvertibleTo(SucoType other) => (ElementType, other) switch
        {
            (_, SucoStringType) => ElementType.ImplicitlyConvertibleTo(SucoType.String),
            (_, SucoBooleanType) => ElementType.ImplicitlyConvertibleTo(SucoType.Boolean),
            _ => base.ImplicitlyConvertibleTo(other)
        };

        public override object InterpretImplicitConversionTo(SucoType type, object operand) => (ElementType, type) switch
        {
            (_, SucoStringType) => ((IEnumerable) operand)?.Cast<object>().Select(item => (string) ElementType.InterpretImplicitConversionTo(SucoType.String, item)).JoinString(),
            (_, SucoBooleanType) =>
                operand is IEnumerable<bool> bs ? bs.All(b => b) :
                operand is IEnumerable<bool?> nbs ? nbs.Aggregate((bool?) true, (prev, next) => prev == false ? false : (bool?) next == false ? false : prev == null || next == null ? null : true) :
                ((IEnumerable) operand)?.Cast<object>().Select(item => (bool?) ElementType.InterpretImplicitConversionTo(SucoType.Boolean, item))
                    .Aggregate((bool?) true, (prev, next) => prev == false ? false : (bool?) next == false ? false : prev == null || next == null ? null : true),
            _ => base.InterpretImplicitConversionTo(type, operand)
        };
    }
}
