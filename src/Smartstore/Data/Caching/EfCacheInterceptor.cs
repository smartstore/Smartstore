using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Smartstore.Data.Caching.Internal;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Entity Framework Core Second Level Caching Library
    /// </summary>
    public class EfCacheInterceptor : DbCommandInterceptor
    {
        private readonly DbCache _cache;
        private readonly IEfCacheDependenciesProcessor _cacheDependenciesProcessor;
        private readonly IEfCacheKeyProvider _cacheKeyProvider;
        private readonly IEfCachePolicyParser _cachePolicyParser;
        private readonly IEfDebugLogger _logger;
        private readonly IEfSqlCommandsProcessor _sqlCommandsProcessor;

        /// <summary>
        /// Entity Framework Core Second Level Caching Library
        /// Please use
        /// services.AddDbContextPool&lt;ApplicationDbContext&gt;((serviceProvider, optionsBuilder) =&gt;
        ///                   optionsBuilder.UseSqlServer(...).AddInterceptors(serviceProvider.GetRequiredService&lt;EfCacheInterceptor&gt;()));
        /// to register it.
        /// </summary>
        public EfCacheInterceptor(
            IEfDebugLogger logger,
            DbCache cache,
            IEfCacheDependenciesProcessor cacheDependenciesProcessor,
            IEfCacheKeyProvider cacheKeyProvider,
            IEfCachePolicyParser cachePolicyParser,
            IEfSqlCommandsProcessor sqlCommandsProcessor)
        {
            _logger = logger;
            _cache = cache;
            _cacheDependenciesProcessor = cacheDependenciesProcessor;
            _cacheKeyProvider = cacheKeyProvider;
            _cachePolicyParser = cachePolicyParser;
            _sqlCommandsProcessor = sqlCommandsProcessor;
        }

        #region DbCommandInterceptor

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteNonQuery
        /// </summary>
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecuted(command, eventData, result);
            }

            return ProcessExecutedCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteNonQueryAsync.
        /// </summary>
        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<int>(ProcessExecutedCommands(command, eventData.Context, result));
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteNonQuery.
        /// </summary>
        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecuting(command, eventData, result);
            }

            return ProcessExecutingCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteNonQueryAsync.
        /// </summary>
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<InterceptionResult<int>>(ProcessExecutingCommands(command, eventData.Context, result));
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteReader.
        /// </summary>
        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecuted(command, eventData, result);
            }

            return ProcessExecutedCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteReaderAsync.
        /// </summary>
        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<DbDataReader>(ProcessExecutedCommands(command, eventData.Context, result));
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteReader.
        /// </summary>
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecuting(command, eventData, result);
            }

            return ProcessExecutingCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteReaderAsync.
        /// </summary>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<InterceptionResult<DbDataReader>>(ProcessExecutingCommands(command, eventData.Context, result));
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteScalar.
        /// </summary>
        public override object ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecuted(command, eventData, result);
            }

            return ProcessExecutedCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called immediately after EF calls System.Data.Common.DbCommand.ExecuteScalarAsync.
        /// </summary>
        public override ValueTask<object> ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<object>(ProcessExecutedCommands(command, eventData.Context, result));
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteScalar.
        /// </summary>
        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecuting(command, eventData, result);
            }

            return ProcessExecutingCommands(command, eventData.Context, result);
        }

        /// <summary>
        /// Called just before EF intends to call System.Data.Common.DbCommand.ExecuteScalarAsync.
        /// </summary>
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context == null)
            {
                return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
            }

            return new ValueTask<InterceptionResult<object>>(ProcessExecutingCommands(command, eventData.Context, result));
        }

        #endregion

        #region Processor

        /// <summary>
        /// Adds command's data to the cache.
        /// </summary>
        protected virtual T ProcessExecutingCommands<T>(DbCommand command, DbContext context, T result)
        {
            var allEntityTypes = _sqlCommandsProcessor.GetAllEntityInfos(context);
            var cachePolicy = _cachePolicyParser.GetEfCachePolicy(command.CommandText, allEntityTypes);
            if (cachePolicy == null)
            {
                return result;
            }

            var efCacheKey = _cacheKeyProvider.GetCacheKey(command, context, cachePolicy);
            if (_cache.Get(efCacheKey, cachePolicy) is not EfCachedData cacheResult)
            {
                _logger.LogDebug($"[{efCacheKey}] was not present in the cache.");
                return result;
            }

            if (result is InterceptionResult<DbDataReader>)
            {
                if (cacheResult.IsNull)
                {
                    _logger.LogDebug("Suppressed the result with an empty TableRows.");
                    return (T)Convert.ChangeType(InterceptionResult<DbDataReader>.SuppressWithResult(new DbTableRowsDataReader(new DbTableRows())), typeof(T));
                }

                _logger.LogDebug($"Suppressed the result with the TableRows[{cacheResult.TableRows.TableName}] from the cache[{efCacheKey}].");
                return (T)Convert.ChangeType(InterceptionResult<DbDataReader>.SuppressWithResult(new DbTableRowsDataReader(cacheResult.TableRows)), typeof(T));
            }

            if (result is InterceptionResult<int>)
            {
                int cachedResult = cacheResult.IsNull ? default : cacheResult.NonQuery;
                _logger.LogDebug($"Suppressed the result with {cachedResult} from the cache[{efCacheKey}].");
                return (T)Convert.ChangeType(InterceptionResult<int>.SuppressWithResult(cachedResult), typeof(T));
            }

            if (result is InterceptionResult<object>)
            {
                object cachedResult = cacheResult.IsNull ? default : cacheResult.Scalar;
                _logger.LogDebug($"Suppressed the result with {cachedResult} from the cache[{efCacheKey}].");
                return (T)Convert.ChangeType(InterceptionResult<object>.SuppressWithResult(cachedResult), typeof(T));
            }

            _logger.LogDebug($"Skipped the result with {result?.GetType()} type.");

            return result;
        }

        /// <summary>
        /// Reads data from cache or caches it and then returns the result.
        /// </summary>
        protected virtual T ProcessExecutedCommands<T>(DbCommand command, DbContext context, T result)
        {
            if (result is DbTableRowsDataReader rowsReader)
            {
                _logger.LogDebug(CacheableEventId.CacheHit, $"Returning the cached TableRows[{rowsReader.TableName}].");
                return result;
            }

            if (_cacheDependenciesProcessor.InvalidateCacheDependencies(command, context, new EfCachePolicy()))
            {
                return result;
            }

            var allEntityInfos = _sqlCommandsProcessor.GetAllEntityInfos(context);
            var cachePolicy = _cachePolicyParser.GetEfCachePolicy(command.CommandText, allEntityInfos);
            if (cachePolicy == null)
            {
                return result;
            }

            var efCacheKey = _cacheKeyProvider.GetCacheKey(command, context, cachePolicy);

            if (result is int data)
            {
                _cache.Put(efCacheKey, new EfCachedData { NonQuery = data }, cachePolicy);
                _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{data}] added to the cache[{efCacheKey}].");
                return result;
            }

            if (result is DbDataReader dataReader)
            {
                DbTableRows tableRows;
                using (var dbReaderLoader = new DbDataReaderLoader(dataReader))
                {
                    tableRows = dbReaderLoader.LoadAndClose();
                }

                _cache.Put(efCacheKey, new EfCachedData { TableRows = tableRows }, cachePolicy);
                _logger.LogDebug(CacheableEventId.QueryResultCached, $"TableRows[{tableRows.TableName}] added to the cache[{efCacheKey}].");
                return (T)(object)new DbTableRowsDataReader(tableRows);
            }

            if (result is object)
            {
                _cache.Put(efCacheKey, new EfCachedData { Scalar = result }, cachePolicy);
                _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{result}] added to the cache[{efCacheKey}].");
                return result;
            }

            return result;
        }

        #endregion
    }
}
