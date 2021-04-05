using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class Parser
    {
        private int _ix;
        private string _source;

        public static SucoExpression ParseConstraint(string source)
        {
            try
            {
                // Try parsing as a standalone expression (e.g. “cells.unique”).
                var parser = new Parser { _ix = 0, _source = source };
                var ret = parser.parseExpression();
                if (parser.getToken().Type != SucoTokenType.Eof)
                    throw new ParseException("There is extra code. End of code expected.", parser._ix);
                return ret;
            }
            catch (ParseException pe1)
            {
                try
                {
                    // Try parsing as a list comprehension (e.g. “a: a.odd”).
                    var parser = new Parser { _ix = 0, _source = source };
                    var ret = parser.parseListComprehension();
                    if (parser.getToken().Type != SucoTokenType.Eof)
                        throw new ParseException("There is extra code. End of code expected.", parser._ix);
                    return ret;
                }
                catch (ParseException pe2)
                {
                    if (pe2.Index - 1 > pe1.Index)
                        throw new ParseException(pe2.Message, pe2.Index - 1, pe2.Highlights?.Select(h => new ParseExceptionHighlight(h.StartIndex - 1, h.EndIndex - 1)).ToArray());
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
                    throw new ParseException("Unterminated conditional operator: ‘:’ expected.", _ix, left, q, truePart);
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
                        throw new ParseException("Identifier expected after ‘.’.", _ix, oldIx);
                    left = new SucoMemberAccessExpression(left.StartIndex, token.EndIndex, left, token.StringValue);
                    _ix = token.EndIndex;
                }
                else if (token("(", out oldIx))
                {
                    // Call with argument list
                    if (token(")", out var closeParenIx))
                        left = new SucoCallExpression(left.StartIndex, _ix, left, new List<SucoExpression>());
                    else
                    {
                        var arguments = parseExpressionList();
                        if (!token(")"))
                            throw new ParseException("List of arguments: expecting ‘)’ (to end the list) or ‘,’ (to continue the list).", _ix, new ParseExceptionHighlight[] { oldIx }.Concat(arguments.Select(arg => (ParseExceptionHighlight) arg)).ToArray());
                        left = new SucoCallExpression(left.StartIndex, _ix, left, arguments);
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
                    throw new ParseException("Unmatched ‘[’: array must be terminated with ‘]’.", _ix, startIx);
                return new SucoArrayExpression(startIx, _ix, exprs);
            }

            if (token("(", out startIx))
            {
                var inner = parseExpression();
                if (!token(")"))
                    throw new ParseException("Unmatched ‘(’: missing ‘)’.", _ix, startIx);
                return inner.WithNewIndexes(startIx, _ix);
            }

            var tok = getToken();
            if (tok.Type == SucoTokenType.Number)
            {
                _ix = tok.EndIndex;
                return new SucoNumberLiteralExpression(tok.StartIndex, tok.EndIndex, tok.NumericalValue);
            }
            else if (tok.Type == SucoTokenType.Identifier)
            {
                _ix = tok.EndIndex;
                return new SucoIdentifierExpression(tok.StartIndex, tok.EndIndex, tok.StringValue);
            }

            throw new ParseException($"Unexpected code: ‘{_source.Substring(tok.StartIndex, tok.EndIndex - tok.StartIndex)}’.", tok.StartIndex);
        }

        private static readonly string[] _selectors = new[] { "$", "+" };
        private SucoExpression parseListComprehension(int? startIx = null, bool consumeCloseCurly = false)
        {
            var rStartIx = startIx ?? _ix;

            var clauses = new List<SucoListClause>();
            SucoExpression selector = null;

            nextClause:
            var clauseStartIx = _ix;
            var selectors = new Dictionary<string, int>();
            nextSelector:
            foreach (var sel in _selectors)
                if (token(sel, out var ix))
                {
                    if (selectors.ContainsKey(sel))
                        throw new ParseException($"Duplicate selector: ‘{sel}’.", _ix, selectors[sel]);
                    selectors[sel] = ix;
                    goto nextSelector;
                }
            var variableName = getToken();
            if (variableName.Type != SucoTokenType.Identifier)
                throw new ParseException($"Expected variable name{_selectors.Except(selectors.Keys).ToArray().Select(s => $" or ‘{s}’").JoinString()}.", variableName.StartIndex);
            _ix = variableName.EndIndex;

            var conditions = new List<SucoListCondition>();
            void commitClause(int endIndex) { clauses.Add(new SucoListClause(clauseStartIx, endIndex, variableName.StringValue, selectors.ContainsKey("$"), selectors.ContainsKey("+"), conditions)); }

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
                    throw new ParseException("Unmatched ‘(’: condition must end in ‘)’.", _ix, oldIx);
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
                _ix = tok.EndIndex;
                var tok2 = getToken();
                if (tok.Type != SucoTokenType.Identifier)
                    throw new ParseException("“from” must be followed by the name of a collection to select items from.", _ix, tok);
                conditions.Add(new SucoListFromCondition(tok.StartIndex, tok2.EndIndex, tok2.StringValue));
                _ix = tok2.EndIndex;
                goto nextCondition;
            }
            if (tok.Type == SucoTokenType.Identifier)
            {
                conditions.Add(new SucoListShortcutCondition(tok.StartIndex, tok.EndIndex, tok.StringValue));
                _ix = tok.EndIndex;
                goto nextCondition;
            }
            else if (tok.Type == SucoTokenType.Number)
            {
                conditions.Add(new SucoListNumberCondition(tok.StartIndex, tok.EndIndex, tok.NumericalValue));
                _ix = tok.EndIndex;
                goto nextCondition;
            }

            commitClause(_ix);

            done:
            if (consumeCloseCurly)
            {
                if (!token("}"))
                    throw new ParseException("Unmatched ‘{’: list comprehension must be terminated with ‘}’.", _ix, rStartIx);
            }
            return new SucoListComprehensionExpression(rStartIx, _ix, clauses, selector);
        }

        /// <summary>
        ///     Determines whether the specified <paramref name="expected"/> token exists at the specified location, and if
        ///     so, advances <see cref="_ix"/> to after it; otherwise, <see cref="_ix"/> is not modified.</summary>
        private bool token(string expected, out int oldIx)
        {
            var token = getToken();
            oldIx = token.StartIndex;
            if (token.StringValue != expected)
                return false;
            _ix = token.EndIndex;
            return true;
        }

        private static readonly string[] _tokens = {
            // Arithmetic
            "+", "-", "−", "*", "×", "^", "%",
            // Relational
            "<", "<=", "≤", ">", ">=", "≥", "=", "!=", "≠",
            // Logical
            "&", "|", "?", ":", "!",
            // Structural
            "{", "}", ",", "(", ")", ".", "[", "]",
            // Selectors
            "$",    // "+" is listed in Arithmetic
            // Filters
            "~", "←", "→", "↑", "↓"    // “v” parses as an identifier; “<”/“>” are in Relational; “^” is in Arithmetic
        };

        /// <summary>
        ///     Determines whether the specified <paramref name="expected"/> token exists at the current location, and if so,
        ///     advances <see cref="_ix"/> to after it; otherwise, <see cref="_ix"/> is not modified.</summary>
        private bool token(string expected) { return token(expected, out _); }

        /// <summary>
        ///     Determines whether any of the specified <paramref name="expected"/> tokens exist at the current location, and
        ///     if so, advances <see cref="_ix"/> to after it; otherwise, <see cref="_ix"/> is not modified. The value
        ///     associated with the token found is assigned to <paramref name="output"/>.</summary>
        private bool tokens<T>(out T output, params (string token, T output)[] options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            for (var i = 0; i < options.Length; i++)
                if (token(options[i].token, out _))
                {
                    output = options[i].output;
                    return true;
                }
            output = default;
            return false;
        }

        private SucoToken getToken()
        {
            var i = _ix;
            while (i < _source.Length && char.IsWhiteSpace(_source, i))
                i += char.IsSurrogate(_source, i) ? 2 : 1;
            var startIndex = i;

            if (i == _source.Length)
                return new SucoToken(SucoTokenType.Eof, startIndex, i);
            else if (char.IsLetter(_source, i) || _source[i] == '_')
            {
                var j = i + 1;
                while (j < _source.Length && (char.IsLetterOrDigit(_source, j) || _source[j] == '_'))
                    j += char.IsSurrogate(_source, j) ? 2 : 1;
                return new SucoToken(SucoTokenType.Identifier, _source.Substring(i, j - i), startIndex, j);
            }
            else if (char.IsDigit(_source, i))
            {
                var j = i;
                var numerical = BigInteger.Zero;
                while (j < _source.Length && char.IsDigit(_source, j))
                {
                    numerical = (numerical * 10 + (int) char.GetNumericValue(_source, j));
                    j += char.IsSurrogate(_source, j) ? 2 : 1;
                }
                return new SucoToken(SucoTokenType.Number, numerical, startIndex, j);
            }

            foreach (var token in _tokens)
                if (i + token.Length <= _source.Length && _source.Substring(i, token.Length) == token)
                    return new SucoToken(SucoTokenType.BuiltIn, token, startIndex, i + token.Length);
            return default;
        }
    }
}
