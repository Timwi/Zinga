namespace Zinga.Suco
{
    public class SucoMemberAccessExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public string MemberName { get; private set; }
        public override string ToString() => $"{Operand}.{MemberName}";

        public SucoMemberAccessExpression(int startIndex, int endIndex, SucoExpression operand, string memberName, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Operand = operand;
            MemberName = memberName;
        }

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var op = Operand.DeduceTypes(env, context);

            try
            {
                // Special case: the “.pos” member should work on any variable from a list comprehension
                if (MemberName == "pos" && op is SucoIdentifierExpression ident && env.IsVariableInListComprehension(ident.Name))
                    return new SucoPositionExpression(ident.StartIndex, ident.EndIndex, ident.Name, SucoType.Integer);

                return new SucoMemberAccessExpression(StartIndex, EndIndex, op, MemberName, op.Type.GetMemberType(MemberName, context));
            }
            catch (SucoTempCompileException ce)
            {
                throw new SucoCompileException(ce.Message, StartIndex, EndIndex);
            }
        }

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var optimizedOperand = Operand.Optimize(env, givens);
            if (optimizedOperand is SucoConstant c)
            {
                var val = optimizedOperand.Type.InterpretMemberAccess(MemberName, c.Value, env, givens);
                if (val != null)
                    return new SucoConstant(StartIndex, EndIndex, Type, val);
            }
            return new SucoMemberAccessExpression(StartIndex, EndIndex, optimizedOperand, MemberName, Type);
        }

        public override object Interpret(SucoEnvironment env, int?[] grid) => Operand.Type.InterpretMemberAccess(MemberName, Operand.Interpret(env, grid), env, grid);
    }
}