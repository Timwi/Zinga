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

        // Logic
        public string ConstraintsJson { get; set; }
        public string GivensJson { get; set; }
        public string UnderSvg { get; set; }
        public string OverSvg { get; set; }

        public Puzzle()
        {
            LastUpdated = DateTime.UtcNow;
            LastAccessed = DateTime.UtcNow;
        }

        private Link[] _linksCache;
        public Link[] Links
        {
            get => _linksCache ??= LinksJson.NullOr(l => ClassifyJson.Deserialize<Link[]>(l));
            set { LinksJson = ClassifyJson.Serialize(value).ToString(); _linksCache = value; }
        }

        private SvgConstraint[] _constraintsCache;
        public SvgConstraint[] Constraints
        {
            get => _constraintsCache ??= ConstraintsJson.NullOr(cstr => ClassifyJson.Deserialize<SvgConstraint[]>(cstr));
            set { ConstraintsJson = ClassifyJson.Serialize(value).ToString(); _constraintsCache = value; }
        }

        private (int cell, int value)[] _givensCache;
        public (int cell, int value)[] Givens
        {
            get => _givensCache ??= GivensJson.NullOr(cstr => ClassifyJson.Deserialize<(int cell, int value)[]>(cstr));
            set { GivensJson = ClassifyJson.Serialize(value).ToString(); _givensCache = value; }
        }
    }
}