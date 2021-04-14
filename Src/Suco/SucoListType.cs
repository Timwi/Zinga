using System;
using System.Collections.Generic;
using System.Linq;
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
            ("none", SucoCellType) => SucoBooleanType.Instance,

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
            ("none", SucoCellType) => throw new NotImplementedException(),

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
