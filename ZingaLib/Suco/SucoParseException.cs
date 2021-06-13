using System;
using System.Runtime.Serialization;

namespace Zinga.Suco
{
    [Serializable]
    public class SucoParseException : Exception
    {
        public SucoParseException(string message, SucoToken tok, params SucoParseExceptionHighlight[] highlights) : this(message, null, tok.StartIndex, tok.EndIndex, highlights) { }
        public SucoParseException(string message, int startIndex, int endIndex, params SucoParseExceptionHighlight[] highlights) : this(message, null, startIndex, endIndex, highlights) { }
        public SucoParseException(string message, Exception innerException, int startIndex, int endIndex, params SucoParseExceptionHighlight[] highlights) : base(message, innerException)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            Highlights = highlights;
        }
        protected SucoParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }
        public SucoParseExceptionHighlight[] Highlights { get; }
    }
}