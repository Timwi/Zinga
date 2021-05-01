using System;
using System.Collections.Generic;
using Zinga.Lib;

namespace Zinga.Suco
{
    public abstract class SucoType : IEquatable<SucoType>
    {
        public static readonly SucoType Boolean = new SucoBooleanType();
        public static readonly SucoType Cell = new SucoCellType();
        public static readonly SucoType Decimal = new SucoDecimalType();
        public static readonly SucoType Integer = new SucoIntegerType();
        public static readonly SucoType String = new SucoStringType();

        public static SucoType List(SucoType inner) => new SucoListType(inner);

        public virtual SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightType, SucoContext context) => throw new SucoTempCompileException($"Binary operator “{op}” not defined on types “{this}” and “{rightType}”.");
        public virtual object InterpretBinaryOperator(object left, BinaryOperator op, SucoType rightType, object right) => throw new SucoTempCompileException($"Binary operator “{op}” not defined on types “{this}” and “{rightType}”.");

        public virtual SucoType GetUnaryOperatorType(UnaryOperator op) => throw new SucoTempCompileException($"Unary operator “{op}” is not defined on type “{this}”.");
        public virtual object InterpretUnaryOperator(UnaryOperator op, object operand) => throw new SucoTempCompileException($"Unary operator “{op}” not defined on type “{this}”.");

        public virtual SucoType GetMemberType(string memberName, SucoContext context) => throw new SucoTempCompileException($"Member “{memberName}” is not defined on type “{this}”.");
        public virtual bool ImplicitlyConvertibleTo(SucoType other) => Equals(other);
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
                    case "bool": return SucoType.Boolean;
                    case "cell": return SucoType.Cell;
                    case "decimal": return SucoType.Decimal;
                    case "int": return SucoType.Integer;
                    case "string": return SucoType.String;

                    case "list":
                        if (!token("(", out int oldIx))
                            throw new SucoParseException("Expected ‘(’, followed by list element type, followed by ‘)’.", _ix, tok);
                        var innerType = ParseType();
                        if (!token(")"))
                            throw new SucoParseException("Expected ‘)’.", _ix, oldIx);
                        return innerType.List();

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

        public static bool TryParse(string source, out SucoType type)
        {
            try
            {
                type = Parse(source);
                return true;
            }
            catch
            {
                type = default;
                return false;
            }
        }
    }
}
