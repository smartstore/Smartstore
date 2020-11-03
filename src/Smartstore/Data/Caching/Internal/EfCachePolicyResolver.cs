using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore.Data.Caching.Internal
{
    /// <summary>
    /// EfCachePolicy Parser Utils
    /// </summary>
    public sealed class EfCachePolicyResolver
    {
        /// <summary>
        /// EFCachePolicy Tag Prefix
        /// </summary>
        public static readonly string EfCachePolicyTagPrefix = $"-- {nameof(EfCachePolicy)}";

        private readonly EfCacheOptions _options;
        private readonly EfSqlCommandProcessor _sqlCommandsProcessor;
        private readonly IEfDebugLogger _logger;

        public EfCachePolicyResolver(
            IOptions<EfCacheOptions> options, 
            EfSqlCommandProcessor sqlCommandProcessor, 
            IEfDebugLogger logger)
        {
            _options = options?.Value;
            _sqlCommandsProcessor = sqlCommandProcessor;
            _logger = logger;
        }

        /// <summary>
        /// Does `commandText` contain EfCachePolicyTagPrefix?
        /// </summary>
        public bool HasEfCachePolicy(string commandText)
        {
            return !string.IsNullOrWhiteSpace(commandText) && commandText.Contains(EfCachePolicyTagPrefix);
        }

        /// <summary>
        /// Removes the EFCachePolicy line from the commandText
        /// </summary>
        public string RemoveEfCachePolicyTag(string commandText)
        {
            var startIndex = commandText.IndexOf(EfCachePolicyTagPrefix, StringComparison.Ordinal);
            if (startIndex == -1)
            {
                return commandText;
            }

            var endIndex = commandText.IndexOf('\n', startIndex);
            if (endIndex == -1)
            {
                return commandText;
            }

            var additionalNewlineIndex = commandText.IndexOf('\n', endIndex + 1) - endIndex;
            if (additionalNewlineIndex == 1 || additionalNewlineIndex == 2)
            {
                // EF's TagWith(..) method inserts an additional line break between
                // comments which we can remove as well
                endIndex += additionalNewlineIndex;
            }

            return commandText.Remove(startIndex, (endIndex - startIndex) + 1);
        }

        /// <summary>
        /// Resolves a cache policy either from query or from global annotation store.
        /// </summary>
        public EfCachePolicy GetEfCachePolicy(string commandText, DbContext context)
        {
            return GetEfCachePolicy(commandText, _sqlCommandsProcessor.GetAllEntityInfos(context));
        }

        /// <summary>
        /// Resolves a cache policy either from query or from global annotation store.
        /// </summary>
        public EfCachePolicy GetEfCachePolicy(string commandText, Dictionary<object, TableEntityInfo> allEntityTypes)
        {
            if (_sqlCommandsProcessor.IsCrudCommand(commandText) || commandText.Contains(EfCachedQueryExtensions.IsNotCachableMarker))
            {
                return null;
            }

            var globalPolicy = GetGlobalPolicy(commandText, allEntityTypes, out var containsToxicEntity);
            if (containsToxicEntity)
            {
                _logger.LogDebug($"Will not cache: command contains at least one toxic entity.");
                return null;
            }

            var queryPolicy = GetQueryPolicy(commandText, globalPolicy);
            if (queryPolicy != null)
            {
                _logger.LogDebug($"Using EFCachePolicy: {queryPolicy}.");
            }

            return queryPolicy;
        }

        /// <summary>
        /// Tries to resolve a global entity cache policy.
        /// </summary>
        private EfCachePolicy GetGlobalPolicy(string commandText, Dictionary<object, TableEntityInfo> allEntityTypes, out bool containsToxicEntity)
        {
            containsToxicEntity = false;
            var queryEntityInfos = _sqlCommandsProcessor.GetSqlCommandEntityInfos(commandText, allEntityTypes);

            var policies = queryEntityInfos
                .Select(x => x.Policy)
                .Where(policy => policy != null)
                .ToArray();

            if (policies.Any(x => x.NeverCache))
            {
                // Never cache a query if one of the dependencies is marked as uncacheable (toxic)
                containsToxicEntity = true;
                return null;
            }

            var policy = policies.FirstOrDefault();
            if (policy == null)
            {
                return null;
            }

            return new EfCachePolicy(policy);
        }

        /// <summary>
        /// Converts the `commandText` to an instance of `EfCachePolicy`
        /// </summary>
        private EfCachePolicy GetQueryPolicy(string commandText, EfCachePolicy globalPolicy)
        {
            var queryPolicy = new EfCachePolicy
            {
                ExpirationTimeout = globalPolicy?.ExpirationTimeout.NullDefault() ?? _options.DefaultExpirationTimeout,
                MaxRows = globalPolicy?.MaxRows.NullDefault() ?? _options.DefaultMaxRows,
                RequestCacheEnabled = globalPolicy?.RequestCacheEnabled.NullDefault() ?? false
            };

            if (!HasEfCachePolicy(commandText))
            {
                return queryPolicy;
            }

            var commandTextLines = commandText.Split('\n');
            var efCachePolicyCommentLine = commandTextLines.First(textLine => textLine.StartsWith(EfCachePolicyTagPrefix)).Trim();

            var parts = efCachePolicyCommentLine.Split(new[] { EfCachePolicy.PartsSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return queryPolicy;
            }

            var options = parts[1].Split(new[] { EfCachePolicy.ItemsSeparator }, StringSplitOptions.None);
            if (options.Length < 2)
            {
                return queryPolicy;
            }

            if (TimeSpan.TryParse(options[0], out var timeout))
            {
                queryPolicy.ExpirationTimeout = timeout;
            }

            if (int.TryParse(options[1], out var maxRows))
            {
                queryPolicy.MaxRows = maxRows;
            }

            if (options.Length >= 3 && bool.TryParse(options[2], out var requestCacheEnabled))
            {
                queryPolicy.RequestCacheEnabled = requestCacheEnabled;
            }

            var cacheDependencies = options.Length >= 4 
                ? options[3].Split(new[] { EfCachePolicy.CacheDependenciesSeparator }, StringSplitOptions.RemoveEmptyEntries) 
                : Array.Empty<string>();

            if (cacheDependencies.Length > 0)
            {
                queryPolicy.WithDependencies(cacheDependencies);
            }

            return queryPolicy;
        }
    }
}