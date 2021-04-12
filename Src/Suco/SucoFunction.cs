using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util;

namespace Zinga.Suco
{
    public class SucoFunction
    {
        private readonly Dictionary<SucoType[], (SucoType returnType, Func<SucoExpression[], SucoEnvironment, SucoJsResult> generator)> _dic = new(SucoTypeArrayComparer.Instance);

        public SucoFunction(params (SucoType[] parameters, SucoType returnType, Func<SucoExpression[], SucoEnvironment, SucoJsResult> generator)[] overloads)
        {
            if (overloads == null)
                throw new ArgumentNullException(nameof(overloads));
            foreach (var (parameters, returnType, generator) in overloads)
                _dic[parameters] = (returnType, generator);
        }

        public SucoType GetReturnType(params SucoType[] argumentTypes) => resolve(argumentTypes)?.returnType;
        public Func<SucoExpression[], SucoEnvironment, SucoJsResult> GetJavaScriptGenerator(params SucoType[] argumentTypes) => resolve(argumentTypes)?.generator;
        public SucoJsResult GetJavaScript(SucoEnvironment env, params SucoExpression[] arguments) => resolve(arguments.Select(arg => arg.Type).ToArray())?.generator(arguments, env);
        public SucoType Type => new SucoFunctionType(_dic.Select(kvp => (kvp.Key, kvp.Value.returnType)).ToArray());

        private (SucoType returnType, Func<SucoExpression[], SucoEnvironment, SucoJsResult> generator)? resolve(params SucoType[] argumentTypes)
        {
            if (argumentTypes == null)
                throw new ArgumentNullException(nameof(argumentTypes));

            foreach (var (parameters, tup) in _dic.ToTuples())
                if (parameters.Length == argumentTypes.Length && Enumerable.Range(0, parameters.Length).All(i => argumentTypes[i].ImplicitlyConvertibleTo(parameters[i])))
                    return (tup.returnType, (exprs, env) => tup.generator(exprs.Select((e, ix) => e.ImplicitlyConvertTo(parameters[ix])).ToArray(), env));

            return null;
        }
    }
}
