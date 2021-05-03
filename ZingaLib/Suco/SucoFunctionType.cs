using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoFunctionType : SucoType
    {
        private readonly Dictionary<SucoType[], SucoType> _dic = new(SucoTypeArrayComparer.Instance);
        public override Type CsType => typeof(SucoFunction);

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

        public (SucoType[] parameterTypes, SucoType returnType) Resolve(SucoType[] argumentTypes)
        {
            var candidates = _dic.Where(kvp => kvp.Key.Length == argumentTypes.Length && Enumerable.Range(0, argumentTypes.Length).All(ix => argumentTypes[ix].ImplicitlyConvertibleTo(kvp.Key[ix]))).ToArray();
            if (candidates.Length == 0)
                throw new SucoFunctionResolutionException($"The function does not accept this number of arguments of these types.");
            var perfectCandidate = candidates.FirstOrNull(kvp => argumentTypes.Zip(kvp.Key, (t1, t2) => t1.Equals(t2)).All(b => b));
            if (perfectCandidate != null)
                return (perfectCandidate.Value.Key, perfectCandidate.Value.Value);
            if (candidates.Length != 1)
                throw new SucoFunctionResolutionException($"The call to this function with these arguments is ambiguous.");
            return (candidates[0].Key, candidates[0].Value);
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
