using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoParser : Parser
    {
        public SucoContext Context { get; private set; }

        public SucoParser(string source, SucoContext context) : base(source, Ut.NewArray(
            // Arithmetic
            "+", "-", "−", "*", "×", "^", "%", "/",
            // Relational
            "<", "<=", "≤", ">", ">=", "≥", "=", "!=", "≠",
            // Logical
            "&", "|", "?", ":", "!",
            // Structural
            "{", "}", ",", "(", ")", ".", "[", "]", ";",
            // Flags; "+" is listed in Arithmetic; "1" is parsed as a numeral
            "$",
            // String literals
            "\"",
            // Shortcut filters
            "~", "←", "→", "↑", "↓"    // “v” parses as an identifier; “<”/“>” are in Relational; “^” is in Arithmetic
        ))
        {
            Context = context;
        }

        public static SucoExpression ParseCode(string source, SucoVariable[] variables, SucoContext context, SucoType expectedResultType = null) =>
            ParseCode(source, variables.Aggregate(new SucoTypeEnvironment(), (env, variable) => env.DeclareVariable(variable.Name, variable.Type)), context, expectedResultType);

        public static SucoExpression ParseCode(string source, SucoTypeEnvironment env, SucoContext context, SucoType expectedResultType = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            try
            {
                // Try parsing as a standalone expression (e.g. “cells.unique”).
                var parser = new SucoParser(source, context);
                var ret = parser.parseExpression();
                parser.EnforceEof();
                ret = ret.DeduceTypes(env, context);
                if (expectedResultType != null && !ret.Type.ImplicitlyConvertibleTo(expectedResultType))
                    throw new SucoParseException($"The expression is of type “{ret.Type}”, which is not implicitly convertible to the required type, “{expectedResultType}”.", parser._ix);
                return expectedResultType == null ? ret : ret.ImplicitlyConvertTo(expectedResultType);
            }
            catch (SucoParseException pe1)
            {
                try
                {
                    // Try parsing as a list comprehension (e.g. “a: a.odd”).
                    var parser = new SucoParser(source, context);
                    var ret = parser.parseListComprehension();
                    parser.EnforceEof();
                    ret = ret.DeduceTypes(env, context);
                    if (expectedResultType != null && !ret.Type.ImplicitlyConvertibleTo(expectedResultType))
                        throw new SucoParseException($"The expression is of type “{ret.Type}”, which is not implicitly convertible to the required type, “{expectedResultType}”.", parser._ix);
                    return expectedResultType == null ? ret : ret.ImplicitlyConvertTo(expectedResultType);
                }
                catch (SucoParseException pe2) when (pe2.Index < pe1.Index)
                {
                }
                throw;
            }
        }

        private SucoExpression parseExpression()
        {
            var left = parseExpressionOr();
            if (token("?", out var q))
            {
                var truePart = parseExpression();
                if (!token(":"))
                    throw new SucoParseException("Unterminated conditional operator: “:” expected.", _ix, left, q, truePart);
                var falsePart = parseExpression();
                return new SucoConditionalExpression(left.StartIndex, falsePart.EndIndex, left, truePart, falsePart);
            }
            return left;
        }

        private SucoExpression parseExpressionOr()
        {
            var left = parseExpressionAnd();
            while (token("|"))
            {
                var right = parseExpressionAnd();
                left = new SucoBinaryOperatorExpression(left.StartIndex, right.EndIndex, left, right, BinaryOperator.Or);
            }
            return left;
        }

        private SucoExpression parseExpressionAnd()
        {
            var left = parseExpressionEquality();
            while (token("&"))
            {
                var right = parseExpressionEquality();
                left = new SucoBinaryOperatorExpression(left.StartIndex, right.EndIndex, left, right, BinaryOperator.And);
            }
            return left;
        }

        private SucoExpression parseExpressionEquality()
        {
            var left = parseExpressionRelational();
            while (tokens(out var op, ("=", BinaryOperator.Equal), ("!=", BinaryOperator.NotEqual), ("≠", BinaryOperator.NotEqual)))
            {
                var right = parseExpressionRelational();
                left = new SucoBinaryOperatorExpression(left.StartIndex, right.EndIndex, left, right, op);
            }
            return left;
        }

        private SucoExpression parseExpressionRelational()
        {
            var left = parseExpressionAdditive();
            while (tokens(out var op,
                (">", BinaryOperator.GreaterThan), (">=", BinaryOperator.GreaterThanOrEqual), ("≥", BinaryOperator.GreaterThanOrEqual),
                ("<", BinaryOperator.LessThan), ("<=", BinaryOperator.LessThanOrEqual), ("≤", BinaryOperator.LessThanOrEqual)))
            {
                var right = parseExpressionAdditive();
                left = new SucoBinaryOperatorExpression(left.StartIndex, right.EndIndex, left, right, op);
            }
            return left;
        }

        private SucoExpression parseExpressionAdditive()
        {
            var left = parseExpressionMultiplicative();
            while (tokens(out var op, ("+", BinaryOperator.Plus), ("-", BinaryOperator.Minus), ("−", BinaryOperator.Minus)))
            {
                var right = parseExpressionMultiplicative();
                left = new SucoBinaryOperatorExpression(left.StartIndex, right.EndIndex, left, right, op);
            }
            return left;
        }

        private SucoExpression parseExpressionMultiplicative()
        {
            var left = parseExpressionExponential();
            while (tokens(out var op, ("*", BinaryOperator.Times), ("×", BinaryOperator.Times), ("%", BinaryOperator.Modulo)))
            {
                var right = parseExpressionExponential();
                left = new SucoBinaryOperatorExpression(left.StartIndex, right.EndIndex, left, right, op);
            }
            return left;
        }

        private SucoExpression parseExpressionExponential()
        {
            var left = parseExpressionUnary();
            if (token("^"))
            {
                var right = parseExpressionExponential();
                left = new SucoBinaryOperatorExpression(left.StartIndex, right.EndIndex, left, right, BinaryOperator.Power);
            }
            return left;
        }

        private SucoExpression parseExpressionUnary()
        {
            var startIndex = _ix;
            if (tokens(out var op, ("!", UnaryOperator.Not), ("-", UnaryOperator.Negative), ("−", UnaryOperator.Negative)))
            {
                var operand = parseExpressionUnary();
                return new SucoUnaryOperatorExpression(startIndex, operand.EndIndex, operand, op);
            }
            return parseExpressionMemberAccessAndCall();
        }

        private SucoExpression parseExpressionMemberAccessAndCall()
        {
            var left = parseExpressionPrimary();
            while (true)
            {
                if (token(".", out var oldIx))
                {
                    // Member access
                    var token = getToken();
                    if (token.Type != SucoTokenType.Identifier)
                        throw new SucoParseException("Identifier expected after “.”.", _ix, oldIx);
                    left = new SucoMemberAccessExpression(left.StartIndex, token.EndIndex, left, token.StringValue);
                    _ix = token.EndIndex;
                }
                else if (token("(", out oldIx))
                {
                    // Call with argument list
                    if (token(")", out var closeParenIx))
                        left = new SucoCallExpression(left.StartIndex, _ix, left, new SucoExpression[0]);
                    else
                    {
                        var arguments = parseExpressionList();
                        if (!token(")"))
                            throw new SucoParseException("List of arguments: expecting “)” (to end the list) or “,” (to continue the list).", _ix, new SucoParseExceptionHighlight[] { oldIx }.Concat(arguments.Select(arg => (SucoParseExceptionHighlight) arg)).ToArray());
                        left = new SucoCallExpression(left.StartIndex, _ix, left, arguments.ToArray());
                    }
                }
                else
                    break;
            }
            return left;
        }

        private List<SucoExpression> parseExpressionList()
        {
            var expressions = new List<SucoExpression>();
            do
                expressions.Add(parseExpression());
            while (token(","));
            return expressions;
        }

        private SucoExpression parseExpressionPrimary()
        {
            if (token("{", out var startIx))
                return parseListComprehension(startIx, consumeCloseCurly: true);

            if (token("[", out startIx))
            {
                var exprs = parseExpressionList();
                if (!token("]"))
                    throw new SucoParseException("Unmatched “[”: array must be terminated with “]”.", _ix, startIx);
                return new SucoArrayExpression(startIx, _ix, exprs);
            }

            if (token("(", out startIx))
            {
                var inner = parseExpression();
                if (!token(")"))
                    throw new SucoParseException("Unmatched “(”: missing “)”.", _ix, startIx);
                return inner;
            }

            if (token("\"", out startIx))
            {
                if (Context == SucoContext.Constraint)
                    throw new SucoParseException("String literals cannot be used in Sudoku constraints.", startIx);

                var pieces = new List<SucoStringLiteralPiece>();
                var curStringPiece = new StringBuilder();
                while (true)
                {
                    if (_ix >= _source.Length)
                        throw new SucoParseException("Unterminated string literal.", startIx);
                    if (_source[_ix] == '\\')
                    {
                        if (_ix + 1 >= _source.Length)
                            throw new SucoParseException("Unterminated string literal.", startIx);
                        curStringPiece.Append(_source[_ix + 1]);
                        _ix += 2;
                        continue;
                    }
                    else if (_source[_ix] == '"')
                    {
                        if (curStringPiece.Length > 0)
                            pieces.Add(curStringPiece.ToString());
                        _ix++;
                        return new SucoStringLiteralExpression(startIx, _ix, pieces.ToArray());
                    }
                    else if (_source[_ix] == '{')
                    {
                        if (curStringPiece.Length > 0)
                            pieces.Add(curStringPiece.ToString());
                        curStringPiece.Clear();
                        _ix++;
                        var interpolatedExpression = parseExpression();
                        if (!token("}"))
                            throw new SucoParseException("Unterminated interpolated expression inside string.", _ix);
                        pieces.Add(interpolatedExpression);
                    }
                    else
                    {
                        curStringPiece.Append(_source[_ix]);
                        _ix++;
                    }
                }
            }

            var tok = getToken();
            if (tok.Type == SucoTokenType.Identifier && tok.StringValue == "let")
            {
                _ix = tok.EndIndex;
                var tk = getToken();
                if (tk.Type != SucoTokenType.Identifier)
                    throw new SucoParseException("Expected identifier after “let”.", tk.StartIndex, tok);
                var variableName = tk.StringValue;
                _ix = tk.EndIndex;
                if (!token("="))
                    throw new SucoParseException("Expected “=” after “let” identifier.", _ix, tok);
                var valueExpr = parseExpression();
                if (!token(";"))
                    throw new SucoParseException("Expected “;” after “let” value expression.", _ix, tok);
                var innerExpr = parseExpression();
                return new SucoLetExpression(tok.StartIndex, _ix, variableName, valueExpr, innerExpr);
            }

            if (tok.Type == SucoTokenType.Integer)
            {
                _ix = tok.EndIndex;
                return new SucoIntegerLiteralExpression(tok.StartIndex, tok.EndIndex, tok.NumericalValue);
            }
            else if (tok.Type == SucoTokenType.Decimal)
            {
                _ix = tok.EndIndex;
                return new SucoDecimalLiteralExpression(tok.StartIndex, tok.EndIndex, tok.DecimalValue);
            }
            else if (tok.Type == SucoTokenType.Identifier)
            {
                _ix = tok.EndIndex;
                return new SucoIdentifierExpression(tok.StartIndex, tok.EndIndex, tok.StringValue);
            }

            throw new SucoParseException($"Unexpected code: “{_source.Substring(tok.StartIndex, tok.EndIndex - tok.StartIndex)}”.", tok.StartIndex);
        }

        private static readonly string[] _flags = new[] { "$", "+", "1" };

        private SucoExpression parseListComprehension(int? startIx = null, bool consumeCloseCurly = false)
        {
            var rStartIx = startIx ?? _ix;

            var clauses = new List<SucoListClause>();
            SucoExpression selector = null;

            nextClause:
            var clauseStartIx = _ix;
            var flags = new Dictionary<string, int>();
            nextFlag:
            foreach (var flag in _flags)
                if (token(flag, out var ix))
                {
                    if (flags.ContainsKey(flag))
                        throw new SucoParseException($"Duplicate flag: “{flag}”.", _ix, flags[flag]);
                    flags[flag] = ix;
                    goto nextFlag;
                }
            var variableName = getToken();
            if (variableName.Type != SucoTokenType.Identifier)
                throw new SucoParseException($"Expected variable name{_flags.Except(flags.Keys).ToArray().Select(s => $" or “{s}”").JoinString()}.", variableName.StartIndex);
            _ix = variableName.EndIndex;

            var conditions = new List<SucoListCondition>();
            SucoExpression fromExpression = null;
            void commitClause(int endIndex)
            {
                if (!flags.ContainsKey("1") && clauses.Any(c => c.HasSingleton))
                    throw new SucoParseException("A clause with a “1” flag cannot be followed by a clause without one.", clauseStartIx, endIndex);
                if (flags.ContainsKey("$") && fromExpression != null)
                    throw new SucoParseException("A clause with a “$” flag cannot also have a “from” condition.", clauseStartIx, endIndex);
                clauses.Add(new SucoListClause(clauseStartIx, endIndex, variableName.StringValue, flags.ContainsKey("$"), flags.ContainsKey("+"), flags.ContainsKey("1"), fromExpression, conditions));
            }

            nextCondition:
            if (token(":", out var oldIx))
            {
                commitClause(oldIx);
                selector = parseExpression();
                goto done;
            }
            else if (token(",", out oldIx))
            {
                commitClause(oldIx);
                goto nextClause;
            }
            else if (token("(", out oldIx))
            {
                var innerExpr = parseExpression();
                if (!token(")"))
                    throw new SucoParseException("Unmatched “(”: condition must end in “)”.", _ix, oldIx);
                conditions.Add(new SucoListExpressionCondition(oldIx, _ix, innerExpr));
                goto nextCondition;
            }
            else if (token("~", out oldIx)) { conditions.Add(new SucoListShortcutCondition(oldIx, _ix, "~")); goto nextCondition; }
            else if (token("<", out oldIx) || token("←", out oldIx)) { conditions.Add(new SucoListShortcutCondition(oldIx, _ix, "←")); goto nextCondition; }
            else if (token(">", out oldIx) || token("→", out oldIx)) { conditions.Add(new SucoListShortcutCondition(oldIx, _ix, "→")); goto nextCondition; }
            else if (token("^", out oldIx) || token("↑", out oldIx)) { conditions.Add(new SucoListShortcutCondition(oldIx, _ix, "↑")); goto nextCondition; }
            else if (token("v", out oldIx) || token("↓", out oldIx)) { conditions.Add(new SucoListShortcutCondition(oldIx, _ix, "↓")); goto nextCondition; }

            var tok = getToken();
            if (tok.Type == SucoTokenType.Identifier && tok.StringValue == "from")
            {
                if (fromExpression != null)
                    throw new SucoParseException("Duplicate “from” condition.", _ix, fromExpression);

                _ix = tok.EndIndex;
                var tokVarName = getToken();
                if (tokVarName.Type == SucoTokenType.Identifier)
                {
                    fromExpression = new SucoIdentifierExpression(tokVarName.StartIndex, tokVarName.EndIndex, tokVarName.StringValue);
                    _ix = tokVarName.EndIndex;
                }
                else
                {
                    if (!token("(", out oldIx))
                        throw new SucoParseException("“from” must be followed by either a variable name, or a parenthesized expression.", _ix);
                    fromExpression = parseExpression();
                    if (!token(")"))
                        throw new SucoParseException("Unmatched “(”: “from” expression must end in “)”.", _ix, oldIx);
                }
                goto nextCondition;
            }
            if (tok.Type == SucoTokenType.Identifier)
            {
                conditions.Add(new SucoListShortcutCondition(tok.StartIndex, tok.EndIndex, tok.StringValue));
                _ix = tok.EndIndex;
                goto nextCondition;
            }

            commitClause(_ix);

            done:
            if (selector == null)
            {
                if (clauses.Count != 1)
                    throw new SucoParseException("List comprehensions with more than one clause must have a selector.", _ix, rStartIx);
                selector = new SucoIdentifierExpression(_ix, _ix, clauses[0].VariableName);
            }
            if (consumeCloseCurly)
            {
                if (!token("}"))
                    throw new SucoParseException("Unmatched “{”: list comprehension must be terminated with “}”.", _ix, rStartIx);
            }
            return new SucoListComprehensionExpression(rStartIx, _ix, clauses, selector);
        }
    }
}
