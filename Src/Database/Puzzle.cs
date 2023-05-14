using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RT.Serialization;
using RT.Util;

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
        public string LinksJson { get; set; }   // ClassifyJson of Link[]
        public DateTime LastUpdated { get; set; }
        public DateTime LastAccessed { get; set; }
        public bool Generated { get; set; }
        public string GivensJson { get; set; }  // ClassifyJson of (int cell, int value)[]
        public string InfoJson { get; set; }    // ClassifyJson of PuzzleInfo

        public Puzzle()
        {
            LastUpdated = DateTime.UtcNow;
            LastAccessed = DateTime.UtcNow;
        }

        [NotMapped] private Link[] _linksCache;
        [NotMapped]
        public Link[] Links
        {
            get => _linksCache ??= LinksJson.NullOr(l => ClassifyJson.Deserialize<Link[]>(l));
            set { LinksJson = value.NullOr(v => v.Length == 0 ? null : ClassifyJson.Serialize(v).ToString()); _linksCache = value; }
        }

        [NotMapped] private (int cell, int value)[] _givensCache;
        [NotMapped]
        public (int cell, int value)[] Givens
        {
            get => _givensCache ??= GivensJson.NullOr(cstr => ClassifyJson.Deserialize<(int cell, int value)[]>(cstr));
            set { GivensJson = value.NullOr(v => ClassifyJson.Serialize(v).ToString()); _givensCache = value; }
        }

        [NotMapped] private PuzzleInfo _infoCache;
        [NotMapped]
        public PuzzleInfo Info
        {
            get => _infoCache ??= InfoJson == null ? PuzzleInfo.Default : ClassifyJson.Deserialize<PuzzleInfo>(InfoJson);
            set { InfoJson = value.IsDefault ? null : ClassifyJson.Serialize(value).ToString(); _infoCache = value; }
        }
    }
}