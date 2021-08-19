using System.Runtime.CompilerServices;
using FluentMigrator;
using FluentMigrator.Builders.Create.Column;
using FluentMigrator.Builders.Schema;

namespace Smartstore.Core.Data.Migrations
{
    public static partial class MigrationExtensions
    {
        public static string SqlServer => "SqlServer";
        public static string MySql => "MySql";

        #region Migration

        public static void ExecuteEmbeddedScripts(this Migration migration, string sqlServerPath, string mySqlPath)
        {
            if (sqlServerPath.HasValue())
            {
                migration.IfDatabase(SqlServer).Execute.EmbeddedScript(sqlServerPath);
            }

            if (mySqlPath.HasValue())
            {
                migration.IfDatabase(MySql).Execute.EmbeddedScript(mySqlPath);
            }
        }

        public static void DeleteTables(this Migration migration, params string[] tableNames)
        {
            foreach (var name in tableNames)
            {
                if (migration.Schema.TableExists(name))
                {
                    migration.Delete.Table(name);
                }
            }
        }

        public static void DeleteColumns(this Migration migration, string tableName, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                if (migration.Schema.ColumnExists(tableName, name))
                {
                    migration.Delete.Column(name);
                }
            }
        }

        public static ICreateColumnAsTypeOrInSchemaSyntax CreateColumn(this Migration migration, string tableName, string columnName)
        {
            if (!migration.Schema.ColumnExists(tableName, columnName))
            {
                return migration.Create.Column(columnName).OnTable(tableName);
            }

            return null;
        }

        #endregion

        #region Schema

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TableExists(this ISchemaExpressionRoot schema, string tableName, string schemaName = "dbo")
        {
            return schema.Schema(schemaName).Table(tableName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ColumnExists(this ISchemaExpressionRoot schema, string tableName, string columnName, string schemaName = "dbo")
        {
            return schema.Schema(schemaName).Table(tableName).Column(columnName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ForeignKeyExists(this ISchemaExpressionRoot schema, string tableName, string keyName, string schemaName = "dbo")
        {
            return schema.Schema(schemaName).Table(tableName).Constraint(keyName).Exists();
        }

        #endregion
    }
}
