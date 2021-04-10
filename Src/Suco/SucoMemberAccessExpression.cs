namespace Zinga.Suco
{
    public class SucoMemberAccessExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public string MemberName { get; private set; }

        public SucoMemberAccessExpression(int startIndex, int endIndex, SucoExpression operand, string memberName, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Operand = operand;
            MemberName = memberName;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoMemberAccessExpression(startIndex, endIndex, Operand, MemberName);
        public override SucoExpression WithType(SucoType type) => new SucoMemberAccessExpression(StartIndex, EndIndex, Operand, MemberName, type);

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var op = Operand.DeduceTypes(env);
            try
            {
                var memberType = op.Type.GetMemberType(MemberName);
                if (memberType == null)
                    throw new SucoCompileException($"“{MemberName}” is not a valid member name on type “{op.Type}”.", Operand.EndIndex, EndIndex);
                return new SucoMemberAccessExpression(StartIndex, EndIndex, op, MemberName, memberType);
            }
            catch (SucoTempCompileException ce)
            {
                throw new SucoCompileException(ce.Message, StartIndex, EndIndex);
            }
        }

        public override SucoJsResult GetJavaScript(SucoEnvironment env)
        {
            try { return Operand.Type.GetMemberJs(MemberName, env, Operand); }
            catch (SucoTempCompileException ce) { throw new SucoCompileException(ce.Message, StartIndex, EndIndex); }
        }
    }
}