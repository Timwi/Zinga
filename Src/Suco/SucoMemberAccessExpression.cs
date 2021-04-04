namespace Zinga.Suco
{
    public class SucoMemberAccessExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public string MemberName { get; private set; }

        public SucoMemberAccessExpression(int startIndex, int endIndex, SucoExpression operand, string memberName)
            : base(startIndex, endIndex)
        {
            Operand = operand;
            MemberName = memberName;
        }
    }
}