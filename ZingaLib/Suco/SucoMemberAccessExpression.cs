using System.Collections.Generic;

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

        protected override SucoExpression deduceTypes(SucoEnvironment env, SucoContext context)
        {
            var op = Operand.DeduceTypes(env, context);
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

        public override object Interpret(Dictionary<string, object> values) => Operand.Type.InterpretMemberAccess(MemberName, Operand.Interpret(values));
    }
}