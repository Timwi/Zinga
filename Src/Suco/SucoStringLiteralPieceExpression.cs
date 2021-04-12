namespace Zinga.Suco
{
    public class SucoStringLiteralPieceExpression : SucoStringLiteralPiece
    {
        public SucoExpression Expression { get; private set; }
        public SucoStringLiteralPieceExpression(SucoExpression expression) { Expression = expression; }
        public override string ToString() => Expression.GetType().Name;
    }
}
