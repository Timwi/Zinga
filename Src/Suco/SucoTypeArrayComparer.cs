using System.Collections.Generic;
using System.Linq;
using RT.Util;

namespace Zinga.Suco
{
    public sealed class SucoTypeArrayComparer : IEqualityComparer<SucoType[]>
    {
        public static readonly SucoTypeArrayComparer Instance = new SucoTypeArrayComparer();
        public bool Equals(SucoType[] x, SucoType[] y) => x == null ? y == null : y != null && y.SequenceEqual(x);
        public int GetHashCode(SucoType[] obj) => Ut.ArrayHash(obj);
        private SucoTypeArrayComparer() { }
    }
}
