using System.Security.Cryptography;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Zinga.Suco
{
    public class SucoStringType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoStringType;
        public static readonly SucoType Instance = new SucoStringType();
        private SucoStringType() { }
        public override string ToString() => "string";
        public override int GetHashCode() => 4;

        public override SucoType GetMemberType(string memberName) => memberName switch
        {
            "hash" => SucoStringType.Instance,
            _ => base.GetMemberType(memberName)
        };

        public override object InterpretMemberAccess(string memberName, object operand) => memberName switch
        {
            "hash" => hash((string) operand),
            _ => base.InterpretMemberAccess(memberName, operand)
        };

        private string hash(string str)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(str.ToUtf8()).ToHex();
        }
    }
}
