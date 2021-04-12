using System.Collections.Generic;
using System.Text;

namespace Zinga.Suco
{
    public class SucoStringLiteralExpression : SucoExpression
    {
        public SucoStringLiteralPiece[] Pieces { get; private set; }

        public SucoStringLiteralExpression(int startIndex, int endIndex, SucoStringLiteralPiece[] pieces, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Pieces = pieces;
        }

        public override SucoNode WithNewIndexes(int startIndex, int endIndex) => new SucoStringLiteralExpression(startIndex, endIndex, Pieces);
        public override SucoExpression WithType(SucoType type) => new SucoStringLiteralExpression(StartIndex, EndIndex, Pieces, type);
        public override SucoExpression DeduceTypes(SucoEnvironment env) => WithType(SucoStringType.Instance);

        public override object Interpret(Dictionary<string, object> values)
        {
            var result = new StringBuilder();
            foreach (var piece in Pieces)
                if (piece is SucoStringLiteralPieceString str)
                    result.Append(str.StringValue);
                else if (piece is SucoStringLiteralPieceExpression expr)
                    result.Append(expr.Expression.Interpret(values));
                else
                    throw new SucoTempCompileException($"Unexpected type of string literal piece: “{piece.GetType().Name}”.");
            return result.ToString();
        }
    }
}