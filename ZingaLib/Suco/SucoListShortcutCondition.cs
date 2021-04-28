﻿using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

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

        public override SucoListCondition DeduceTypes(SucoTypeEnvironment env, SucoContext context, SucoType elementType)
        {
            switch (Name)
            {
                case "first":
                case "last":
                case "before":
                case "after":
                case "~":
                    break;

                case "diagonal":
                case "adjacent":
                case "orthogonal":
                case "^^":
                case ">>":
                case "vv":
                case "<<":
                case "above":
                case "right":
                case "below":
                case "left":
                case "samerow":
                case "samecol":
                case "samebox":
                case "topleft":
                case "topright":
                case "bottomleft":
                case "bottomright":
                case "lefttop":
                case "righttop":
                case "leftbottom":
                case "rightbottom":
                    if (!elementType.Equals(SucoCellType.Instance))
                        throw new SucoCompileException($"“{Name}” can only be used on lists of cells.", StartIndex, EndIndex);
                    break;

                default:
                    throw new SucoCompileException($"Unknown shortcut condition: “{Name}”.", StartIndex, EndIndex);
            }
            return this;
        }

        public override bool? Interpret(SucoEnvironment env) => Name switch
        {
            "first" => env.GetLastIndex() == 0,
            "last" => env.GetLastIndex() == env.GetLastList().Count - 1,
            "before" => env.GetLastIndex() < env.GetPrevLastIndex(),
            "after" => env.GetLastIndex() > env.GetPrevLastIndex(),
            "~" => env.GetLastList() != env.GetPrevLastList() ? throw new SucoCompileException("“~” requires that both elements are from the same list.", StartIndex, EndIndex) : env.GetLastIndex() == env.GetPrevLastIndex() + 1,
            "diagonal" => cellOp(env, (c1, c2) => Math.Abs(c1.X - c2.X) == Math.Abs(c1.Y - c2.Y)),
            "adjacent" => cellOp(env, (c1, c2) => Math.Abs(c1.X - c2.X) <= 1 && Math.Abs(c1.Y - c2.Y) <= 1),
            "orthogonal" => cellOp(env, (c1, c2) => (c1.X == c2.X && Math.Abs(c1.Y - c2.Y) == 1) || (c1.Y == c2.Y && Math.Abs(c1.X - c2.X) == 1)),
            "^^" => cellOp(env, (c1, c2) => c2.Y == c1.Y - 1 && c2.X == c1.X),
            ">>" => cellOp(env, (c1, c2) => c2.X == c1.X + 1 && c2.Y == c1.Y),
            "vv" => cellOp(env, (c1, c2) => c2.Y == c1.Y + 1 && c2.X == c1.X),
            "<<" => cellOp(env, (c1, c2) => c2.X == c1.X - 1 && c2.Y == c1.Y),
            "above" => cellOp(env, (c1, c2) => c2.Y < c1.Y && c2.X == c1.X),
            "right" => cellOp(env, (c1, c2) => c2.X > c1.X && c2.Y == c1.Y),
            "below" => cellOp(env, (c1, c2) => c2.Y > c1.Y && c2.X == c1.X),
            "left" => cellOp(env, (c1, c2) => c2.X < c1.X && c2.Y == c1.Y),
            "samerow" => cellOp(env, (c1, c2) => c2.Y == c1.Y),
            "samecol" => cellOp(env, (c1, c2) => c2.X == c1.X),
            "samebox" => cellOp(env, (c1, c2) => c2.Box == c1.Box),
            "topleft" => cellSetOp(env, c => c.GroupBy(c => c.Y).MinElement(g => g.Key).MinElement(c => c.X).Index),
            "topright" => cellSetOp(env, c => c.GroupBy(c => c.Y).MinElement(g => g.Key).MaxElement(c => c.X).Index),
            "bottomleft" => cellSetOp(env, c => c.GroupBy(c => c.Y).MaxElement(g => g.Key).MinElement(c => c.X).Index),
            "bottomright" => cellSetOp(env, c => c.GroupBy(c => c.Y).MaxElement(g => g.Key).MaxElement(c => c.X).Index),
            "lefttop" => cellSetOp(env, c => c.GroupBy(c => c.X).MinElement(g => g.Key).MinElement(c => c.Y).Index),
            "righttop" => cellSetOp(env, c => c.GroupBy(c => c.X).MaxElement(g => g.Key).MinElement(c => c.Y).Index),
            "leftbottom" => cellSetOp(env, c => c.GroupBy(c => c.X).MinElement(g => g.Key).MaxElement(c => c.Y).Index),
            "rightbottom" => cellSetOp(env, c => c.GroupBy(c => c.X).MaxElement(g => g.Key).MaxElement(c => c.Y).Index),
            _ => throw new SucoTempCompileException($"Unknown shortcut condition “{Name}”.")
        };

        private bool cellOp(SucoEnvironment env, Func<Cell, Cell, bool> fnc) => fnc((Cell) env.GetPrevLastValue(), (Cell) env.GetLastValue());
        private bool cellSetOp(SucoEnvironment env, Func<IEnumerable<Cell>, int> fnc) => fnc(env.GetLastList().Cast<Cell>()) == ((Cell) env.GetLastValue()).Index;
    }
}