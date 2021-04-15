using System;
using System.Runtime.Serialization;

namespace Zinga.Suco
{
    [Serializable]
    public class SucoParseException : Exception
    {
        public SucoParseException(string message, int index, params SucoParseExceptionHighlight[] highlights) : this(message, null, index, highlights) { }
        public SucoParseException(string message, Exception innerException, int index, params SucoParseExceptionHighlight[] highlights) : base(message, innerException)
        {
            Index = index;
            Highlights = highlights;
        }
        protected SucoParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public int Index { get; private set; }
        public SucoParseExceptionHighlight[] Highlights { get; }
    }
}