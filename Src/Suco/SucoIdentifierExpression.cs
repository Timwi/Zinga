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

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var variable = env.GetVariable(Name);
            if (variable == null)
                throw new SucoCompileException($"Unknown variable “{Name}”.", StartIndex, EndIndex);
            return new SucoIdentifierExpression(StartIndex, EndIndex, Name, variable.Type);
        }

        public override object Interpret(Dictionary<string, object> values) =>
            values.TryGetValue(Name, out object value) ? value : throw new SucoCompileException($"The variable “{Name}” is not defined.", StartIndex, EndIndex);
    }
}