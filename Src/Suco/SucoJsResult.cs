using System;

namespace Zinga.Suco
{
    public abstract class SucoJsResult
    {
        public virtual string Code => throw new InvalidOperationException("Internal error while attempting to generate JavaScript code.");

        public static implicit operator SucoJsResult(string code) => new SucoJsCodeResult(code);
        public static implicit operator SucoJsResult(SucoFunction fnc) => new SucoJsFunctionResult(fnc);
    }
}
