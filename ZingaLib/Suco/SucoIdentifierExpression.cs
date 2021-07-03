namespace Zinga.Suco
{
    public class SucoIdentifierExpression : SucoExpression
    {
        public string Name { get; private set; }
        public override string ToString() => Name;

        public SucoIdentifierExpression(int startIndex, int endIndex, string name, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Name = name;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            try
            {
                var type = env.GetVariableType(Name);
                if (type == null)
                    throw new SucoCompileException($"Unknown variable “{Name}”.", StartIndex, EndIndex);
                return new SucoIdentifierExpression(StartIndex, EndIndex, Name, type);
            }
            catch (SucoTempCompileException tc)
            {
                throw new SucoCompileException(tc.Message, StartIndex, EndIndex);
            }
        }

        public override object Interpret(SucoEnvironment env, int?[] grid)
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

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var val = env.GetValue(Name);
            return val == null ? this : new SucoConstant(StartIndex, EndIndex, Type, val);
        }
    }
}