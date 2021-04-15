using System;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoEnumType : SucoType
    {
        public string[] Names { get; private set; }
        public override string ToString() => $"[{Names.JoinString(", ")}]";

        public SucoEnumType(string[] names)
        {
            Names = names ?? throw new ArgumentNullException(nameof(names));
        }

        public override bool Equals(SucoType other) => other is SucoEnumType e && e.Names.SequenceEqual(Names);

        public override int GetHashCode() => Ut.ArrayHash(Names);
    }
}
