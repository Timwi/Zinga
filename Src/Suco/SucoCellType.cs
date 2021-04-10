namespace Zinga.Suco
{
    public class SucoCellType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoCellType;
        public static readonly SucoType Instance = new SucoCellType();
        private SucoCellType() { }
        public override string ToString() => "cell";

        public override SucoType GetMemberType(string memberName) => memberName switch
        {
            "value" => SucoIntegerType.Instance,
            "pos" => SucoIntegerType.Instance,
            "x" => SucoIntegerType.Instance,
            "y" => SucoIntegerType.Instance,
            "box" => SucoIntegerType.Instance,
            _ => base.GetMemberType(memberName)
        };

        public override SucoJsResult GetMemberJs(string memberName, SucoEnvironment env, SucoExpression callee) => memberName switch
        {
            "value" => $"{callee.GetJavaScript(env).Code}.value",
            "pos" => $"{callee.GetJavaScript(env).Code}.pos",
            "x" => $"{callee.GetJavaScript(env).Code}.x",
            "y" => $"{callee.GetJavaScript(env).Code}.y",
            "box" => $"{callee.GetJavaScript(env).Code}.box",
            _ => base.GetMemberJs(memberName, env, callee)
        };

        public override int GetHashCode() => 2;
    }
}
