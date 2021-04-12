using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RT.Serialization;

namespace Zinga.Database
{
    public sealed class PuzzleConstraint
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PuzzleConstraintID { get; set; }

        public int PuzzleID { get; set; }
        public int ConstraintID { get; set; }
        public string ValuesJson { get; set; }

        private Dictionary<string, object> _valuesCache;
        public Dictionary<string, object> Values
        {
            get => _valuesCache ??= ClassifyJson.Deserialize<Dictionary<string, object>>(ValuesJson);
            set { ValuesJson = ClassifyJson.Serialize(value).ToString(); _valuesCache = value; }
        }
    }
}
