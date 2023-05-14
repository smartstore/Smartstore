using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Smartstore.Data.Sqlite
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class SqliteSmartRelationalConnection : SqliteRelationalConnection
    {
        public SqliteSmartRelationalConnection(
            RelationalConnectionDependencies dependencies,
            IRawSqlCommandBuilder rawSqlCommandBuilder,
            IDiagnosticsLogger<DbLoggerCategory.Infrastructure> logger)
            : base(dependencies, rawSqlCommandBuilder, logger)
        {
        }

        protected override DbConnection CreateDbConnection()
        {
            var connection = base.CreateDbConnection();
            InitializeDbConnection(connection);

            return connection;
        }

        private static void InitializeDbConnection(DbConnection connection)
        {
            if (connection is SqliteConnection sqliteConnection)
            {
                sqliteConnection.CreateCollation(
                    "NOCASE",
                    static (string left, string right) => 
                    {
                        // Override equality check (Sqlite cannot compare non-ascii chars uncased)
                        return string.Compare(left, right, ignoreCase: true);
                    });

                sqliteConnection.CreateFunction(
                    "lower",
                    static (string input) =>
                    {
                        // Override lower function (Sqlite ignores non-ascii chars)
                        return input?.ToLower();
                    },
                    isDeterministic: true);

                sqliteConnection.CreateFunction(
                    "upper",
                    static (string input) =>
                    {
                        // Override upper function (Sqlite ignores non-ascii chars)
                        return input?.ToUpper();
                    },
                    isDeterministic: true);

                sqliteConnection.CreateFunction<string, string, int?>(
                    "instr",
                    static (string input, string substr) =>
                    {
                        // Override instr function (Sqlite cannot compare non-ascii chars uncased)
                        if (input == null || substr == null)
                        {
                            return null;
                        }

                        var index = input.IndexOf(substr, StringComparison.CurrentCultureIgnoreCase);

                        // Sqlite instr is 1-based.
                        return index + 1;
                    },
                    isDeterministic: true);
            }
        }
    }
}
