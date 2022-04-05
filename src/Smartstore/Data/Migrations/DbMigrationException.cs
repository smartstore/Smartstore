namespace Smartstore.Data.Migrations
{
    public class DbMigrationException : ApplicationException
    {
        private const string MSG_DDL = "Migration {0} could not be applied due to following error: {1}.";
        private const string MSG_SEED = "Migration {0} could not be seeded due to following error: {1}.";
        private const string MSG_SUFFIX = " All changes to the database were reversed to {0}. Please downgrade your application to a version which is known to be compatible with {0}.";

        public DbMigrationException(string message)
            : base(message)
        {
        }

        public DbMigrationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public DbMigrationException(string initialMigration, string targetMigration, Exception inner, bool isSeed)
            : base(GetMessage(initialMigration, targetMigration, inner, isSeed), inner)
        {
            InitialMigration = initialMigration;
            TargetMigration = targetMigration;
        }

        public string InitialMigration { get; private set; }
        public string TargetMigration { get; private set; }

        private static string GetMessage(string initialMigration, string targetMigration, Exception inner, bool isSeed)
        {
            var msg = (isSeed ? MSG_SEED : MSG_DDL).FormatCurrent(targetMigration, inner?.Message.EmptyNull());

            if (initialMigration.HasValue())
            {
                return msg + MSG_SUFFIX.FormatInvariant(initialMigration);
            }

            return msg;
        }
    }
}
