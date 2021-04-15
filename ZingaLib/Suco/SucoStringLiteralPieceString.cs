namespace Zinga.Suco
{
    public class SucoStringLiteralPieceString : SucoStringLiteralPiece
    {
        public string StringValue { get; private set; }
        public SucoStringLiteralPieceString(string stringValue) { StringValue = stringValue; }
        public override string ToString() => StringValue;
    }
}
