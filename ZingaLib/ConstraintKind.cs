namespace Zinga
{
    public enum ConstraintKind
    {
        [ConstraintKindInfo("Custom (anything goes)")]
        Custom,

        [ConstraintKindInfo("Global (e.g. anti-knight)")]
        Global,

        [ConstraintKindInfo("Path (e.g. thermometer)")]
        Path,

        [ConstraintKindInfo("Region (e.g. killer cage)")]
        Region,

        [ConstraintKindInfo("Matching regions (e.g. clone)")]
        MatchingRegions,

        [ConstraintKindInfo("Row or column (e.g. sandwich)")]
        RowColumn,

        [ConstraintKindInfo("Diagonal (e.g. little killer)")]
        Diagonal,

        [ConstraintKindInfo("Two orthogonal cells (e.g. Kropki dot)")]
        TwoCells,

        [ConstraintKindInfo("Four cells in a 2×2 (e.g. clockwise)")]
        FourCells,

        [ConstraintKindInfo("Single cell (e.g. odd/even)")]
        SingleCell
    }
}