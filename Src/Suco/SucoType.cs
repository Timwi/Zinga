using System;
using System.Collections.Generic;

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

        public static SucoType Parse(string source)
        {
            var parser = new SucoTypeParser(source);
            var ret = parser.ParseType();
            parser.EnforceEof();
            return ret;
        }

        class SucoTypeParser : Parser
        {
            public SucoTypeParser(string source) : base(source, new[] { "(", ")", "[", "]", "," })
            {
            }

            public SucoType ParseType()
            {
                if (token("["))
                    return parseEnumType();

                var tok = getToken();
                if (tok.Type != SucoTokenType.Identifier)
                    throw new SucoParseException("Expected type name or ‘[’ (for an enum type).", tok.StartIndex);
                _ix = tok.EndIndex;

                switch (tok.StringValue)
                {
                    case "bool": return SucoBooleanType.Instance;
                    case "int": return SucoIntegerType.Instance;
                    case "cell": return SucoCellType.Instance;

                    case "list":
                        if (!token("(", out int oldIx))
                            throw new SucoParseException("Expected ‘(’, followed by list element type, followed by ‘)’.", _ix, tok);
                        var innerType = ParseType();
                        if (!token(")"))
                            throw new SucoParseException("Expected ‘)’.", _ix, oldIx);
                        return new SucoListType(innerType);

                    default:
                        throw new SucoParseException($"Unknown type name: “{tok.StringValue}”.", _ix);
                }
            }

            private SucoType parseEnumType()
            {
                var names = new List<string>();
                while (true)
                {
                    var tok = getToken();
                    if (tok.Type != SucoTokenType.Identifier)
                        throw new SucoParseException("Expected enum member name.", tok.StartIndex);
                    _ix = tok.EndIndex;
                    names.Add(tok.StringValue);

                    if (token("]"))
                        return new SucoEnumType(names.ToArray());
                    if (!token(","))
                        throw new SucoParseException("Expected ‘]’ (to finish the enum type) or ‘,’ (to specify further enum names).", _ix);
                }
            }
        }
    }
}
