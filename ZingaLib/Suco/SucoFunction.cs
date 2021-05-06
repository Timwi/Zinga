using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoFunction
    {
        private readonly Dictionary<SucoType[], (SucoType returnType, Func<object[], object> interpreter)> _dic = new(SucoTypeArrayComparer.Instance);

        public SucoFunction(params (SucoType[] parameters, SucoType returnType, Func<object[], object> interpreter)[] overloads)
        {
            if (overloads == null)
                throw new ArgumentNullException(nameof(overloads));
            foreach (var (parameters, returnType, generator) in overloads)
                _dic[parameters] = (returnType, generator);
        }

        public SucoType GetReturnType(params SucoType[] argumentTypes) => resolve(argumentTypes)?.returnType;
        public SucoType Type => new SucoFunctionType(_dic.Select(kvp => (kvp.Key, kvp.Value.returnType)).ToArray());
        public object Interpret(SucoType[] argumentTypes, object[] arguments)
        {
            var tup = resolve(argumentTypes);
            if (tup == null)
                throw new SucoTempCompileException($"Function does not accept argument types [{argumentTypes.JoinString(", ")}].");
            return tup.Value.interpreter(arguments);
        }

        public (SucoType returnType, Func<object[], object> interpreter)? resolve(params SucoType[] argumentTypes)
        {
            if (argumentTypes == null)
                throw new ArgumentNullException(nameof(argumentTypes));

            foreach (var (parameters, tup) in _dic)
                if (parameters.Length == argumentTypes.Length && Enumerable.Range(0, parameters.Length).All(i => argumentTypes[i].ImplicitlyConvertibleTo(parameters[i])))
                    return tup;

            return null;
        }
    }
}
