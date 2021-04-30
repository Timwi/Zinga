﻿using System.Collections.Generic;

namespace Zinga.Suco
{
    public class SucoUnaryOperatorExpression : SucoExpression
    {
        public SucoExpression Operand { get; private set; }
        public UnaryOperator Operator { get; private set; }

        public SucoUnaryOperatorExpression(int startIndex, int endIndex, SucoExpression operand, UnaryOperator op, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            Operand = operand;
            Operator = op;
        }

        public override object Interpret(SucoEnvironment env) => Operand.Type.InterpretUnaryOperator(Operator, Operand.Interpret(env));

        protected override SucoExpression deduceTypes(SucoTypeEnvironment env, SucoContext context)
        {
            var op = Operand.DeduceTypes(env, context);
            try
            {
                var resultType = op.Type.GetUnaryOperatorType(Operator);
                if (resultType == null)
                    throw new SucoCompileException($"Type “{op.Type}” does not support the “{Operator}” unary operator.", StartIndex, EndIndex);
                return new SucoUnaryOperatorExpression(StartIndex, EndIndex, op, Operator, resultType);
            }
            catch (SucoTempCompileException ce)
            {
                throw new SucoCompileException(ce.Message, StartIndex, EndIndex);
            }
        }
    }
}