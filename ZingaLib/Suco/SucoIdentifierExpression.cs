using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoIdentifierExpression : SucoExpression
    {
        public string Name { get; private set; }

        public SucoIdentifierExpression(int startIndex, int endIndex, string name, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Name = name;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            try
            {
                var variable = env.GetVariable(Name);
                if (variable == null)
                    throw new SucoCompileException($"Unknown variable “{Name}”.", StartIndex, EndIndex);
                return new SucoIdentifierExpression(StartIndex, EndIndex, Name, variable.Type);
            }
            catch (SucoTempCompileException tc)
            {
                throw new SucoCompileException(tc.Message, StartIndex, EndIndex);
            }
        }

        public override object Interpret(SucoEnvironment env)
        {
            try
            {
                return env.GetValue(Name);
            }
            catch (SucoTempCompileException tce)
            {
                throw new SucoCompileException(tce.Message, StartIndex, EndIndex);
            }
        }
    }
}