using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Smartstore.Utilities;

namespace Smartstore.Data.Caching.Internal
{
    /// <summary>
    /// A Table's EntityInfo and policy information.
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
        /// Policy annotation.
        /// </summary>
        public CacheableEntityAttribute Policy { get; set; }

        /// <summary>
        /// Debug info.
        /// </summary>
        public override string ToString() => $"{ClrType}::{TableName}";
    }

    /// <summary>
    /// SqlCommands Utils
    /// </summary>
    public class EfSqlCommandProcessor
    {
        // Keys are both entity CLR type and table name (therefore object, not Type)
        private readonly ConcurrentDictionary<Type, Lazy<Dictionary<object, TableEntityInfo>>> _contextTableInfos =
            new ConcurrentDictionary<Type, Lazy<Dictionary<object, TableEntityInfo>>>();

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

        /// <summary>
        /// Returns all of the given context's entity infos.
        /// </summary>
        public Dictionary<object, TableEntityInfo> GetAllEntityInfos(DbContext context)
        {
            return _contextTableInfos.GetOrAdd(context.GetType(),
                _ => new Lazy<Dictionary<object, TableEntityInfo>>(() =>
                {
                    var infos = new Dictionary<object, TableEntityInfo>();
                    foreach (var entityType in context.Model.GetEntityTypes())
                    {
                        var clrType = entityType.ClrType;
                        var tableName = entityType.GetTableName();
                        var info = new TableEntityInfo
                        {
                            ClrType = clrType,
                            TableName = tableName,
                            Policy = clrType.GetAttribute<CacheableEntityAttribute>(false)
                        };

                        infos[clrType] = info;
                        infos[tableName] = info;
                    }
                    return infos;
                },
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        /// <summary>
        /// Extracts the table names of an SQL command.
        /// </summary>
        public SortedSet<string> GetSqlCommandTableNames(string commandText)
        {
            var commandTextKey = $"{XxHashUnsafe.ComputeHash(commandText):X}";
            return _commandTableNames.GetOrAdd(commandTextKey,
                    _ => new Lazy<SortedSet<string>>(() => GetRawSqlCommandTableNames(commandText),
                            LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        /// <summary>
        /// Extracts the entity types of an SQL command.
        /// </summary>
        public IList<TableEntityInfo> GetSqlCommandEntityInfos(string commandText, Dictionary<object, TableEntityInfo> allEntityInfos)
        {
            var commandTableNames = GetSqlCommandTableNames(commandText);
            return commandTableNames
                .Select(tableName => allEntityInfos.Get(tableName))
                .Where(x => x != null)
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