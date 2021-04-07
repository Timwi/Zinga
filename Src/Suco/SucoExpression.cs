namespace Zinga.Suco
{
    public abstract class SucoExpression : SucoNode
    {
        public SucoType Type { get; private set; }

        public SucoExpression(int startIndex, int endIndex, SucoType type = null)
            : base(startIndex, endIndex)
        {
            Type = type;
        }

        public abstract SucoExpression WithType(SucoType type);
        public abstract SucoExpression DeduceTypes(SucoEnvironment env);
        public abstract SucoJsResult GetJavaScript(SucoEnvironment env);

        public SucoExpression ImplicitlyConvertTo(SucoType type) => Type.Equals(type) ? this : new SucoImplicitConversionExpression(StartIndex, EndIndex, this, type);
    }
}
