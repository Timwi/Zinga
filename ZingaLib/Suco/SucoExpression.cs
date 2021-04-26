using System;

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

        public SucoExpression DeduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var result = deduceTypes(env, context);
            switch (context)
            {
                case SucoContext.Constraint:
                    if (result.Type is SucoDecimalType)
                        throw new SucoTempCompileException("You cannot use decimal numbers in a puzzle constraint.");
                    if (result.Type is SucoStringType)
                        throw new SucoTempCompileException("You cannot use strings in a puzzle constraint.");
                    break;

                case SucoContext.Svg:
                    break;
            }
            return result;
        }

        protected abstract SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context);

        public SucoExpression ImplicitlyConvertTo(SucoType type) =>
            Type.Equals(type) ? this :
            Type.ImplicitlyConvertibleTo(type) ? new SucoImplicitConversionExpression(StartIndex, EndIndex, this, type) :
            throw new InvalidOperationException("Unexpected implicit conversion. Call Type.ImplicitlyConvertibleTo first to ensure convertibility.");

        public abstract object Interpret(SucoEnvironment env);

    }
}
