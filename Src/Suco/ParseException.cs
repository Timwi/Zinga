using System;
using System.Runtime.Serialization;

namespace Zinga.Suco
{
    [Serializable]
    public class ParseException : Exception
    {
        public ParseException(string message, int index, params ParseExceptionHighlight[] highlights) : this(message, null, index, highlights) { }
        public ParseException(string message, Exception innerException, int index, params ParseExceptionHighlight[] highlights) : base(message, innerException)
        {
            Index = index;
            Highlights = highlights;
        }
        protected ParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public int Index { get; private set; }
        public ParseExceptionHighlight[] Highlights { get; }
    }
}