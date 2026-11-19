using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.IconKit.Sources;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO;
using Smartstore.Json;
using Smartstore.Threading;

namespace Smartstore.IconKit.Cache;

public sealed class IconCacheLifecycle : IIconCacheLifecycle, IAsyncDisposable
{
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);
    private readonly AsyncLock _gate = new();
    private readonly AsyncLock _recoveryGate = new();
    private readonly Lock _watchLock = new();
    private readonly List<(string Filter, IChangeToken Token, IconChange Change)> _watches = [];
    private SourceChangeToken _sourceToken;
    private IconCacheIndex _index;
    private readonly IFileSystem _fileCache;
    private readonly IIconCacheRecovery _recovery;
    private object _validatedArtifacts;
    private DateTimeOffset _lastScan;
    private Task _cleanup = Task.CompletedTask;
    private bool _disposed;

    public IconCacheLifecycle(
        IApplicationContext appContext,
        IOptions<IconKitOptions> options,
        IIconCacheRecovery recovery)
    {
        _recovery = recovery;
        _fileCache = IconFileHelper.CreateFileSystem(appContext, options.Value.CachePath);
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
    public IChangeToken StartWatchingSources(IFileSystem sourceFileSystem)
    {
        Guard.NotNull(sourceFileSystem);

        lock (_watchLock)
        {
            var previous = _watches.ToDictionary(x => x.Filter, StringComparer.Ordinal);
            _watches.Clear();

            Watch("config.json", IconChangeKind.GlobalConfiguration);
            Watch("kits.json", IconChangeKind.GlobalConfiguration);
            Watch("collections.json", IconChangeKind.GlobalConfiguration);

            // Includes newly installed libraries and changes to variant declarations.
            Watch("*/library.json", IconChangeKind.LibraryManifest);

            foreach (var library in sourceFileSystem.GetDirectoryContents(string.Empty).Where(x => x.IsDirectory))
            {
                Watch(library.Name + "/mapping.json", IconChangeKind.Mapping, library.Name);
                Watch(library.Name + "/metadata.json", IconChangeKind.SearchMetadata, library.Name);
                var variants = new HashSet<string>(StringComparer.Ordinal);
                var manifest = sourceFileSystem.GetFileInfo(library.Name + "/library.json");
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
                    : (filter, sourceFileSystem.Watch(filter), new(kind, library, variant)));
            }
        }
    }

    /// <inheritdoc />
    public async Task TrackCacheFileAsync(IconCacheArtifact artifact, IconCatalog catalog, CancellationToken cancelToken = default)
    {
        Guard.NotNull(artifact);
        Guard.NotNull(catalog);
        cancelToken.ThrowIfCancellationRequested();

        var sourceSnapshot = catalog.ChangeToken;
        var artifactVersion = catalog.ArtifactVersion;
        ValidateArtifact(artifact);
        await EnsureIndexAsync(catalog, cancelToken);

        using (await _gate.LockAsync())
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureCurrent(sourceSnapshot);
            if (_validatedArtifacts != artifactVersion)
            {
                throw new IOException("The icon cache index changed during registration. Retry with the current catalog.");
            }

            var changed = false;
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

            StartCleanupIfDue(sourceSnapshot, artifactVersion);
        }
    }

    // Called under the index lock so concurrent registrations cannot start competing cleanups.
    private void StartCleanupIfDue(IChangeToken sourceSnapshot, object artifactVersion)
    {
        if (!_cleanup.IsCompleted || DateTimeOffset.UtcNow - _lastScan < ScanInterval)
        {
            return;
        }

        _lastScan = DateTimeOffset.UtcNow;

        // Keep the task so shutdown can wait for it. No periodic timer is needed.
        _cleanup = Task.Run(async () =>
        {
            using (await _gate.LockAsync())
            {
                if (_disposed || _validatedArtifacts != artifactVersion || sourceSnapshot.HasChanged)
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

    private async Task EnsureIndexAsync(IconCatalog catalog, CancellationToken cancelToken)
    {
        var snapshot = catalog.ChangeToken;
        var artifactVersion = catalog.ArtifactVersion;
        using (await _gate.LockAsync())
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureCurrent(snapshot);
            if (_validatedArtifacts == artifactVersion && _fileCache.FileExists("index.json"))
            {
                return;
            }
        }

        // Only one request rebuilds the index. Other requests reuse its result.
        using (await _recoveryGate.LockAsync())
        {
            IconCacheIndex index;
            IconCacheArtifact[] knownFiles;
            using (await _gate.LockAsync())
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                EnsureCurrent(snapshot);
                if (_validatedArtifacts == artifactVersion && _fileCache.FileExists("index.json"))
                {
                    return;
                }

                _fileCache.GetDirectory(string.Empty).Create();
                if (_index == null || !_fileCache.FileExists("index.json"))
                {
                    _index = LoadIndex();
                }

                index = _index;
                knownFiles = index.CurrentFiles.ToArray();
                _validatedArtifacts = null;
            }

            // Directory reads and revision calculation do not hold the index lock.
            var files = EnumerateArtifacts().ToDictionary(
                file => Path.GetRelativePath(_fileCache.Root, file.PhysicalPath).Replace('\\', '/'),
                file => file.LastModified,
                StringComparer.Ordinal);
            var current = _recovery.GetCurrentFiles(catalog, files.Keys.ToHashSet(StringComparer.Ordinal), knownFiles).ToList();
            foreach (var item in current)
            {
                ValidateArtifact(item);
            }

            var currentNames = current.Select(x => x.File).ToHashSet(StringComparer.Ordinal);

            using (await _gate.LockAsync())
            {
                cancelToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_disposed, this);
                EnsureCurrent(snapshot);

                // Never overwrite state replaced while the calculation was running.
                if (_index != index)
                {
                    throw new IOException("The icon cache index changed during recovery. Retry with the current catalog.");
                }

                var now = DateTimeOffset.UtcNow;
                foreach (var file in files)
                {
                    if (currentNames.Contains(file.Key))
                    {
                        index.ObsoleteFiles.Remove(file.Key);
                    }
                    else
                    {
                        index.ObsoleteFiles.TryAdd(file.Key, index.Recovered ? now : file.Value);
                    }
                }

                index.CurrentFiles = current;
                index.Recovered = true;
                await SaveIndexAsync();
                _validatedArtifacts = artifactVersion;
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
                var file = _fileCache.GetFile(entry.Key);
                if (file.Exists)
                {
                    await file.DeleteAsync();
                }

                changed |= _index.ObsoleteFiles.Remove(entry.Key);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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
            if (_fileCache.FileExists("index.json"))
            {
                var index = JsonSerializer.Deserialize<IconCacheIndex>(_fileCache.ReadAllText("index.json"), SmartJsonOptions.CamelCased);
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
            await _fileCache.WriteAllTextAsync("index.pending.json", SmartJsonOptions.CamelCased.SerializeIndented(_index));
            if (_fileCache.FileExists("index.json"))
            {
                await _fileCache.GetFile("index.json").DeleteAsync();
            }

            await _fileCache.GetFile("index.pending.json").MoveToAsync(_fileCache.GetFile("index.json").SubPath);
        }
        catch
        {
            // A failed write must not make subsequent registrations trust unsaved in-memory state.
            _index = null;
            _validatedArtifacts = null;
            throw;
        }
    }

    private IEnumerable<IFile> EnumerateArtifacts()
    {
        foreach (var folder in new[] { "kits", "browser" })
        {
            var directory = _fileCache.GetDirectory(folder);
            if (!directory.Exists)
            {
                continue;
            }

            foreach (var file in directory.EnumerateFiles())
            {
                var path = Path.GetRelativePath(_fileCache.Root, file.PhysicalPath).Replace('\\', '/');
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

        var prefix = item.Kind switch
        {
            "kit" when IconAddress.IsQualifier(item.Kit) && IconAddress.IsQualifier(item.Library)
                && IconAddress.IsQualifier(item.Variant) => $"kits/{item.Kit}-",
            "browser" when IconAddress.IsQualifier(item.Library) && IconAddress.IsQualifier(item.Variant)
                => $"browser/{item.Library}-{item.Variant}-",
            "manifest" => "kits/manifest-",
            _ => null
        };
        var extension = item.Kind == "manifest" ? ".json" : ".svg";
        if (prefix == null || !item.File.StartsWith(prefix, StringComparison.Ordinal)
            || !item.File.EndsWith(extension, StringComparison.Ordinal)
            || item.File.Length != prefix.Length + 24 + extension.Length)
        {
            throw new InvalidDataException("The icon cache file does not match its saved selection.");
        }
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

        // Wait for recovery to finish before shutdown completes.
        using var recoveryLock = await _recoveryGate.LockAsync();
    }

    // Active watchers set one flag. Passive providers are checked at most once a second.
    private sealed class SourceChangeToken : IChangeToken
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
