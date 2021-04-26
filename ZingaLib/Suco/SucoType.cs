using System;
using System.Collections.Generic;

namespace Zinga.Suco
{
    public abstract class SucoType : IEquatable<SucoType>
    {
        public virtual SucoType GetMemberType(string memberName, SucoContext context) => throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
        public virtual SucoType GetUnaryOperatorType(UnaryOperator op) => throw new SucoTempCompileException($"Unary operator “{op}” is not defined on type “{this}”.");
        public virtual SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand, SucoContext context) => throw new SucoTempCompileException($"Binary operator “{op}” is not defined on type “{this}”.");

        public virtual bool ImplicitlyConvertibleTo(SucoType other) => Equals(other);
        public virtual object InterpretUnaryOperator(UnaryOperator op, object operand) => throw new SucoTempCompileException($"Unary operator “{op}” not defined on type “{this}”.");
        public virtual object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => throw new SucoTempCompileException($"Binary operator “{op}” not defined on types “{this}” and “{rightType}”.");
        public virtual object InterpretImplicitConversionTo(SucoType type, object operand) => Equals(type) ? operand : throw new SucoTempCompileException($"Implicit conversion not defined from type “{this}” to “{type}”.");
        public virtual object InterpretMemberAccess(string memberName, object operand) => throw new SucoTempCompileException($"Member “{memberName}” not defined on type “{this}”.");
        public abstract override int GetHashCode();
        public abstract bool Equals(SucoType other);
        public abstract override string ToString();

        public static SucoType Parse(string source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
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
                    case "cell": return SucoCellType.Instance;
                    case "decimal": return SucoDecimalType.Instance;
                    case "int": return SucoIntegerType.Instance;
                    case "string": return SucoStringType.Instance;

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
