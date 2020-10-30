using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// EfCachePolicy Parser Utils
    /// </summary>
    public interface IEfCachePolicyParser
    {
        /// <summary>
        /// Converts the `commandText` to an instance of `EfCachePolicy`
        /// </summary>
        EfCachePolicy GetEfCachePolicy(string commandText, IList<TableEntityInfo> allEntityTypes);

        /// <summary>
        /// Does `commandText` contain EfCachePolicyTagPrefix?
        /// </summary>
        bool HasEfCachePolicy(string commandText);

        /// <summary>
        /// Removes the EFCachePolicy line from the commandText
        /// </summary>
        string RemoveEfCachePolicyTag(string commandText);
    }

    internal class EfCachePolicyParser : IEfCachePolicyParser
    {
        /// <summary>
        /// EFCachePolicy Tag Prefix
        /// </summary>
        public static readonly string EfCachePolicyTagPrefix = $"-- {nameof(EfCachePolicy)}";

        private readonly EfCacheOptions _options;
        private readonly IEfSqlCommandsProcessor _sqlCommandsProcessor;
        private readonly IEfDebugLogger _logger;

        public EfCachePolicyParser(IOptions<EfCacheOptions> options, IEfSqlCommandsProcessor sqlCommandsProcessor, IEfDebugLogger logger)
        {
            _options = options?.Value;
            _sqlCommandsProcessor = sqlCommandsProcessor;
            _logger = logger;
        }

        public bool HasEfCachePolicy(string commandText)
        {
            return !string.IsNullOrWhiteSpace(commandText) && commandText.Contains(EfCachePolicyTagPrefix);
        }

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

        public EfCachePolicy GetEfCachePolicy(string commandText, IList<TableEntityInfo> allEntityTypes)
        {
            var efCachePolicy = GetParsedPolicy(commandText) ?? GetGlobalPolicy(commandText, allEntityTypes);
            if (efCachePolicy != null)
            {
                _logger.LogDebug($"Using EFCachePolicy: {efCachePolicy}.");
            }

            return efCachePolicy;
        }

        private EfCachePolicy GetGlobalPolicy(string commandText, IList<TableEntityInfo> allEntityTypes)
        {
            if (_sqlCommandsProcessor.IsCrudCommand(commandText) || commandText.Contains(EfCachedQueryExtensions.IsNotCachableMarker))
            {
                return null;
            }

            var shouldBeCached = false;

            var queryEntityTypes = _sqlCommandsProcessor.GetSqlCommandEntityTypes(commandText, allEntityTypes);
            if (queryEntityTypes.Any(entityType => _options.CacheableEntities.ContainsKey(entityType)))
            {
                shouldBeCached = true;
            }

            return shouldBeCached
                ? new EfCachePolicy().Timeout(_options.Timeout)
                : null;
        }

        private EfCachePolicy GetParsedPolicy(string commandText)
        {
            if (!HasEfCachePolicy(commandText))
            {
                return null;
            }

            var commandTextLines = commandText.Split('\n');
            var efCachePolicyCommentLine = commandTextLines.First(textLine => textLine.StartsWith(EfCachePolicyTagPrefix)).Trim();

            var parts = efCachePolicyCommentLine.Split(new[] { EfCachePolicy.PartsSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return null;
            }

            var options = parts[1].Split(new[] { EfCachePolicy.ItemsSeparator }, StringSplitOptions.None);
            if (options.Length < 2)
            {
                return null;
            }

            if (!TimeSpan.TryParse(options[0], out var timeout))
            {
                return null;
            }

            var cacheDependencies = options.Length >= 2 ? options[1].Split(new[] { EfCachePolicy.CacheDependenciesSeparator }, StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();

            return new EfCachePolicy().Timeout(timeout).CacheDependencies(cacheDependencies);
        }
    }
}