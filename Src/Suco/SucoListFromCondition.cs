namespace Zinga.Suco
{
    public class SucoListFromCondition : SucoListCondition
    {
        private string VariableName;

        public SucoListFromCondition(int startIndex, int endIndex, string variableName, SucoType type = null)
            : base(startIndex, endIndex, type)
        {
            VariableName = variableName;
        }

        public override SucoExpression WithNewIndexes(int startIndex, int endIndex) => new SucoListFromCondition(startIndex, endIndex, VariableName);
        public override SucoExpression WithType(SucoType type) => new SucoListFromCondition(StartIndex, EndIndex, VariableName, type);
    }
}