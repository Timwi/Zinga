using System;
using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoListShortcutCondition : SucoListCondition
    {
        public string Name { get; private set; }

        public SucoListShortcutCondition(int startIndex, int endIndex, string name)
            : base(startIndex, endIndex)
        {
            Name = name;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoListShortcutCondition(startIndex, endIndex, Name);

        private static readonly Dictionary<string, Func<string, string, string>> _shortcutsJs = new Dictionary<string, Func<string, string, string>>
        {
            ["diagonal"] = (cur, prev) => $"Math.abs({prev}.x-{cur}.x) === Math.abs({prev}.y-{cur}.y)",
            ["adjacent"] = (cur, prev) => $"(Math.abs({prev}.x-{cur}.x) <= 1 && Math.abs({prev}.y-{cur}.y) <= 1)",
            ["orthogonal"] = (cur, prev) => $"$contains([Math.abs({prev}.x-{cur}.x), Math.abs({prev}.y-{cur}.y)], [0, 1])",
            ["first"] = (cur, prev) => $"({prev}.pos === 1)",
            ["last"] = (cur, prev) => $"({prev}.pos === cells.count)",
            ["~"] = (cur, prev) => $"({cur}.pos === {prev}.pos + 1)",
            ["^^"] = (cur, prev) => $"({cur}.y === {prev}.y - 1 && {cur}.x === {prev}.x)",
            [">>"] = (cur, prev) => $"({cur}.x === {prev}.x + 1 && {cur}.y === {prev}.y)",
            ["vv"] = (cur, prev) => $"({cur}.y === {prev}.y + 1 && {cur}.x === {prev}.x)",
            ["<<"] = (cur, prev) => $"({cur}.x === {prev}.x - 1 && {cur}.y === {prev}.y)",
            ["above"] = (cur, prev) => $"({cur}.y < {prev}.y && {cur}.x === {prev}.x)",
            ["right"] = (cur, prev) => $"({cur}.x > {prev}.x && {cur}.y === {prev}.y)",
            ["below"] = (cur, prev) => $"({cur}.y > {prev}.y && {cur}.x === {prev}.x)",
            ["left"] = (cur, prev) => $"({cur}.x < {prev}.x && {cur}.y === {prev}.y)",
            ["samerow"] = (cur, prev) => $"({cur}.y === {prev}.y)",
            ["samecol"] = (cur, prev) => $"({cur}.x === {prev}.x)",
            ["samebox"] = (cur, prev) => $"({cur}.box === {prev}.box)",
            ["before"] = (cur, prev) => $"({cur}.pos < {prev}.pos)",
            ["after"] = (cur, prev) => $"({cur}.pos > {prev}.pos)"
        };

        public override string GetJavaScript(SucoEnvironment env)
        {
            if (!_shortcutsJs.TryGetValue(Name, out var func))
                throw new SucoCompileException($"Unknown shortcut condition “{Name}”.", StartIndex, EndIndex);

            try
            {
                return func(env.GetCurVariable().Name, env.GetPrevVariable().Name);
            }
            catch (SucoTempCompileException ce)
            {
                throw new SucoCompileException(ce.Message, StartIndex, EndIndex);
            }
        }
    }
}