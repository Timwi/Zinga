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

        public override object Interpret(SucoEnvironment env, int?[] grid)
        {
            var result = new StringBuilder();
            foreach (var piece in Pieces)
                if (piece is SucoStringLiteralPieceString str)
                    result.Append(str.StringValue);
                else if (piece is SucoStringLiteralPieceExpression expr)
                    result.Append(expr.Expression.Interpret(env, grid));
                else
                    throw new SucoTempCompileException($"Unexpected type of string literal piece: “{piece.GetType().Name}”.");
            return result.ToString();
        }

        public override SucoExpression Optimize(SucoEnvironment env, int?[] givens)
        {
            var newPieces = new List<SucoStringLiteralPiece>();
            foreach (var piece in Pieces)
            {
                string addString = null;
                if (piece is SucoStringLiteralPieceString st)
                    addString = st.StringValue;
                else if (piece is SucoStringLiteralPieceExpression expr)
                {
                    var optimized = expr.Expression.Optimize(env, givens);
                    if (optimized is SucoConstant c)
                        addString = (string) c.Value;
                    else
                        newPieces.Add(piece);
                }

                if (addString != null && addString.Length > 0)
                {
                    if (newPieces.Count > 0 && newPieces[newPieces.Count - 1] is SucoStringLiteralPieceString s)
                        newPieces[newPieces.Count - 1] = new SucoStringLiteralPieceString(s.StringValue + addString);
                    else
                        newPieces.Add(new SucoStringLiteralPieceString(addString));
                }
            }
            if (newPieces.Count == 1 && newPieces[0] is SucoStringLiteralPieceString str)
                return new SucoConstant(StartIndex, EndIndex, Type, str.StringValue);
            return new SucoStringLiteralExpression(StartIndex, EndIndex, newPieces.ToArray(), Type);
        }
    }
}