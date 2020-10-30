using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Smartstore.Utilities;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// A Table's EntityInfo
    /// </summary>
    public class TableEntityInfo
    {
        /// <summary>
        /// Gets the CLR class that is used to represent instances of this type.
        /// Returns null if the type does not have a corresponding CLR class (known as a shadow type).
        /// </summary>
        public Type ClrType { set; get; }

        /// <summary>
        /// The Corresponding table's name.
        /// </summary>
        public string TableName { set; get; }

        /// <summary>
        /// Debug info.
        /// </summary>
        public override string ToString() => $"{ClrType}::{TableName}";
    }

    /// <summary>
    /// SqlCommands Utils
    /// </summary>
    public interface IEfSqlCommandsProcessor
    {
        /// <summary>
        /// Extracts the table names of an SQL command.
        /// </summary>
        SortedSet<string> GetSqlCommandTableNames(string commandText);

        /// <summary>
        /// Extracts the entity types of an SQL command.
        /// </summary>
        IList<Type> GetSqlCommandEntityTypes(string commandText, IList<TableEntityInfo> allEntityTypes);

        /// <summary>
        /// Returns all of the given context's entity infos.
        /// </summary>
        IList<TableEntityInfo> GetAllEntityInfos(DbContext context);

        /// <summary>
        /// Is `insert`, `update` or `delete`?
        /// </summary>
        bool IsCrudCommand(string text);
    }

    public class EfSqlCommandsProcessor : IEfSqlCommandsProcessor
    {
        private readonly ConcurrentDictionary<Type, Lazy<List<TableEntityInfo>>> _contextTableNames =
                    new ConcurrentDictionary<Type, Lazy<List<TableEntityInfo>>>();

        private readonly ConcurrentDictionary<string, Lazy<SortedSet<string>>> _commandTableNames =
                    new ConcurrentDictionary<string, Lazy<SortedSet<string>>>();

        /// <summary>
        /// Is `insert`, `update` or `delete`?
        /// </summary>
        public bool IsCrudCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] crudMarkers = { "insert ", "update ", "delete ", "create " };

            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                foreach (var marker in crudMarkers)
                {
                    if (line.Trim().StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IList<TableEntityInfo> GetAllEntityInfos(DbContext context)
        {
            return _contextTableNames.GetOrAdd(context.GetType(),
                _ => new Lazy<List<TableEntityInfo>>(() =>
                {
                    var infos = new List<TableEntityInfo>();
                    foreach (var entityType in context.Model.GetEntityTypes())
                    {
                        infos.Add(
                            new TableEntityInfo
                            {
                                ClrType = entityType.ClrType,
                                TableName = entityType.GetTableName()
                            });
                    }
                    return infos;
                },
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        public SortedSet<string> GetSqlCommandTableNames(string commandText)
        {
            var commandTextKey = $"{XxHashUnsafe.ComputeHash(commandText):X}";
            return _commandTableNames.GetOrAdd(commandTextKey,
                    _ => new Lazy<SortedSet<string>>(() => GetRawSqlCommandTableNames(commandText),
                            LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        public IList<Type> GetSqlCommandEntityTypes(string commandText, IList<TableEntityInfo> allEntityTypes)
        {
            var commandTableNames = GetSqlCommandTableNames(commandText);
            return allEntityTypes.Where(entityType => commandTableNames.Contains(entityType.TableName))
                                .Select(entityType => entityType.ClrType)
                                .ToList();
        }

        private static SortedSet<string> GetRawSqlCommandTableNames(string commandText)
        {
            string[] tableMarkers = { "FROM", "JOIN", "INTO", "UPDATE" };

            var tables = new SortedSet<string>();

            var sqlItems = commandText.Split(new[] { " ", "\r\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var sqlItemsLength = sqlItems.Length;
            for (var i = 0; i < sqlItemsLength; i++)
            {
                foreach (var marker in tableMarkers)
                {
                    if (!sqlItems[i].Equals(marker, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ++i;
                    if (i >= sqlItemsLength)
                    {
                        continue;
                    }

                    var tableName = string.Empty;

                    var tableNameParts = sqlItems[i].Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    if (tableNameParts.Length == 1)
                    {
                        tableName = tableNameParts[0].Trim();
                    }
                    else if (tableNameParts.Length >= 2)
                    {
                        tableName = tableNameParts[1].Trim();
                    }

                    if (string.IsNullOrWhiteSpace(tableName))
                    {
                        continue;
                    }

                    tableName = tableName.Replace("[", "")
                                        .Replace("]", "")
                                        .Replace("'", "")
                                        .Replace("`", "")
                                        .Replace("\"", "");
                    tables.Add(tableName);
                }
            }
            return tables;
        }
    }
}