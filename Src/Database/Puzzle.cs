using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using RT.Util;
using RT.Util.ExtensionMethods;

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
        public string Links { get; set; }

        public DateTime LastAccessed { get; set; }

        public void SetHash()
        {
            using var sha1 = SHA1.Create();
            PuzzleHash = sha1.ComputeHash((ConstraintsJson + UnderSvg + OverSvg + Title + Author + Rules + Links).ToUtf8()).ToHex();
        }
    }
}