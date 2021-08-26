using System;

namespace Smartstore.Core.Data.Migrations
{
    public class DbMigrationException : ApplicationException
    {
        private const string MSG_DDL = "Migration '{0}' could not be applied due to following error: '{1}'.";
        private const string MSG_SEED = "Migration '{0}' could not be seeded due to following error: '{1}'.";
        private const string MSG_SUFFIX = " All changes to the database were reversed to '{2}'. Please downgrade your application to a version which is known to be compatible with '{2}'.";

        public DbMigrationException(string message)
            : base(message)
        {
        }

        public DbMigrationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public DbMigrationException(string initialMigration, string targetMigration, Exception inner, bool isSeed)
            : base(((isSeed ? MSG_SEED : MSG_DDL) + MSG_SUFFIX).FormatCurrent(targetMigration.EmptyNull(), inner?.Message.EmptyNull(), initialMigration.EmptyNull()), inner)
        {
            InitialMigration = initialMigration;
            TargetMigration = targetMigration;
        }

        public string InitialMigration { get; private set; }
        public string TargetMigration { get; private set; }
    }
}
