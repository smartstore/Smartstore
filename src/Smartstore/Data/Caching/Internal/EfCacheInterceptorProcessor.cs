using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Smartstore.Data.Caching.Internal
{
    public class EfCacheInterceptorProcessor
    {
        private readonly DbCache _cache;
        private readonly EfCacheDependenciesProcessor _cacheDependenciesProcessor;
        private readonly EfCacheKeyGenerator _cacheKeyGenerator;
        private readonly EfCachePolicyResolver _policyResolver;
        private readonly IEfDebugLogger _logger;
        private readonly EfSqlCommandProcessor _sqlCommandProcessor;

        public EfCacheInterceptorProcessor(
            IEfDebugLogger logger,
            DbCache cache,
            EfCacheDependenciesProcessor cacheDependenciesProcessor,
            EfCacheKeyGenerator cacheKeyProvider,
            EfCachePolicyResolver policyResolver,
            EfSqlCommandProcessor sqlCommandProcessor)
        {
            _logger = logger;
            _cache = cache;
            _cacheDependenciesProcessor = cacheDependenciesProcessor;
            _cacheKeyGenerator = cacheKeyProvider;
            _policyResolver = policyResolver;
            _sqlCommandProcessor = sqlCommandProcessor;
        }

        /// <summary>
        /// Adds command's data to the cache.
        /// </summary>
        public T ProcessExecutingCommands<T>(DbCommand command, DbContext context, T result)
        {
            var cachePolicy = _policyResolver.GetEfCachePolicy(command.CommandText, context);
            if (cachePolicy == null)
            {
                return result;
            }

            var efCacheKey = _cacheKeyGenerator.GenerateCacheKey(command, context, cachePolicy);

            if (_cache.Get(efCacheKey, cachePolicy) is not EFCacheData cacheResult)
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
        public T ProcessExecutedCommands<T>(DbCommand command, DbContext context, T result)
        {
            if (!BeginProcessExecutedCommand(command, context, result, out var cacheKey, out var policy))
            {
                return result;
            }

            if (result is int data)
            {
                _cache.Put(cacheKey, new EFCacheData { NonQuery = data }, policy);
                _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{data}] added to the cache[{cacheKey}].");
                return result;
            }

            if (result is DbDataReader dataReader)
            {
                DbTableRows tableRows;
                using (var dbReaderLoader = new DbDataReaderLoader(dataReader))
                {
                    tableRows = dbReaderLoader.LoadAndClose();
                }

                var maxRows = policy.MaxRows;
                if (maxRows > 0 && tableRows.RowCount > maxRows)
                {
                    _logger.LogDebug($"Will not cache: Max row limit of {maxRows} exceeded. Row count: {tableRows.RowCount}");
                }
                else
                {
                    _cache.Put(cacheKey, new EFCacheData { TableRows = tableRows }, policy);
                    _logger.LogDebug(CacheableEventId.QueryResultCached, $"TableRows[{tableRows.TableName}] added to the cache[{policy}].");
                }

                return (T)(object)new DbTableRowsDataReader(tableRows);
            }

            if (result is object)
            {
                _cache.Put(cacheKey, new EFCacheData { Scalar = result }, policy);
                _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{result}] added to the cache[{cacheKey}].");
                return result;
            }

            return result;
        }

        /// <summary>
        /// Reads data from cache or caches it and then returns the result.
        /// </summary>
        public async Task<T> ProcessExecutedCommandsAsync<T>(DbCommand command, DbContext context, T result)
        {
            if (!BeginProcessExecutedCommand(command, context, result, out var cacheKey, out var policy))
            {
                return result;
            }

            if (result is int data)
            {
                await _cache.PutAsync(cacheKey, new EFCacheData { NonQuery = data }, policy);
                _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{data}] added to the cache[{cacheKey}].");
                return result;
            }

            if (result is DbDataReader dataReader)
            {
                DbTableRows tableRows;
                using (var dbReaderLoader = new DbDataReaderLoader(dataReader))
                {
                    tableRows = await dbReaderLoader.LoadAndCloseAsync();
                }

                var maxRows = policy.MaxRows;
                if (maxRows > 0 && tableRows.RowCount > maxRows)
                {
                    _logger.LogDebug($"Will not cache: Max row limit of {maxRows} exceeded. Row count: {tableRows.RowCount}");
                }
                else
                {
                    await _cache.PutAsync(cacheKey, new EFCacheData { TableRows = tableRows }, policy);
                    _logger.LogDebug(CacheableEventId.QueryResultCached, $"TableRows[{tableRows.TableName}] added to the cache[{policy}].");
                }

                return (T)(object)new DbTableRowsDataReader(tableRows);
            }

            if (result is object)
            {
                await _cache.PutAsync(cacheKey, new EFCacheData { Scalar = result }, policy);
                _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{result}] added to the cache[{cacheKey}].");
                return result;
            }

            return result;
        }

        private bool BeginProcessExecutedCommand<T>(DbCommand command, DbContext context, T result, out EfCacheKey key, out EfCachePolicy policy)
        {
            key = null;
            policy = null;

            if (result is DbTableRowsDataReader rowsReader)
            {
                _logger.LogDebug(CacheableEventId.CacheHit, $"Returning the cached TableRows[{rowsReader.TableName}].");
                return false;
            }

            if (_cacheDependenciesProcessor.InvalidateCacheDependencies(command, context, new EfCachePolicy()))
            {
                return false;
            }

            var allEntityInfos = _sqlCommandProcessor.GetAllEntityInfos(context);
            policy = _policyResolver.GetEfCachePolicy(command.CommandText, context);
            if (policy == null)
            {
                return false;
            }

            key = _cacheKeyGenerator.GenerateCacheKey(command, context, policy);
            return key != null;
        }
    }
}