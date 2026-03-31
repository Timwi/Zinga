using Microsoft.EntityFrameworkCore;

namespace Zinga.Database
{
    public sealed class Db : DbContext
    {
        public static string ConnectionString { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer(ConnectionString);

        public DbSet<Puzzle> Puzzles { get; set; }
        public DbSet<Constraint> Constraints { get; set; }
        public DbSet<PuzzleConstraint> PuzzleConstraints { get; set; }
    }
}
