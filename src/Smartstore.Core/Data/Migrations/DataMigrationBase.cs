using FluentMigrator;

namespace Smartstore.Core.Data.Migrations
{
    public abstract class DataMigrationBase : Migration
    {
        public static string SqlServer => "SqlServer";
        public static string MySql => "MySql";

        /// <summary>
        /// Executes embedded SQL scripts for SQL Server and MySQL.
        /// </summary>
        /// <param name="path">
        /// Path of embedded scripts. Replaces placeholder "{0}" by <see cref="SqlServer"/> and <see cref="MySql"/>.
        /// </param>
        protected void ExecuteEmbeddedScripts(string path)
        {
            Guard.NotEmpty(path, nameof(path));

            IfDatabase(SqlServer).Execute.EmbeddedScript(path.FormatInvariant(SqlServer));
            IfDatabase(MySql).Execute.EmbeddedScript(path.FormatInvariant(MySql));
        }
    }
}
