using System;
using System.Runtime.Serialization;

namespace Zinga.Suco
{
    [Serializable]
    public class SucoTempCompileException : Exception
    {
        public SucoTempCompileException(string message) : this(message, null) { }
        public SucoTempCompileException(string message, Exception innerException) : base(message, innerException) { }
        protected SucoTempCompileException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}