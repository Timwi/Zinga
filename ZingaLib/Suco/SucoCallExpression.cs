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

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            try
            {
                var operand = Operand.DeduceTypes(env, context);
                if (operand.Type is not SucoFunctionType fnc)
                    throw new SucoCompileException($"“{operand.Type}” is not a function type.", operand.StartIndex, operand.EndIndex);

                var newArguments = Arguments.Select(arg => arg.DeduceTypes(env, context)).ToArray();
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

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var optimizedOperand = Operand.Optimize(env, givens);
            var optimizedArgs = Arguments.Select(arg => arg.Optimize(env, givens)).ToArray();
            return optimizedOperand is SucoConstant constOperand && optimizedArgs.All(arg => arg is SucoConstant)
                ? new SucoConstant(StartIndex, EndIndex, Type, ((SucoFunction) constOperand.Value).Interpret(optimizedArgs.Select(a => a.Type).ToArray(), optimizedArgs.Select(a => ((SucoConstant) a).Value).ToArray()))
                : new SucoCallExpression(StartIndex, EndIndex, optimizedOperand, optimizedArgs, Type);
        }

        public override object Interpret(SucoEnvironment env, int?[] grid)
        {
            var result = Operand.Interpret(env, grid);
            if (result is SucoFunction fnc)
                return fnc.Interpret(Arguments.Select(a => a.Type).ToArray(), Arguments.Select(a => a.Interpret(env, grid)).ToArray());
            throw new SucoCompileException($"Operand isn’t a function.", Operand.StartIndex, Operand.EndIndex);
        }
    }
}