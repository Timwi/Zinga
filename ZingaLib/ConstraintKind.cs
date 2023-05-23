namespace Zinga
{
    // Remember that the DB uses the integer values, so do not change them
    public enum ConstraintKind
    {
        [ConstraintKindInfo("Custom (anything goes)")]
        Custom = 0,

        [ConstraintKindInfo("Global (e.g. anti-knight)")]
        Global = 1,

        [ConstraintKindInfo("Single cell (e.g. odd/even)")]
        SingleCell = 9,

        [ConstraintKindInfo("Two orthogonal cells (e.g. Kropki dot)")]
        TwoCells = 7,

        [ConstraintKindInfo("Four cells in a 2×2 (e.g. clockwise)")]
        FourCells = 8,

        [ConstraintKindInfo("Path (e.g. thermometer)")]
        Path = 2,

        [ConstraintKindInfo("Region (e.g. killer cage)")]
        Region = 3,

        [ConstraintKindInfo("Matching regions (e.g. clone)")]
        MatchingRegions = 4,

        [ConstraintKindInfo("Row or column (e.g. sandwich)")]
        RowColumn = 5,

        [ConstraintKindInfo("Diagonal (e.g. little killer)")]
        Diagonal = 6
    }
}