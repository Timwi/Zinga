using System.Collections.Generic;
using System.Linq;

namespace Zinga.Suco
{
    public class SucoCallExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public SucoExpression[] Arguments { get; private set; }

        public SucoCallExpression(int startIndex, int endIndex, SucoExpression operand, SucoExpression[] arguments, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Operand = operand;
            Arguments = arguments;
        }

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            try
            {
                var operand = Operand.DeduceTypes(env);
                if (operand.Type is not SucoFunctionType fnc)
                    throw new SucoCompileException($"“{operand.Type}” is not a function.", operand.StartIndex, operand.EndIndex);

                var newArguments = Arguments.Select(arg => arg.DeduceTypes(env)).ToArray();
                var (parameterTypes, returnType) = fnc.Resolve(newArguments.Select(a => a.Type).ToArray());
                for (var i = 0; i < newArguments.Length; i++)
                    newArguments[i] = newArguments[i].ImplicitlyConvertTo(parameterTypes[i]);
                return new SucoCallExpression(StartIndex, EndIndex, operand, newArguments, returnType);
            }
            catch (SucoFunctionResolutionException re)
            {
                throw new SucoCompileException(re.Message, StartIndex, EndIndex);
            }
        }

        public override object Interpret(Dictionary<string, object> values)
        {
            var result = Operand.Interpret(values);
            if (result is SucoFunction fnc)
                return fnc.Interpret(Arguments.Select(a => a.Type).ToArray(), Arguments.Select(a => a.Interpret(values)).ToArray());
            throw new SucoCompileException($"Operand isn’t a function.", Operand.StartIndex, Operand.EndIndex);
        }
    }
}