namespace Zinga.Suco
{
    public abstract class SucoStringLiteralPiece
    {
        public static implicit operator SucoStringLiteralPiece(string str) => new SucoStringLiteralPieceString(str);
        public static implicit operator SucoStringLiteralPiece(SucoExpression expr) => new SucoStringLiteralPieceExpression(expr);
    }
}
