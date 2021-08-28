using FluentMigrator;
using FluentMigrator.Builders.Create.Column;
using FluentMigrator.Builders.Create.Index;
using Smartstore.Data;

namespace Smartstore.Core.Data.Migrations
{
    // TODO: (mg) (core) add code comments to DatabaseMigrationBase when ready.
    // TODO: (mg) (core) We gonna use AutoReversingMigration whereever possible. This class is counter-productive. Maybe extension methods?
    public abstract class DatabaseMigrationBase : Migration
    {
        private string _dbSystemName;

        protected string DbSystemName
        {
            get => _dbSystemName ??= DataSettings.Instance.DbFactory.DbSystem.ToString();
        }

        protected virtual bool TableExists(string tableName)
        {
            return IfDatabase(DbSystemName).Schema.Table(tableName).Exists();
        }

        protected virtual bool ColumnExists(string tableName, string columnName)
        {
            return IfDatabase(DbSystemName).Schema.Table(tableName).Column(columnName).Exists();
        }

        protected virtual bool IndexExists(string tableName, string indexName)
        {
            return IfDatabase(DbSystemName).Schema.Table(tableName).Index(indexName).Exists();
        }

        protected virtual bool ForeignKeyExists(string tableName, string keyName)
        {
            return IfDatabase(DbSystemName).Schema.Table(tableName).Constraint(keyName).Exists();
        }

        protected virtual void DeleteTables(params string[] tableNames)
        {
            foreach (var name in tableNames)
            {
                if (TableExists(name))
                {
                    Delete.Table(name);
                }
            }
        }

        protected virtual void DeleteColumns(string tableName, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                if (ColumnExists(tableName, name))
                {
                    Delete.Column(name);
                }
            }
        }

        protected virtual ICreateColumnAsTypeOrInSchemaSyntax CreateColumn(string tableName, string columnName)
        {
            if (!ColumnExists(tableName, columnName))
            {
                return Create.Column(columnName).OnTable(tableName);
            }

            return null;
        }

        protected virtual ICreateIndexColumnOptionsSyntax CreateIndex(string tableName, string columnName, string indexName)
        {
            if (!IndexExists(tableName, indexName))
            {
                return Create.Index(indexName).OnTable(tableName).OnColumn(columnName);
            }

            return null;
        }
    }
}
