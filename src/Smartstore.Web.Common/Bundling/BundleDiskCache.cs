using Microsoft.Extensions.Options;
using Smartstore.IO;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Web.Bundling
{
    public interface IBundleDiskCache : IBundleCache
    {
    }

    public class BundleDiskCache : IBundleDiskCache
    {
        const string DirName = "BundleCache";

        private readonly IApplicationContext _appContext;
        private readonly IFileSystem _fs;
        private readonly IOptionsMonitor<BundlingOptions> _options;

        public BundleDiskCache(
            IApplicationContext appContext,
            IOptionsMonitor<BundlingOptions> options)
        {
            _appContext = appContext;
            _fs = _appContext.TenantRoot;
            _options = options;

            _appContext.TenantRoot.TryCreateDirectory(DirName);
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<BundleResponse> GetResponseAsync(BundleCacheKey cacheKey, Bundle bundle)
        {
            if (_options.CurrentValue.EnableDiskCache == false)
            {
                return null;
            }

            Guard.NotNull(cacheKey.Key, nameof(cacheKey.Key));
            Guard.NotNull(bundle, nameof(bundle));

            var dir = GetCacheDirectory(cacheKey);

            if (dir.Exists)
            {
                try
                {
                    var deps = await ReadFile(dir, "bundle.dependencies");
                    var hash = await ReadFile(dir, "bundle.hash");
                    var pcodes = (await ParseFileContent(await ReadFile(dir, "bundle.pcodes"))).ToArray();

                    var (valid, parsedDeps, currentHash) = await TryValidate(bundle, deps, hash, pcodes);

                    if (!valid)
                    {
                        Logger.Debug("Invalidating cached bundle for '{0}' because it is not valid anymore.", bundle.Route);
                        InvalidateAssetInternal(dir);
                        return null;
                    }

                    var content = await ReadFile(dir, ResolveBundleContentFileName(bundle));
                    if (content == null)
                    {
                        using (await AsyncLock.KeyedAsync(BuildLockKey(dir.Name)))
                        {
                            InvalidateAssetInternal(dir);
                            return null;
                        }
                    }

                    var response = new BundleResponse
                    {
                        Route = bundle.Route,
                        CreationDate = dir.LastModified,
                        Content = content,
                        ContentType = bundle.ContentType,
                        ProcessorCodes = pcodes,
                        IncludedFiles = parsedDeps
                    };

                    Logger.Debug("Succesfully read bundle '{0}' from disk cache.", bundle.Route);

                    return response;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error while resolving bundle '{0}' from the disk cache.", bundle.Route);
                }
            }

            return null;
        }

        public async Task PutResponseAsync(BundleCacheKey cacheKey, Bundle bundle, BundleResponse response)
        {
            if (_options.CurrentValue.EnableDiskCache == false)
            {
                return;
            }

            Guard.NotNull(cacheKey.Key, nameof(cacheKey.Key));
            Guard.NotNull(bundle, nameof(bundle));
            Guard.NotNull(response, nameof(response));

            var dir = GetCacheDirectory(cacheKey);

            using (await AsyncLock.KeyedAsync(BuildLockKey(dir.Name)))
            {
                dir.Create();

                try
                {
                    // Save main content file
                    await CreateFileFromEntries(dir, ResolveBundleContentFileName(bundle), new[] { response.Content });

                    // Save dependencies file
                    var deps = response.IncludedFiles;
                    await CreateFileFromEntries(dir, "bundle.dependencies", deps);

                    // Save hash file
                    var currentHash = await GetFileHash(bundle, deps);
                    await CreateFileFromEntries(dir, "bundle.hash", new[] { currentHash });

                    // Save codes file
                    await CreateFileFromEntries(dir, "bundle.pcodes", response.ProcessorCodes);

                    Logger.Debug("Succesfully inserted bundle '{0}' to disk cache.", bundle.Route);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error while inserting bundle '{0}' to disk cache.", bundle.Route);
                    InvalidateAssetInternal(dir);
                }
            }
        }

        public Task RemoveResponseAsync(BundleCacheKey cacheKey)
        {
            if (cacheKey.Key.HasValue())
            {
                InvalidateAssetInternal(GetCacheDirectory(cacheKey));
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _fs.TryDeleteDirectory(DirName);
            return Task.CompletedTask;
        }

        #region Utils

        /// <summary>
        /// Checks whether bundle is up-to-date.
        /// </summary>
        internal async Task<(bool valid, IEnumerable<string> parsedDeps, string currentHash)> TryValidate(Bundle bundle, string lastDeps, string lastHash, string[] pcodes)
        {
            bool valid = false;
            IEnumerable<string> parsedDeps = null;
            string currentHash = null;

            try
            {
                if (lastDeps.HasValue() && lastHash.HasValue())
                {
                    // First check if pcodes match, this one is faster than file hash check.
                    var enableMinification = _options.CurrentValue.EnableMinification == true;
                    var enableAutoprefixer = bundle.ContentType == "text/css" ? _options.CurrentValue.EnableAutoprefixer == true : false;
                    var isMinified = pcodes.Contains(BundleProcessorCodes.Minify);
                    var isAutoprefixed = pcodes.Contains(BundleProcessorCodes.Autoprefix);

                    valid = isMinified == enableMinification && isAutoprefixed == enableAutoprefixer;

                    if (valid)
                    {
                        // Check file hash only if pcodes did match
                        parsedDeps = await ParseFileContent(lastDeps);

                        // Check if dependency files hash matches the last saved hash
                        currentHash = await GetFileHash(bundle, parsedDeps);

                        valid = lastHash == currentHash;
                    }
                }
            }
            catch
            {
                valid = false;
            }

            return (valid, parsedDeps, currentHash);
        }

        private async Task<string> GetFileHash(Bundle bundle, IEnumerable<string> files)
        {
            var fileProvider = bundle.FileProvider ?? _options.CurrentValue.FileProvider;
            var combiner = HashCodeCombiner.Start();

            foreach (var file in files)
            {
                var fileInfo = fileProvider.GetFileInfo(file);
                if (fileInfo is IFileHashProvider hashProvider)
                {
                    combiner.Add(await hashProvider.GetFileHashAsync());
                }
                else
                {
                    combiner.Add(fileInfo);
                }
            }

            return combiner.CombinedHashString;
        }

        private bool InvalidateAssetInternal(IDirectory dir)
            => _fs.TryDeleteDirectory(dir.SubPath);

        private IDirectory GetCacheDirectory(BundleCacheKey cacheKey)
            => _fs.GetDirectory(PathUtility.Join(DirName.AsSpan(), ResolveBundleDirectoryName(cacheKey).AsSpan()));

        private static string ResolveBundleDirectoryName(BundleCacheKey cacheKey)
            => PathUtility.SanitizeFileName(cacheKey.Key.Trim(PathUtility.PathSeparators));

        private static string ResolveBundleContentFileName(Bundle bundle)
            => $"bundle.{MimeTypes.MapMimeTypeToExtension(bundle.ContentType)}";

        private static string BuildLockKey(string dirName)
            => "BundleCache.Dir." + dirName;

        private Task<string> ReadFile(IDirectory dir, string fileName)
            => _fs.ReadAllTextAsync(PathUtility.Join(dir.SubPath, fileName));

        private static async Task<IEnumerable<string>> ParseFileContent(string content)
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (content.IsEmpty()) return list;

            using var sr = new StringReader(content);
            while (true)
            {
                var f = await sr.ReadLineAsync();
                if (f != null && f.HasValue())
                {
                    list.Add(f);
                }
                else
                {
                    break;
                }
            }

            return list;
        }

        private async Task CreateFileFromEntries(IDirectory dir, string fileName, IEnumerable<string> entries)
        {
            if (entries == null || !entries.Any())
                return;

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            foreach (var f in entries)
            {
                sb.AppendLine(f);
            }

            var content = sb.ToString().TrimEnd();
            if (content.HasValue())
            {
                await _fs.WriteAllTextAsync(PathUtility.Join(dir.SubPath, fileName), content);
            }
        }

        #endregion
    }
}
