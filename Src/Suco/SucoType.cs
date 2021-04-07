using System;

namespace Zinga.Suco
{
    public abstract class SucoType : IEquatable<SucoType>
    {
        public virtual SucoType GetMemberType(string memberName) => null;
        public virtual SucoJsResult GetMemberJs(string memberName, SucoEnvironment env, SucoExpression callee) => null;
        public virtual SucoType GetUnaryOperatorType(UnaryOperator op) => null;
        public virtual SucoJsResult GetUnaryOperatorJs(UnaryOperator op, SucoEnvironment env, SucoExpression operand) => null;
        public virtual SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => null;
        public virtual SucoJsResult GetBinaryOperatorJs(BinaryOperator op, SucoEnvironment env, SucoExpression leftOperand, SucoExpression rightOperand) => null;
        public virtual bool ImplicitlyConvertibleTo(SucoType other) => Equals(other);
        public virtual SucoJsResult GetImplicitConversionTo(SucoType other, SucoEnvironment env, SucoExpression convertee) => convertee.GetJavaScript(env);

        public abstract override string ToString();
        public abstract override int GetHashCode();
        public abstract bool Equals(SucoType other);
    }
}
