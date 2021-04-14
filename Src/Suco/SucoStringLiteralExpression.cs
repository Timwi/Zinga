using System.Collections.Generic;
using System.Linq;
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

        public override SucoExpression DeduceTypes(SucoEnvironment env)
        {
            var newPieces = Pieces.Select(p =>
            {
                if (p is not SucoStringLiteralPieceExpression expr)
                    return p;
                var typedExpr = expr.Expression.DeduceTypes(env);
                if (!typedExpr.Type.ImplicitlyConvertibleTo(SucoStringType.Instance))
                    throw new SucoCompileException($"Expression interpolated into a string literal is of type “{typedExpr.Type}”, which not implicitly convertible to string.", expr.Expression.StartIndex, expr.Expression.EndIndex);
                return typedExpr.ImplicitlyConvertTo(SucoStringType.Instance);
            }).ToArray();
            return new SucoStringLiteralExpression(StartIndex, EndIndex, newPieces, SucoStringType.Instance);
        }

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