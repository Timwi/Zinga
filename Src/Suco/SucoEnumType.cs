using System;

namespace Zinga.Suco
{
    public class SucoEnumType : SucoType
    {
        public string[] Names { get; private set; }

        public SucoEnumType(string[] names)
        {
            Names = names ?? throw new ArgumentNullException(nameof(names));
        }
    }
}
