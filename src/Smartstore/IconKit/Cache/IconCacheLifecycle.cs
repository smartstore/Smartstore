using Smartstore.IconKit.Sources;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;
using Smartstore.Engine;
using Smartstore.Json;
using Smartstore.Threading;

namespace Smartstore.IconKit.Cache;

public sealed class IconCacheLifecycle : IIconCacheLifecycle, IAsyncDisposable
{
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);
    private readonly AsyncLock _gate = new();
    private readonly Lock _watchLock = new();
    private readonly List<(string Filter, IChangeToken Token, IconChange Change)> _watches = [];
    private SourceChangeToken _sourceToken;
    private IconCacheIndex _index;
    private readonly IFileSystem _cache;
    private readonly IIconCacheRecovery _recovery;
    private readonly IDisposable _ownedCacheRoot;
    private IChangeToken _validatedSources;
    private DateTimeOffset _lastScan;
    private Task _cleanup = Task.CompletedTask;
    private bool _disposed;

    public IconCacheLifecycle(
        IApplicationContext applicationContext,
        IOptions<IconKitOptions> options,
        IIconCacheRecovery recovery)
    {
        _recovery = recovery;

        var cachePath = options.Value.CachePath;
        var cacheRoot = applicationContext.ContentRoot;
        if (!PathUtility.PathIsWithinDirectory(cachePath, cacheRoot.Root, out _))
        {
            // External cache locations can be missing initially; expand from their existing volume.
            cacheRoot = new LocalFileSystem(Path.GetPathRoot(cachePath));
            _ownedCacheRoot = (IDisposable)cacheRoot;
        }

        _cache = new ExpandedFileSystem(Path.GetRelativePath(cacheRoot.Root, cachePath), cacheRoot);
    }

    internal Task PendingCleanup => _cleanup;

    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <inheritdoc />
    public IReadOnlyList<IconChange> CollectChanges()
    {
        var changes = new List<IconChange>();
        lock (_watchLock)
        {
            // Evaluate each token once. A second HasChanged pass could discard a notification
            // that arrived after collecting the changes, leaving that variant's source index stale.
            for (var i = _watches.Count - 1; i >= 0; i--)
            {
                if (_watches[i].Token.HasChanged)
                {
                    changes.Add(_watches[i].Change);
                    _watches.RemoveAt(i);
                }
            }
        }

        return changes;
    }

    /// <inheritdoc />
    public IChangeToken StartWatchingSources(IFileSystem sources)
    {
        Guard.NotNull(sources);

        lock (_watchLock)
        {
            var previous = _watches.ToDictionary(x => x.Filter, StringComparer.Ordinal);
            _watches.Clear();

            Watch("config.json", IconChangeKind.GlobalConfiguration);
            Watch("kits.json", IconChangeKind.GlobalConfiguration);
            Watch("collections.json", IconChangeKind.GlobalConfiguration);

            // Includes newly installed libraries and changes to variant declarations.
            Watch("*/library.json", IconChangeKind.LibraryManifest);

            foreach (var library in sources.GetDirectoryContents(string.Empty).Where(x => x.IsDirectory))
            {
                Watch(library.Name + "/mapping.json", IconChangeKind.Mapping, library.Name);
                Watch(library.Name + "/metadata.json", IconChangeKind.SearchMetadata, library.Name);
                var variants = new HashSet<string>(StringComparer.Ordinal);
                var manifest = sources.GetFileInfo(library.Name + "/library.json");
                if (manifest.Exists)
                {
                    using var stream = manifest.CreateReadStream();
                    using var json = JsonDocument.Parse(stream);
                    if (json.RootElement.TryGetProperty("variants", out var declared))
                    {
                        foreach (var variant in declared.EnumerateObject())
                        {
                            variants.Add(variant.Name);
                        }
                    }
                }

                foreach (var variant in variants)
                {
                    var root = library.Name + "/" + variant;
                    Watch(root + "/icons.zip", IconChangeKind.VariantSources, library.Name, variant);
                    Watch(root + "/icons/*.svg", IconChangeKind.VariantSources, library.Name, variant);
                    Watch(root + "/user/*.svg", IconChangeKind.VariantSources, library.Name, variant);
                }
            }

            var next = new SourceChangeToken(_watches.Select(x => x.Token).ToArray());
            _sourceToken?.Dispose();
            _sourceToken = next;
            return next;

            void Watch(string filter, IconChangeKind kind, string library = null, string variant = null)
            {
                // Reuse unsignaled watches, including any that changed during this refresh.
                // Their token keeps the next generation stale until its notification is consumed.
                _watches.Add(previous.TryGetValue(filter, out var existing)
                    ? existing
                    : (filter, sources.Watch(filter), new(kind, library, variant)));
            }
        }
    }

    /// <inheritdoc />
    public async Task TrackCacheFileAsync(IconCacheArtifact artifact, IconCatalog catalog, CancellationToken cancelToken = default)
    {
        Guard.NotNull(artifact);
        Guard.NotNull(catalog);

        var sourceSnapshot = catalog.ChangeToken;
        ValidateArtifact(artifact);
        using (await _gate.LockAsync(cancelToken: cancelToken))
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureCurrent(sourceSnapshot);
            _cache.GetDirectory(string.Empty).Create();
            var indexWasMissing = !_cache.FileExists("index.json");
            if (_index == null || indexWasMissing)
            {
                _index = LoadIndex();
                _validatedSources = null;
            }

            var changed = false;

            // Reconcile disk only after restart, index loss, or observed source changes.
            // Never infer obsolescence from an empty in-memory catalog.
            if (_validatedSources != sourceSnapshot)
            {
                var files = EnumerateArtifacts().ToArray();
                var names = files.Select(x => Path.GetRelativePath(_cache.Root, x.PhysicalPath).Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
                var current = _recovery.GetCurrentFiles(catalog, names).ToList();

                foreach (var item in current)
                {
                    ValidateArtifact(item);
                }

                EnsureCurrent(sourceSnapshot);
                var currentNames = current.Select(x => x.File).ToHashSet(StringComparer.Ordinal);
                var now = DateTimeOffset.UtcNow;

                foreach (var file in files)
                {
                    var name = Path.GetRelativePath(_cache.Root, file.PhysicalPath).Replace('\\', '/');
                    if (currentNames.Contains(name))
                    {
                        _index.ObsoleteFiles.Remove(name);
                    }
                    else
                    {
                        _index.ObsoleteFiles.TryAdd(name, _index.Recovered ? now : file.LastModified);
                    }
                }

                _index.CurrentFiles = current;
                _index.Recovered = true;
                _validatedSources = sourceSnapshot;
                changed = true;
            }

            EnsureCurrent(sourceSnapshot);
            var previous = _index.CurrentFiles.FirstOrDefault(x => SameSelection(x, artifact));
            if (previous != artifact)
            {
                _index.CurrentFiles.RemoveAll(x => SameSelection(x, artifact));
                _index.CurrentFiles.Add(artifact);
                changed = true;
            }

            changed |= _index.ObsoleteFiles.Remove(artifact.File);
            if (previous != null && previous.File != artifact.File && !_index.CurrentFiles.Any(x => x.File == previous.File))
            {
                _index.ObsoleteFiles.TryAdd(previous.File, DateTimeOffset.UtcNow);
            }

            if (changed)
            {
                await SaveIndexAsync();
            }

            if (_cleanup.IsCompleted && DateTimeOffset.UtcNow - _lastScan >= ScanInterval)
            {
                _lastScan = DateTimeOffset.UtcNow;

                // Track this one-shot task so DI disposal can await it. No timer or message bus.
                _cleanup = Task.Run(async () =>
                {
                    using (await _gate.LockAsync())
                    {
                        if (_disposed || sourceSnapshot.HasChanged)
                        {
                            return;
                        }

                        try
                        {
                            await CleanupAsync(sourceSnapshot);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "Icon cache cleanup will retry on a later trigger.");
                        }
                    }
                });
            }
        }
    }

    private async Task CleanupAsync(IChangeToken snapshot)
    {
        var changed = false;
        var current = _index.CurrentFiles.Select(x => x.File).ToHashSet(StringComparer.Ordinal);

        foreach (var entry in _index.ObsoleteFiles.ToArray())
        {
            EnsureCurrent(snapshot);
            if (current.Contains(entry.Key))
            {
                changed |= _index.ObsoleteFiles.Remove(entry.Key);
                continue;
            }

            if (DateTimeOffset.UtcNow - entry.Value < Retention)
            {
                continue;
            }

            try
            {
                var file = _cache.GetFile(entry.Key);
                if (file.Exists)
                {
                    await file.DeleteAsync();
                }

                changed |= _index.ObsoleteFiles.Remove(entry.Key);
            }
            catch (IOException ex)
            {
                Logger.Warn(ex, "Could not delete an obsolete icon artifact.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Warn(ex, "Could not delete an obsolete icon artifact.");
            }
        }

        if (changed)
        {
            await SaveIndexAsync();
        }
    }

    private IconCacheIndex LoadIndex()
    {
        try
        {
            if (_cache.FileExists("index.json"))
            {
                var index = JsonSerializer.Deserialize<IconCacheIndex>(_cache.ReadAllText("index.json"), SmartJsonOptions.CamelCased);
                if (index?.SchemaVersion != 1 || index.CurrentFiles == null || index.ObsoleteFiles == null)
                {
                    throw new InvalidDataException("Unsupported icon cache index.");
                }

                foreach (var item in index.CurrentFiles)
                {
                    ValidateArtifact(item);
                }

                foreach (var file in index.ObsoleteFiles.Keys)
                {
                    ValidatePath(file);
                }

                index.Recovered = true;
                return index;
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            Logger.Warn(ex, "Reconstructing the icon cache index from current sources.");
        }

        return new();
    }

    private async Task SaveIndexAsync()
    {
        // Publish complete JSON. A crash between replacement steps leaves a missing index,
        // which is recoverable; readers never observe a partly written index.json.
        try
        {
            await _cache.WriteAllTextAsync("index.pending.json", SmartJsonOptions.CamelCased.SerializeIndented(_index));
            if (_cache.FileExists("index.json"))
            {
                await _cache.GetFile("index.json").DeleteAsync();
            }

            await _cache.GetFile("index.pending.json").MoveToAsync(_cache.GetFile("index.json").SubPath);
        }
        catch
        {
            // A failed write must not make subsequent registrations trust unsaved in-memory state.
            _index = null;
            _validatedSources = null;
            throw;
        }
    }

    private IEnumerable<IFile> EnumerateArtifacts()
    {
        foreach (var folder in new[] { "kits", "browser" })
        {
            var directory = _cache.GetDirectory(folder);
            if (!directory.Exists)
            {
                continue;
            }

            foreach (var file in directory.EnumerateFiles())
            {
                var path = Path.GetRelativePath(_cache.Root, file.PhysicalPath).Replace('\\', '/');
                if (IsArtifactPath(path) && !file.IsSymbolicLink(out _))
                {
                    yield return file;
                }
            }
        }
    }

    private static bool SameSelection(IconCacheArtifact x, IconCacheArtifact y)
        => x.Kind == y.Kind && x.Kit == y.Kit && x.Library == y.Library && x.Variant == y.Variant;

    private static void EnsureCurrent(IChangeToken snapshot)
    {
        if (snapshot.HasChanged)
        {
            throw new IOException("Icon sources changed during cache maintenance. Retry with the current catalog.");
        }
    }

    private static bool IsArtifactPath(string path)
        => path != null && Regex.IsMatch(path, @"\A(?:kits/[a-zA-Z0-9_-]+-[0-9a-f]{24}\.(?:svg|json)|browser/[a-zA-Z0-9_-]+-[0-9a-f]{24}\.svg)\z");

    private static void ValidatePath(string path)
    {
        if (!IsArtifactPath(path))
        {
            throw new InvalidDataException("Invalid icon cache artifact path.");
        }
    }

    private static void ValidateArtifact(IconCacheArtifact item)
    {
        if (item == null || item.Kind is not ("kit" or "browser" or "manifest"))
        {
            throw new InvalidDataException("Invalid icon cache selection.");
        }

        ValidatePath(item.File);
    }

    public async ValueTask DisposeAsync()
    {
        Task cleanup;
        using (await _gate.LockAsync())
        {
            _disposed = true;
            cleanup = _cleanup;
        }

        await cleanup;

        lock (_watchLock)
        {
            _sourceToken?.Dispose();
        }

        _ownedCacheRoot?.Dispose();
    }

    // Active watchers set one flag. Passive providers are checked at most once a second.
    private sealed class SourceChangeToken : IChangeToken, IDisposable
    {
        private readonly CancellationTokenSource _signal = new();
        private readonly CancellationToken _token;
        private readonly IChangeToken[] _passiveTokens;
        private readonly List<IDisposable> _registrations = [];
        private long _lastPoll;

        public SourceChangeToken(IChangeToken[] tokens)
        {
            _token = _signal.Token;
            _passiveTokens = tokens.Where(x => !x.ActiveChangeCallbacks && x != NullChangeToken.Singleton).ToArray();
            foreach (var token in tokens)
            {
                if (token.ActiveChangeCallbacks)
                {
                    _registrations.Add(token.RegisterChangeCallback(
                        static state => ((CancellationTokenSource)state).Cancel(), _signal));
                }

                // A notification may already have arrived before subscription.
                if (token.HasChanged)
                {
                    _signal.Cancel();
                }
            }
        }

        public bool HasChanged
        {
            get
            {
                if (_token.IsCancellationRequested)
                {
                    return true;
                }

                if (_passiveTokens.Length > 0)
                {
                    var now = Environment.TickCount64;
                    var last = Volatile.Read(ref _lastPoll);
                    if (now - last >= 1000 && Interlocked.CompareExchange(ref _lastPoll, now, last) == last
                        && _passiveTokens.Any(x => x.HasChanged))
                    {
                        _signal.Cancel();
                    }
                }

                return _token.IsCancellationRequested;
            }
        }

        public bool ActiveChangeCallbacks => true;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            => _token.Register(callback, state);

        public void Dispose()
        {
            foreach (var registration in _registrations)
            {
                registration.Dispose();
            }

            _signal.Cancel();
            // Old catalog readers may still poll this token. Keep its signal usable until collected.
        }
    }

    private sealed class IconCacheIndex
    {
        public int SchemaVersion { get; set; } = 1;
        public List<IconCacheArtifact> CurrentFiles { get; set; } = [];
        public Dictionary<string, DateTimeOffset> ObsoleteFiles { get; set; } = new(StringComparer.Ordinal);
        [JsonIgnore]
        public bool Recovered { get; set; }
    }
}
