using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Data.Caching.Internal;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Cache dependencies processor
    /// </summary>
    public interface IEfCacheDependenciesProcessor
    {
        /// <summary>
        /// Finds the related table names of the current query.
        /// </summary>
        SortedSet<string> GetCacheDependencies(DbCommand command, DbContext context, EfCachePolicy cachePolicy);

        /// <summary>
        /// Finds the related table names of the current query.
        /// </summary>
        SortedSet<string> GetCacheDependencies(EfCachePolicy cachePolicy, SortedSet<string> tableNames, string commandText);

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        bool InvalidateCacheDependencies(DbCommand command, DbContext context, EfCachePolicy cachePolicy);
    }

    internal class EfCacheDependenciesProcessor : IEfCacheDependenciesProcessor
    {
        private readonly IEfDebugLogger _logger;
        private readonly DbCache _cache;
        private readonly IEfSqlCommandsProcessor _sqlCommandsProcessor;

        public EfCacheDependenciesProcessor(IEfDebugLogger logger, DbCache cache, IEfSqlCommandsProcessor sqlCommandsProcessor)
        {
            _logger = logger;
            _cache = cache;
            _sqlCommandsProcessor = sqlCommandsProcessor;
        }

        public SortedSet<string> GetCacheDependencies(DbCommand command, DbContext context, EfCachePolicy cachePolicy)
        {
            var tableNames = new SortedSet<string>(
                    _sqlCommandsProcessor.GetAllEntityInfos(context).Select(x => x.TableName));
            return GetCacheDependencies(cachePolicy, tableNames, command.CommandText);
        }

        public SortedSet<string> GetCacheDependencies(EfCachePolicy cachePolicy, SortedSet<string> tableNames, string commandText)
        {
            var textsInsideSquareBrackets = _sqlCommandsProcessor.GetSqlCommandTableNames(commandText);
            var cacheDependencies = new SortedSet<string>(tableNames.Intersect(textsInsideSquareBrackets));
            if (cacheDependencies.Any())
            {
                LogProcess(tableNames, textsInsideSquareBrackets, cacheDependencies);
                return cacheDependencies;
            }

            cacheDependencies = cachePolicy.CacheItemsDependencies as SortedSet<string>;
            if (cacheDependencies?.Any() != true)
            {
                _logger.LogDebug($"It's not possible to calculate the related table names of the current query[{commandText}]. Please use EfCachePolicy.Configure(options => options.CacheDependencies(\"real_table_name_1\", \"real_table_name_2\")) to specify them explicitly.");
                cacheDependencies = new SortedSet<string> { EfCachePolicy.EfUnknownCacheDependency };
            }
            LogProcess(tableNames, textsInsideSquareBrackets, cacheDependencies);
            return cacheDependencies;
        }

        private void LogProcess(SortedSet<string> tableNames, SortedSet<string> textsInsideSquareBrackets, SortedSet<string> cacheDependencies)
        {
            _logger.LogDebug($"ContextTableNames: {string.Join(", ", tableNames)}, PossibleQueryTableNames: {string.Join(", ", textsInsideSquareBrackets)} -> CacheDependencies: {string.Join(", ", cacheDependencies)}.");
        }

        public bool InvalidateCacheDependencies(DbCommand command, DbContext context, EfCachePolicy cachePolicy)
        {
            var commandText = command.CommandText;
            if (!_sqlCommandsProcessor.IsCrudCommand(commandText))
            {
                return false;
            }

            var cacheDependencies = GetCacheDependencies(command, context, cachePolicy);
            cacheDependencies.Add(EfCachePolicy.EfUnknownCacheDependency);
            _cache.InvalidateCacheDependencies(new EfCacheKey { CacheDependencies = cacheDependencies });

            _logger.LogDebug(CacheableEventId.QueryResultInvalidated, $"Invalidated [{string.Join(", ", cacheDependencies)}] dependencies.");
            return true;
        }
    }
}