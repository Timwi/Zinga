namespace Zinga.Suco
{
    public abstract class SucoListCondition : SucoNode
    {
        protected SucoListCondition(int startIndex, int endIndex)
            : base(startIndex, endIndex)
        {
        }

        public abstract SucoListCondition DeduceTypes(SucoTypeEnvironment env, SucoContext context, SucoType elementType);

        public abstract object Optimize(SucoEnvironment env, int?[] givens);
        public abstract bool? Interpret(SucoEnvironment env, int?[] grid);
    }
}