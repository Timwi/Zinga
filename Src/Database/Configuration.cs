using System.Data.Entity.Migrations;

namespace Zinga.Database
{
    internal sealed class Configuration : DbMigrationsConfiguration<Db>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "Zinga.Db";
        }

        protected override void Seed(Db db)
        {
        }
    }
}