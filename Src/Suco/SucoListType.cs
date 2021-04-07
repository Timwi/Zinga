using System;

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
        public override string ToString() => $"list of {Inner}";

        public override SucoType GetMemberType(string memberName)
        {
            return (memberName, Inner) switch
            {
                // Lists of cells
                ("count", SucoCellType) => SucoIntegerType.Instance,
                ("sum", SucoCellType) => SucoIntegerType.Instance,
                ("unique", SucoCellType) => SucoBooleanType.Instance,
                ("none", SucoCellType) => SucoBooleanType.Instance,

                // Lists of booleans
                ("all", SucoBooleanType) => SucoBooleanType.Instance,
                ("any", SucoBooleanType) => SucoBooleanType.Instance,
                ("none", SucoBooleanType) => SucoBooleanType.Instance,

                // Lists of lists of integers
                ("unique", SucoListType { Inner: SucoIntegerType }) => SucoBooleanType.Instance,

                _ => null,
            };
        }

        public override SucoJsResult GetMemberJs(string memberName, SucoEnvironment env, SucoExpression callee)
        {
            return (memberName, Inner) switch
            {
                // Lists of cells
                ("count", SucoCellType) => $"{callee.GetJavaScript(env)}.length",
                ("sum", SucoCellType) => $"{callee.GetJavaScript(env)}.reduce((p, n) => p === null || grid[n] === null ? null : p + grid[n], 0)",
                ("unique", SucoCellType) => $"(function(vals) {{ return vals.some(v => v === null) ? null : new Set(vals).size === vals.length; }})({callee.GetJavaScript(env)}.map(c => grid[c]))",
                ("none", SucoCellType) => $"({callee.GetJavaScript(env)}.length === 0)",

                // Lists of booleans
                ("all", SucoBooleanType) => $"{callee.GetJavaScript(env)}.reduce((p, n) => p === false || n === false ? false : n === null ? null : p, true)",
                ("any", SucoBooleanType) => $"{callee.GetJavaScript(env)}.reduce((p, n) => p === true || n === true ? true : n === null ? null : p, false)",
                ("none", SucoBooleanType) => $"{callee.GetJavaScript(env)}.reduce((p, n) => n === true ? false : n === null ? null : p, true)",

                // Lists of lists of integers
                ("unique", SucoListType { Inner: SucoIntegerType }) => throw new NotImplementedException(),

                _ => null,
            };
        }

        public override int GetHashCode() => Inner.GetHashCode() * 47;
    }
}
