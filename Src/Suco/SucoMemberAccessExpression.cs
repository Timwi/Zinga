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

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoMemberAccessExpression(startIndex, endIndex, Operand, MemberName);
        public override SucoExpression WithType(SucoType type) => new SucoMemberAccessExpression(StartIndex, EndIndex, Operand, MemberName, type);
    }
}