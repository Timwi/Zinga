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

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var newPieces = Pieces.Select(p =>
            {
                if (p is not SucoStringLiteralPieceExpression expr)
                    return p;
                var typedExpr = expr.Expression.DeduceTypes(env, context);
                if (!typedExpr.Type.ImplicitlyConvertibleTo(SucoType.String))
                    throw new SucoCompileException($"Expression interpolated into a string literal is of type “{typedExpr.Type}”, which not implicitly convertible to string.", expr.Expression.StartIndex, expr.Expression.EndIndex);
                return typedExpr.ImplicitlyConvertTo(SucoType.String);
            }).ToArray();
            return new SucoStringLiteralExpression(StartIndex, EndIndex, newPieces, SucoType.String);
        }

        public override object Interpret(SucoEnvironment env)
        {
            var result = new StringBuilder();
            foreach (var piece in Pieces)
                if (piece is SucoStringLiteralPieceString str)
                    result.Append(str.StringValue);
                else if (piece is SucoStringLiteralPieceExpression expr)
                    result.Append(expr.Expression.Interpret(env));
                else
                    throw new SucoTempCompileException($"Unexpected type of string literal piece: “{piece.GetType().Name}”.");
            return result.ToString();
        }
    }
}