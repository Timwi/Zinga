namespace Zinga.Suco
{
    public static class SucoExtensionMethods
    {
        public static SucoVariable ListOfCellsVariable(this string name) => new(name, new SucoListType(SucoCellType.Instance));
        public static SucoVariable ListOfListOfCellsVariable(this string name) => new(name, new SucoListType(new SucoListType(SucoCellType.Instance)));
        public static SucoVariable IntVariable(this string name) => new(name, SucoIntegerType.Instance);
        public static SucoVariable BoolVariable(this string name) => new(name, SucoBooleanType.Instance);
    }
}
