using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Data.Caching.Internal;

namespace Smartstore.Data.Caching.Internal
{
    /// <summary>
    /// Cache dependencies processor
    /// </summary>
    public sealed class EfCacheDependenciesProcessor
    {
        private readonly IEfDebugLogger _logger;
        private readonly DbCache _cache;
        private readonly EfSqlCommandProcessor _sqlCommandProcessor;

        public EfCacheDependenciesProcessor(IEfDebugLogger logger, DbCache cache, EfSqlCommandProcessor sqlCommandsProcessor)
        {
            _logger = logger;
            _cache = cache;
            _sqlCommandProcessor = sqlCommandsProcessor;
        }

        /// <summary>
        /// Finds the related table names of the current query.
        /// </summary>
        public SortedSet<string> GetCacheDependencies(DbCommand command, DbContext context, EfCachePolicy cachePolicy)
        {
            var tableNames = new SortedSet<string>(_sqlCommandProcessor
                .GetAllEntityInfos(context)
                .Values
                .Select(x => x.TableName));

            return GetCacheDependencies(cachePolicy, tableNames, command.CommandText);
        }

        /// <summary>
        /// Finds the related table names of the current query.
        /// </summary>
        public SortedSet<string> GetCacheDependencies(EfCachePolicy cachePolicy, SortedSet<string> tableNames, string commandText)
        {
            var textsInsideSquareBrackets = _sqlCommandProcessor.GetSqlCommandTableNames(commandText);
            var cacheDependencies = new SortedSet<string>(tableNames.Intersect(textsInsideSquareBrackets));
            if (cacheDependencies.Any())
            {
                LogProcess(tableNames, textsInsideSquareBrackets, cacheDependencies);
                return cacheDependencies;
            }

            cacheDependencies = cachePolicy.CacheItemDependencies as SortedSet<string>;
            if (cacheDependencies?.Any() != true)
            {
                _logger.LogDebug($"It's not possible to calculate the related table names of the current query[{commandText}]. Please use EfCachePolicy.Configure(options => options.CacheDependencies(\"real_table_name_1\", \"real_table_name_2\")) to specify them explicitly.");
                cacheDependencies = new SortedSet<string> { EfCachePolicy.EfUnknownCacheDependency };
            }

            LogProcess(tableNames, textsInsideSquareBrackets, cacheDependencies);
            return cacheDependencies;
        }

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        public bool InvalidateCacheDependencies(DbCommand command, DbContext context, EfCachePolicy cachePolicy)
        {
            var commandText = command.CommandText;
            if (!_sqlCommandProcessor.IsCrudCommand(commandText))
            {
                return false;
            }

            var cacheDependencies = GetCacheDependencies(command, context, cachePolicy);
            cacheDependencies.Add(EfCachePolicy.EfUnknownCacheDependency);
            _cache.InvalidateCacheDependencies(new EfCacheKey { CacheDependencies = cacheDependencies });

            _logger.LogDebug(CacheableEventId.QueryResultInvalidated, $"Invalidated [{string.Join(", ", cacheDependencies)}] dependencies.");
            return true;
        }

        private void LogProcess(SortedSet<string> tableNames, SortedSet<string> textsInsideSquareBrackets, SortedSet<string> cacheDependencies)
        {
            _logger.LogDebug($"ContextTableNames: {string.Join(", ", tableNames)}, PossibleQueryTableNames: {string.Join(", ", textsInsideSquareBrackets)} -> CacheDependencies: {string.Join(", ", cacheDependencies)}.");
        }
    }
}