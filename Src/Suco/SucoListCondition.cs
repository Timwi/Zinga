using System.Collections.Generic;

namespace Zinga.Suco
{
    public abstract class SucoListCondition : SucoNode
    {
        protected SucoListCondition(int startIndex, int endIndex)
            : base(startIndex, endIndex)
        {
        }

        public abstract bool Interpret(Dictionary<string, object> values, IEnumerable<object> curList, object cur, int curIx, IEnumerable<object> prevList, object prev, int? prevIx);
    }
}