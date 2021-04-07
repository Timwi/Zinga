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
            _ => null
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Not => SucoBooleanType.Instance,
            _ => null
        };
        public override int GetHashCode() => 1;
    }
}
