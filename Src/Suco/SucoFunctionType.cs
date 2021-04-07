using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoFunctionType : SucoType
    {
        private readonly Dictionary<SucoType[], SucoType> _dic = new Dictionary<SucoType[], SucoType>(SucoTypeArrayComparer.Instance);

        public SucoFunctionType(params (SucoType[] parameters, SucoType returnType)[] overloads)
        {
            if (overloads == null)
                throw new ArgumentNullException(nameof(overloads));
            foreach (var (parameters, returnType) in overloads)
                _dic[parameters] = returnType;
        }

        public override bool Equals(SucoType other) => other is SucoFunctionType fnc &&
            fnc._dic.All(kvp => _dic.TryGetValue(kvp.Key, out var value) && kvp.Value.Equals(value)) &&
            _dic.All(kvp => fnc._dic.TryGetValue(kvp.Key, out var value) && kvp.Value.Equals(value));

        public override string ToString() => $"function: [{_dic.Select(kvp => $"({kvp.Key.JoinString(", ")}) → {kvp.Value}").JoinString(" | ")}]";

        public static int Resolve(SucoType[][] overloads, SucoType[] argumentTypes)
        {
            var candidates = overloads.SelectIndexWhere(paramTypes => paramTypes.Length == argumentTypes.Length && Enumerable.Range(0, paramTypes.Length).All(ix => argumentTypes[ix].ImplicitlyConvertibleTo(paramTypes[ix]))).ToArray();
            if (candidates.Length == 0)
                throw new SucoFunctionResolutionException($"The function does not accept this number of arguments of these types.");
            var perfectCandidate = candidates.FirstOrNull(ix => overloads[ix].Length == argumentTypes.Length && Enumerable.Range(0, overloads[ix].Length).All(ix => argumentTypes[ix].ImplicitlyConvertibleTo(overloads[ix][ix])));
            if (perfectCandidate != null)
                return perfectCandidate.Value;
            if (candidates.Length != 1)
                throw new SucoFunctionResolutionException($"The call to this function with these arguments is ambiguous.");
            return candidates[0];
        }

        public SucoType GetReturnType(params SucoType[] argumentTypes)
        {
            if (argumentTypes == null)
                throw new ArgumentNullException(nameof(argumentTypes));
            var candidates = _dic.Keys.ToArray();
            return _dic[candidates[Resolve(candidates, argumentTypes)]];
        }

        public override int GetHashCode()
        {
            // The hash code needs to be independent of the order the key/value pairs are in the dictionary
            var hash = 0;
            foreach (var kvp in _dic)
                hash ^= Ut.ArrayHash(kvp.Key) * 147 + kvp.Value.GetHashCode();
            return hash;
        }
    }
}
