using System;
using System.Collections.Generic;

namespace Zinga.Suco
{
    public abstract class SucoExpression : SucoNode
    {
        public SucoType Type { get; private set; }

        public SucoExpression(int startIndex, int endIndex, SucoType type = null)
            : base(startIndex, endIndex)
        {
            Type = type;
        }

        public abstract SucoExpression DeduceTypes(SucoEnvironment env);

        public SucoExpression ImplicitlyConvertTo(SucoType type) =>
            Type.Equals(type) ? this :
            Type.ImplicitlyConvertibleTo(type) ? new SucoImplicitConversionExpression(StartIndex, EndIndex, this, type) :
            throw new InvalidOperationException("Unexpected implicit conversion. Call Type.ImplicitlyConvertibleTo first to ensure convertibility.");

        public abstract object Interpret(Dictionary<string, object> values);

    }
}
