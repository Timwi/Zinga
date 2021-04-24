using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RT.Util;
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

        private SucoFunction outline(int[] constraintCells) => new(
            (parameters: new[] { SucoDecimalType.Instance, SucoDecimalType.Instance }, returnType: SucoStringType.Instance, interpreter: arr => ZingaUtil.GenerateSvgPath(constraintCells, (double) arr[0], (double) arr[1])),
            (parameters: new[] { SucoDecimalType.Instance, SucoDecimalType.Instance, SucoDecimalType.Instance, SucoDecimalType.Instance }, returnType: SucoStringType.Instance, interpreter: arr => ZingaUtil.GenerateSvgPath(constraintCells, (double) arr[0], (double) arr[1], (double) arr[2], (double) arr[3])));

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
