using System;
using System.Numerics;

namespace Zinga.Suco
{
    public abstract class Parser
    {
        protected int _ix = 0;
        protected readonly string _source;
        protected readonly string[] _tokens;

        protected Parser(string source, string[] tokens)
        {
            _source = source;
            _tokens = tokens;
        }

        /// <summary>
        ///     Determines whether the specified <paramref name="expected"/> token exists at the specified location, and if
        ///     so, advances <see cref="_ix"/> to after it; otherwise, <see cref="_ix"/> is not modified.</summary>
        protected bool token(string expected, out int oldIx)
        {
            var token = getToken();
            oldIx = token.StartIndex;
            if (_source.Substring(token.StartIndex, token.EndIndex - token.StartIndex) != expected)
                return false;
            _ix = token.EndIndex;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified <paramref name="expected"/> token exists at the current location, and if so,
        ///     advances <see cref="_ix"/> to after it; otherwise, <see cref="_ix"/> is not modified.</summary>
        protected bool token(string expected) { return token(expected, out _); }

        /// <summary>
        ///     Determines whether any of the specified <paramref name="expected"/> tokens exist at the current location, and
        ///     if so, advances <see cref="_ix"/> to after it; otherwise, <see cref="_ix"/> is not modified. The value
        ///     associated with the token found is assigned to <paramref name="output"/>.</summary>
        protected bool tokens<T>(out T output, params (string token, T output)[] options)
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

        protected SucoToken getToken()
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

        public void EnforceEof()
        {
            if (getToken().Type != SucoTokenType.Eof)
                throw new SucoParseException("There is extra code. End of code expected.", _ix);
        }
    }
}