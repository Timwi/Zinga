using System;
using System.Runtime.Serialization;

namespace Zinga.Suco
{
    [Serializable]
    internal class SucoFunctionResolutionException : Exception
    {
        public SucoFunctionResolutionException(string message) : base(message)
        {
        }

        public SucoFunctionResolutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SucoFunctionResolutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}