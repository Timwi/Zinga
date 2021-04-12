using System.Collections.Generic;

namespace Zinga.Suco
{
    public abstract class SucoListCondition : SucoNode
    {
        protected SucoListCondition(int startIndex, int endIndex)
            : base(startIndex, endIndex)
        {
        }

        public abstract bool Interpret(Dictionary<string, object> values, object cur, int curIx, int curCount, object prev, int? prevIx, int? prevCount);
    }
}