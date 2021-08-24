using System.Runtime.CompilerServices;
using FluentMigrator;
using FluentMigrator.Builders.Create.Column;
using FluentMigrator.Builders.Create.Index;
using FluentMigrator.Builders.Schema;

namespace Smartstore.Core.Data.Migrations
{
    public static partial class MigrationExtensions
    {
        private const string DEFAULT_SCHEMA = "dbo";

        #region Migration

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

        public static ICreateIndexColumnOptionsSyntax CreateIndex(this Migration migration, string tableName, string columnName, string indexName)
        {
            if (!migration.Schema.IndexExists(tableName, indexName))
            {
                return migration.Create.Index(indexName).OnTable(tableName).OnColumn(columnName);
            }

            return null;
        }

        #endregion

        #region Schema

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TableExists(this ISchemaExpressionRoot schema, string tableName, string schemaName = default)
        {
            // TODO: (mg) (core) "dbo" is not always the default schema. We need to be db-provider-agnostic.
            return schema.Schema(schemaName ?? DEFAULT_SCHEMA).Table(tableName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ColumnExists(this ISchemaExpressionRoot schema, string tableName, string columnName, string schemaName = default)
        {
            return schema.Schema(schemaName ?? DEFAULT_SCHEMA).Table(tableName).Column(columnName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndexExists(this ISchemaExpressionRoot schema, string tableName, string indexName, string schemaName = default)
        {
            return schema.Schema(schemaName ?? DEFAULT_SCHEMA).Table(tableName).Index(indexName).Exists();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ForeignKeyExists(this ISchemaExpressionRoot schema, string tableName, string keyName, string schemaName = default)
        {
            return schema.Schema(schemaName ?? DEFAULT_SCHEMA).Table(tableName).Constraint(keyName).Exists();
        }

        #endregion
    }
}
