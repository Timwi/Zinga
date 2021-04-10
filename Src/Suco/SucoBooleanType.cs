namespace Zinga.Suco
{
    public class SucoBooleanType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoBooleanType;
        public static readonly SucoType Instance = new SucoBooleanType();
        private SucoBooleanType() { }
        public override string ToString() => "boolean";

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => (op, rightOperand) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.And, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.Or, SucoBooleanType) => SucoBooleanType.Instance,
            _ => base.GetBinaryOperatorType(op, rightOperand)
        };

        public override SucoJsResult GetBinaryOperatorJs(BinaryOperator op, SucoEnvironment env, SucoExpression leftOperand, SucoExpression rightOperand) => (op, rightOperand.Type) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a === $b)",
            (BinaryOperator.NotEqual, SucoBooleanType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a !== $b)",
            (BinaryOperator.And, SucoBooleanType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a && $b)",
            (BinaryOperator.Or, SucoBooleanType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a || $b)",
            _ => base.GetBinaryOperatorJs(op, env, leftOperand, rightOperand)
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Not => SucoBooleanType.Instance,
            _ => base.GetUnaryOperatorType(op)
        };

        public override SucoJsResult GetUnaryOperatorJs(UnaryOperator op, SucoEnvironment env, SucoExpression operand) => op switch
        {
            UnaryOperator.Not => $"$op1({operand.GetJavaScript(env).Code}, $a => !$a)",
            _ => base.GetUnaryOperatorJs(op, env, operand)
        };

        public override int GetHashCode() => 1;
    }
}
