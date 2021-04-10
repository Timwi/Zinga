namespace Zinga.Suco
{
    public class SucoIntegerType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoIntegerType;
        public static readonly SucoType Instance = new SucoIntegerType();
        private SucoIntegerType() { }
        public override string ToString() => "integer";

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => (op, rightOperand) switch
        {
            (BinaryOperator.Equal, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThan, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThan, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => SucoBooleanType.Instance,
            (BinaryOperator.Plus, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Minus, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Times, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Modulo, SucoIntegerType) => SucoIntegerType.Instance,
            (BinaryOperator.Power, SucoIntegerType) => SucoIntegerType.Instance,
            _ => base.GetBinaryOperatorType(op, rightOperand),
        };

        public override SucoJsResult GetBinaryOperatorJs(BinaryOperator op, SucoEnvironment env, SucoExpression leftOperand, SucoExpression rightOperand) => (op, rightOperand.Type) switch
        {
            (BinaryOperator.Equal, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a === $b)",
            (BinaryOperator.NotEqual, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a !== $b)",
            (BinaryOperator.LessThan, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a < $b)",
            (BinaryOperator.LessThanOrEqual, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a <= $b)",
            (BinaryOperator.GreaterThan, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a > $b)",
            (BinaryOperator.GreaterThanOrEqual, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a >= $b)",
            (BinaryOperator.Plus, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a + $b)",
            (BinaryOperator.Minus, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a - $b)",
            (BinaryOperator.Times, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a * $b)",
            (BinaryOperator.Modulo, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => $a % $b)",
            (BinaryOperator.Power, SucoIntegerType) => $"$op2({leftOperand.GetJavaScript(env).Code}, {rightOperand.GetJavaScript(env).Code}, ($a, $b) => Math.pow($a, $b))",
            _ => base.GetBinaryOperatorJs(op, env, leftOperand, rightOperand),
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Negative => SucoIntegerType.Instance,
            _ => base.GetUnaryOperatorType(op),
        };

        public override SucoJsResult GetUnaryOperatorJs(UnaryOperator op, SucoEnvironment env, SucoExpression operand) => op switch
        {
            UnaryOperator.Negative => $"$op1({operand.GetJavaScript(env).Code}, $a => -$a)",
            _ => base.GetUnaryOperatorJs(op, env, operand)
        };

        public override int GetHashCode() => 3;
    }
}
