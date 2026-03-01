using System;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoEnumType(string[] names) : SucoType
    {
        public string[] Names { get; private set; } = names ?? throw new ArgumentNullException(nameof(names));
        public override string ToString() => $"[{Names.JoinString(", ")}]";
        public override Type CsType => typeof(string);

        public override bool Equals(SucoType other) => other is SucoEnumType e && e.Names.SequenceEqual(Names);

        public override int GetHashCode() => Ut.ArrayHash(Names);
    }
}
