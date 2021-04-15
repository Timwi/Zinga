using System;
using System.Runtime.Serialization;

namespace Zinga.Suco
{
    [Serializable]
    public class SucoCompileException : Exception
    {
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }

        public SucoCompileException(string message, int startIndex, int endIndex) : this(message, startIndex, endIndex, null) { }

        public SucoCompileException(string message, int startIndex, int endIndex, Exception innerException) : base(message, innerException)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        protected SucoCompileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}