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
            _ => null,
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Negative => SucoIntegerType.Instance,
            _ => null
        };

        public override int GetHashCode() => 3;
    }
}
