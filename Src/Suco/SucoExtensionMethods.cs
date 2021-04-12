namespace Zinga.Suco
{
    public static class SucoExtensionMethods
    {
        public static SucoVariable ListOfCellsVariable(this string name) => new(name, new SucoListType(SucoCellType.Instance));
    }
}
