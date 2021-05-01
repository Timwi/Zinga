using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;
using Zinga.Lib;

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

        public override SucoType GetMemberType(string memberName, SucoContext context) => (memberName, Inner) switch
        {
            // Lists of cells
            ("sum", SucoCellType) => SucoType.Integer,
            ("unique", SucoCellType) => SucoType.Boolean,
            ("outline", SucoCellType) => new SucoFunctionType(
                (new[] { SucoType.Decimal, SucoType.Decimal }, SucoType.String),
                (new[] { SucoType.Decimal, SucoType.Decimal, SucoType.Decimal, SucoType.Decimal }, SucoType.String)),

            // Lists of integers
            ("contains", SucoIntegerType) => new SucoFunctionType(
                (new[] { SucoType.Integer }, SucoType.Boolean),
                (new[] { SucoType.Integer.List() }, SucoType.Boolean)),
            ("same", SucoIntegerType) => SucoType.Boolean,

            // Lists of booleans
            ("all", SucoBooleanType) => SucoType.Boolean,
            ("any", SucoBooleanType) => SucoType.Boolean,
            ("none", SucoBooleanType) => SucoType.Boolean,

            // Lists of lists of integers
            ("unique", SucoListType { Inner: SucoIntegerType }) => SucoType.Boolean,

            // All lists
            ("count", _) => SucoType.Integer,

            _ => base.GetMemberType(memberName, context),
        };

        public override object InterpretMemberAccess(string memberName, object operand) => (memberName, Inner) switch
        {
            // Lists of cells
            ("sum", SucoCellType) => ((IEnumerable<object>) operand)?.Cast<Cell>().Aggregate((int?) 0, (prev, next) => prev == null ? null : next.Value == null ? null : prev.Value + next.Value.Value),
            ("unique", SucoCellType) => operand == null ? null : checkUnique(((IEnumerable<object>) operand).Cast<Cell>()),
            ("outline", SucoCellType) => operand == null ? null : outline(((IEnumerable<object>) operand).Cast<Cell>().Select(c => c.Index).ToArray()),

            // Lists of integers
            ("contains", SucoIntegerType) => operand == null ? null : contains(((IEnumerable<object>) operand).Select(c => (int?) c).ToArray()),
            ("same", SucoIntegerType) => operand == null ? null : same(((IEnumerable<object>) operand).Select(c => (int?) c)),

            // Lists of booleans
            ("all", SucoBooleanType) => ((IEnumerable<object>) operand)?.Aggregate((bool?) true, (prev, next) => prev == false || (bool?) next == false ? false : prev == null || next == null ? null : true),
            ("any", SucoBooleanType) => ((IEnumerable<object>) operand)?.Aggregate((bool?) false, (prev, next) => prev == true || (bool?) next == true ? true : prev == null || next == null ? null : false),
            ("none", SucoBooleanType) => ((IEnumerable<object>) operand)?.Aggregate((bool?) true, (prev, next) => prev == false || (bool?) next == true ? false : prev == null || next == null ? null : true),

            // Lists of lists of integers
            ("unique", SucoListType { Inner: SucoIntegerType }) =>
                operand == null || ((IEnumerable<object>) operand).Any(l => l == null || ((IEnumerable<object>) l).Contains(null)) ? null :
                checkUnique(((IEnumerable<object>) operand)?.Select(obj => ((IEnumerable<object>) obj)?.Select(i => (int?) i).ToArray()).ToArray()),

            // All lists
            ("count", _) => count((IEnumerable<object>) operand),

            _ => base.InterpretMemberAccess(memberName, operand)
        };

        private int? count(IEnumerable<object> operand)
        {
            if (operand == null)
                return null;
            var c = 0;
            foreach (var item in operand)
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

        private bool? checkUnique(IEnumerable<Cell> cells)
        {
            var anyNull = false;
            var digits = new HashSet<int>();
            foreach (var cell in cells)
            {
                if (cell.Value == null)
                    anyNull = true;
                else if (!digits.Add(cell.Value.Value))
                    return false;
            }
            return anyNull ? null : true;
        }

        private SucoFunction contains(int?[] operand) => new(
            (parameters: new[] { SucoType.Integer }, returnType: SucoType.Boolean, interpreter: arg => containsInt(operand, (int?) arg[0])),
            (parameters: new[] { SucoType.Integer.List() }, returnType: SucoType.Boolean, interpreter: arg => containsList(operand, ((IEnumerable<object>) arg[0]).Select(v => (int?) v).ToArray())));

        private static bool? containsInt(int?[] listToSearch, int? find)
        {
            if (find == null || listToSearch == null)
                return null;
            if (listToSearch.Any(val => val != null && val.Value == find.Value))
                return true;
            return listToSearch.Contains(null) ? null : false;
        }

        private static bool? containsList(int?[] listToSearch, int?[] find)
        {
            if (find == null || find.Contains(null))
                return null;
            if (find.All(f => listToSearch.Count(v => v == f.Value) == find.Count(v => v == f.Value)))
                return true;
            return listToSearch.Contains(null) ? null : false;
        }

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => (op, rightType) switch
        {
            (BinaryOperator.Plus, SucoListType { Inner: SucoType i }) when i.ImplicitlyConvertibleTo(Inner) || Inner.ImplicitlyConvertibleTo(i)
                => (i.ImplicitlyConvertibleTo(Inner) ? Inner : i).List(),
            _ => base.GetBinaryOperatorType(op, rightType, context),
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

        private SucoFunction outline(int[] constraintCells) => new(
            (parameters: new[] { SucoType.Decimal, SucoType.Decimal }, returnType: SucoType.String, interpreter: arr => ZingaUtil.GenerateSvgPath(constraintCells, (double) arr[0], (double) arr[1])),
            (parameters: new[] { SucoType.Decimal, SucoType.Decimal, SucoType.Decimal, SucoType.Decimal }, returnType: SucoType.String, interpreter: arr => ZingaUtil.GenerateSvgPath(constraintCells, (double) arr[0], (double) arr[1], (double) arr[2], (double) arr[3])));

        private bool? checkUnique(int?[][] lists)
        {
            if (lists == null)
                return null;
            var anyNull = false;
            for (var i = 0; i < lists.Length; i++)
            {
                if (lists[i] == null)
                {
                    anyNull = true;
                    continue;
                }
                if (lists[i].Contains(null))
                    anyNull = true;
                else
                    for (var j = i + 1; j < lists.Length; j++)
                        if (lists[j] != null && lists[i].SequenceEqual(lists[j]))
                            return false;
            }
            return anyNull ? null : true;
        }

        public override bool ImplicitlyConvertibleTo(SucoType other) => (Inner, other) switch
        {
            (_, SucoStringType) => Inner.ImplicitlyConvertibleTo(SucoType.String),
            (_, SucoBooleanType) => Inner.ImplicitlyConvertibleTo(SucoType.Boolean),
            _ => base.ImplicitlyConvertibleTo(other)
        };

        public override object InterpretImplicitConversionTo(SucoType type, object operand) => (Inner, type) switch
        {
            (_, SucoStringType) => ((IEnumerable<object>) operand).Select(item => (string) Inner.InterpretImplicitConversionTo(SucoType.String, item)).JoinString(),
            (_, SucoBooleanType) => ((IEnumerable<object>) operand)
                ?.Select(item => (bool?) Inner.InterpretImplicitConversionTo(SucoType.Boolean, item))
                .Aggregate((bool?) true, (prev, next) => prev == false ? false : (bool?) next == false ? false : prev == null || next == null ? null : true),
            _ => base.InterpretImplicitConversionTo(type, operand)
        };
    }
}
