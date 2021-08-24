using System.Runtime.CompilerServices;
using FluentMigrator;
using FluentMigrator.Builders.Create.Column;
using FluentMigrator.Builders.Create.Index;

namespace Smartstore.Core.Data.Migrations
{
    // TODO: (mg) (core) put all this in a migration base class (avoid "this" keyword and encapsulate dbSystemName).
    public static partial class MigrationExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TableExists(this Migration migration, string dbSystemName, string tableName)
        {
            return migration.IfDatabase(dbSystemName).Schema.Table(tableName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ColumnExists(this Migration migration, string dbSystemName, string tableName, string columnName)
        {
            return migration.IfDatabase(dbSystemName).Schema.Table(tableName).Column(columnName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndexExists(this Migration migration, string dbSystemName, string tableName, string indexName)
        {
            return migration.IfDatabase(dbSystemName).Schema.Table(tableName).Index(indexName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ForeignKeyExists(this Migration migration, string dbSystemName, string tableName, string keyName)
        {
            return migration.IfDatabase(dbSystemName).Schema.Table(tableName).Constraint(keyName).Exists();
        }

        public static void DeleteTables(this Migration migration, string dbSystemName, params string[] tableNames)
        {
            foreach (var name in tableNames)
            {
                if (migration.TableExists(dbSystemName, name))
                {
                    migration.Delete.Table(name);
                }
            }
        }

        public static void DeleteColumns(this Migration migration, string dbSystemName, string tableName, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                if (migration.ColumnExists(dbSystemName, tableName, name))
                {
                    migration.Delete.Column(name);
                }
            }
        }

        public static ICreateColumnAsTypeOrInSchemaSyntax CreateColumn(
            this Migration migration,
            string dbSystemName,
            string tableName, 
            string columnName)
        {
            if (!migration.ColumnExists(dbSystemName, tableName, columnName))
            {
                return migration.Create.Column(columnName).OnTable(tableName);
            }

            return null;
        }

        public static ICreateIndexColumnOptionsSyntax CreateIndex(
            this Migration migration, 
            string dbSystemName, 
            string tableName, 
            string columnName, 
            string indexName)
        {
            if (!migration.IndexExists(dbSystemName, tableName, indexName))
            {
                return migration.Create.Index(indexName).OnTable(tableName).OnColumn(columnName);
            }

            return null;
        }
    }
}
