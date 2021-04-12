namespace Zinga.Suco
{
    public class SucoBooleanType : SucoType
    {
        public override bool Equals(SucoType other) => other is SucoBooleanType;
        public static readonly SucoType Instance = new SucoBooleanType();
        private SucoBooleanType() { }
        public override string ToString() => "bool";

        public override SucoType GetBinaryOperatorType(BinaryOperator op, SucoType rightOperand) => (op, rightOperand) switch
        {
            (BinaryOperator.Equal, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.NotEqual, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.And, SucoBooleanType) => SucoBooleanType.Instance,
            (BinaryOperator.Or, SucoBooleanType) => SucoBooleanType.Instance,
            _ => base.GetBinaryOperatorType(op, rightOperand)
        };

        public override SucoType GetUnaryOperatorType(UnaryOperator op) => op switch
        {
            UnaryOperator.Not => SucoBooleanType.Instance,
            _ => base.GetUnaryOperatorType(op)
        };

        public override int GetHashCode() => 1;
    }
}
