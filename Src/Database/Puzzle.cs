using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RT.Serialization;
using RT.Util;
using SvgPuzzleConstraints;

namespace Zinga.Database
{
    public sealed class Puzzle
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PuzzleID { get; set; }

        // Metadata
        public string UrlName { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Rules { get; set; }
        public string LinksJson { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime LastAccessed { get; set; }
        public bool Generated { get; set; }
        public string GivensJson { get; set; }

        public Puzzle()
        {
            LastUpdated = DateTime.UtcNow;
            LastAccessed = DateTime.UtcNow;
        }

        private Link[] _linksCache;
        public Link[] Links
        {
            get => _linksCache ??= LinksJson.NullOr(l => ClassifyJson.Deserialize<Link[]>(l));
            set { LinksJson = value.NullOr(v => v.Length == 0 ? null : ClassifyJson.Serialize(v).ToString()); _linksCache = value; }
        }

        private (int cell, int value)[] _givensCache;
        public (int cell, int value)[] Givens
        {
            get => _givensCache ??= GivensJson.NullOr(cstr => ClassifyJson.Deserialize<(int cell, int value)[]>(cstr));
            set { GivensJson = value.NullOr(v => ClassifyJson.Serialize(v).ToString()); _givensCache = value; }
        }
    }
}