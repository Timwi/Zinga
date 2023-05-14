using System.Linq;

namespace Zinga
{
    public sealed class PuzzleInfo
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int[][] Regions { get; private set; }
        public bool RowsUnique { get; private set; }
        public bool ColumnsUnique { get; private set; }
        public int[] Values { get; private set; }

        private PuzzleInfo() { }    // for Classify

        public PuzzleInfo(int width, int height, int[][] regions, bool rowsUnique, bool columnsUnique, int[] values)
        {
            Width = width;
            Height = height;
            Regions = regions;
            RowsUnique = rowsUnique;
            ColumnsUnique = columnsUnique;
            Values = values;
        }

        public bool IsDefault
        {
            get
            {
                if (Width != 9 || Height != 9 || !RowsUnique || !ColumnsUnique || Regions.Length != 9 || !Values.SequenceEqual(Enumerable.Range(1, 9)))
                    return false;
                for (var r = 0; r < 9; r++)
                    if (!Regions[r].SequenceEqual(Enumerable.Range(0, 9).Select(i => i % 3 + 3 * (r % 3) + 9 * (i / 3 + 3 * (r / 3)))))
                        return false;
                return true;
            }
        }

        public static readonly PuzzleInfo Default = new(9, 9, Enumerable.Range(0, 9).Select(r => Enumerable.Range(0, 9).Select(i => i % 3 + 3 * (r % 3) + 9 * (i / 3 + 3 * (r / 3))).ToArray()).ToArray(), true, true, Enumerable.Range(1, 9).ToArray());
    }
}