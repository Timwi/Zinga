using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zinga.Database
{
    public sealed class Puzzle
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PuzzleID { get; set; }

        public string PuzzleHash { get; set; }
        public string ConstraintsJson { get; set; }
        public string UnderSvg { get; set; }
        public string OverSvg { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Rules { get; set; }

        public DateTime LastAccessed { get; set; }
    }
}