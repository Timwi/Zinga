using System;

namespace Zinga.Suco
{
    public abstract class SucoType : IEquatable<SucoType>
    {
        public virtual SucoType GetMemberType(string memberName) => throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
        public virtual SucoJsResult GetMemberJs(string memberName, SucoEnvironment env, SucoExpression callee) => throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
        public virtual SucoType GetUnaryOperatorType(UnaryOperator op) => throw new SucoTempCompileException($"Unary operator “{op}” is not defined on type “{this}”.");
        public virtual SucoJsResult GetUnaryOperatorJs(UnaryOperator op, SucoEnvironment env, SucoExpression operand) => throw new SucoTempCompileException($"Unary operator “{op}” is not defined on type “{this}”.");
        public virtual SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => throw new SucoTempCompileException($"Binary operator “{op}” is not defined on type “{this}”.");
        public virtual SucoJsResult GetBinaryOperatorJs(BinaryOperator op, SucoEnvironment env, SucoExpression leftOperand, SucoExpression rightOperand) => throw new SucoTempCompileException($"Binary operator “{op}” is not defined on type “{this}”.");
        public virtual bool ImplicitlyConvertibleTo(SucoType other) => Equals(other);
        public virtual SucoJsResult GetImplicitConversionTo(SucoType other, SucoEnvironment env, SucoExpression convertee) =>
            Equals(other) ? convertee.GetJavaScript(env) : throw new SucoTempCompileException($"Type “{this}” is not implicitly convertible to “{other}”.");

        public abstract override string ToString();
        public abstract override int GetHashCode();
        public abstract bool Equals(SucoType other);
    }
}
