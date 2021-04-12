using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zinga.Database
{
    public sealed class PuzzleConstraint
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PuzzleConstraintID { get; set; }

        public int PuzzleID { get; set; }
        public int ConstraintID { get; set; }
        public string ValuesJson { get; set; }
    }
}
