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

        public override bool Interpret(Dictionary<string, object> values, object cur, int curIx, int curCount, object prev, int? prevIx, int? prevCount) => Name switch
        {
            "first" => curIx == 0,
            "last" => curIx == curCount - 1,
            "before" => prevIx == null ? throw new SucoCompileException("“before” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : curIx < prevIx.Value,
            "after" => prevIx == null ? throw new SucoCompileException("“after” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : curIx > prevIx.Value,
            "~" => prevIx == null ? throw new SucoCompileException("“~” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : curIx == prevIx.Value + 1,
            "diagonal" => prevIx == null ? throw new SucoCompileException("“diagonal” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? Math.Abs(c1.X - c2.X) == Math.Abs(c1.Y - c2.Y) : throw new SucoTempCompileException("“diagonal” condition can only be used on cells."),
            "adjacent" => prevIx == null ? throw new SucoCompileException("“adjacent” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? Math.Abs(c1.X - c2.X) <= 1 && Math.Abs(c1.Y - c2.Y) <= 1 : throw new SucoTempCompileException("“adjacent” condition can only be used on cells."),
            "orthogonal" => prevIx == null ? throw new SucoCompileException("“orthogonal” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? (c1.X == c2.X && Math.Abs(c1.Y - c2.Y) == 1) || (c1.Y == c2.Y && Math.Abs(c1.X - c2.X) == 1) : throw new SucoTempCompileException("“orthogonal” condition can only be used on cells."),
            "^^" => prevIx == null ? throw new SucoCompileException("“^^” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.Y == c1.Y - 1 && c2.X == c1.X : throw new SucoTempCompileException("“#” condition can only be used on cells."),
            ">>" => prevIx == null ? throw new SucoCompileException("“>>” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.X == c1.X + 1 && c2.Y == c1.Y : throw new SucoTempCompileException("“>>” condition can only be used on cells."),
            "vv" => prevIx == null ? throw new SucoCompileException("“vv” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.Y == c1.Y + 1 && c2.X == c1.X : throw new SucoTempCompileException("“vv” condition can only be used on cells."),
            "<<" => prevIx == null ? throw new SucoCompileException("“<<” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.X == c1.X - 1 && c2.Y == c1.Y : throw new SucoTempCompileException("“<<” condition can only be used on cells."),
            "above" => prevIx == null ? throw new SucoCompileException("“above” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.Y < c1.Y && c2.X == c1.X : throw new SucoTempCompileException("“above” condition can only be used on cells."),
            "right" => prevIx == null ? throw new SucoCompileException("“right” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.X > c1.X && c2.Y == c1.Y : throw new SucoTempCompileException("“right” condition can only be used on cells."),
            "below" => prevIx == null ? throw new SucoCompileException("“below” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.Y > c1.Y && c2.X == c1.X : throw new SucoTempCompileException("“below” condition can only be used on cells."),
            "left" => prevIx == null ? throw new SucoCompileException("“left” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.X < c1.X && c2.Y == c1.Y : throw new SucoTempCompileException("“left” condition can only be used on cells."),
            "samerow" => prevIx == null ? throw new SucoCompileException("“samerow” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.Y == c1.Y : throw new SucoTempCompileException("“samerow” condition can only be used on cells."),
            "samecol" => prevIx == null ? throw new SucoCompileException("“samecol” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.X == c1.X : throw new SucoTempCompileException("“samecol” condition can only be used on cells."),
            "samebox" => prevIx == null ? throw new SucoCompileException("“samebox” cannot be used on the first variable in a list comprehension.", StartIndex, EndIndex) : prev is Cell c1 && cur is Cell c2 ? c2.Box == c1.Box : throw new SucoTempCompileException("“samebox” condition can only be used on cells."),
            _ => throw new SucoTempCompileException($"Unknown shortcut condition “{Name}”.")
        };
    }
}