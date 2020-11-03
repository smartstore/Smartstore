using System;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Smartstore.Data.Caching.Internal;
using Smartstore.Utilities;

namespace Smartstore.Data.Caching.Internal
{
    /// <summary>
    /// A custom cache key generator for EF queries.
    /// </summary>
    public sealed class EfCacheKeyGenerator
    {
        private readonly EfCacheDependenciesProcessor _cacheDependenciesProcessor;
        private readonly IEfDebugLogger _logger;
        private readonly EfCachePolicyResolver _policyResolver;

        /// <summary>
        /// A custom cache key provider for EF queries.
        /// </summary>
        public EfCacheKeyGenerator(
            EfCacheDependenciesProcessor cacheDependenciesProcessor,
            EfCachePolicyResolver policyResolver,
            IEfDebugLogger logger)
        {
            _cacheDependenciesProcessor = cacheDependenciesProcessor;
            _logger = logger;
            _policyResolver = policyResolver;
        }

        /// <summary>
        /// Gets an EF query and returns its hashed key to store in the cache.
        /// </summary>
        /// <param name="command">The EF query.</param>
        /// <param name="context">DbContext is a combination of the Unit Of Work and Repository patterns.</param>
        /// <param name="cachePolicy">determines the Expiration time of the cache.</param>
        /// <returns>Information of the computed key of the input LINQ query.</returns>
        public EfCacheKey GenerateCacheKey(DbCommand command, DbContext context, EfCachePolicy cachePolicy)
        {
            var cacheKey = GenerateCacheKey(command);
            var cacheKeyHash = $"{XxHashUnsafe.ComputeHash(cacheKey):X}";
            var cacheDependencies = _cacheDependenciesProcessor.GetCacheDependencies(command, context, cachePolicy);

            _logger.LogDebug($"KeyHash: {cacheKeyHash}, CacheDependencies: {string.Join(", ", cacheDependencies)}.");

            return new EfCacheKey
            {
                Key = cacheKey,
                KeyHash = cacheKeyHash,
                CacheDependencies = cacheDependencies
            };
        }

        private string GenerateCacheKey(DbCommand command)
        {
            using var psb = StringBuilderPool.Instance.Get(out var sb);
            sb.AppendLine(_policyResolver.RemoveEfCachePolicyTag(command.CommandText));

            foreach (DbParameter parameter in command.Parameters)
            {
                sb.Append(parameter.ParameterName)
                    .Append('=').Append(GetParameterValue(parameter)).Append(',')
                    .Append("Size").Append('=').Append(parameter.Size).Append(',')
                    .Append("Precision").Append('=').Append(parameter.Precision).Append(',')
                    .Append("Scale").Append('=').Append(parameter.Scale).Append(',')
                    .Append("Direction").Append('=').Append(parameter.Direction).Append(',');
            }

            return sb.ToString().Trim();
        }

        private static string GetParameterValue(DbParameter parameter)
        {
            if (parameter.Value is DBNull || parameter.Value is null)
            {
                return "null";
            }

            if (parameter.Value is byte[] buffer)
            {
                return BytesToHex(buffer);
            }

            return parameter.Value.ToString();
        }

        private static string BytesToHex(byte[] buffer)
        {
            var sb = new StringBuilder(buffer.Length * 2);
            foreach (var @byte in buffer)
            {
                sb.Append(@byte.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}