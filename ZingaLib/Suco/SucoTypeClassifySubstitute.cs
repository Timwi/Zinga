using RT.Serialization;

namespace Zinga.Suco
{
    class SucoTypeClassifySubstitute : IClassifySubstitute<SucoType, string>
    {
        public string ToSubstitute(SucoType instance) => instance.ToString();
        public SucoType FromSubstitute(string instance) => SucoType.Parse(instance);
    }
}
