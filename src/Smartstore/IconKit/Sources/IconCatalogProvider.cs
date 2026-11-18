using Smartstore.IconKit.Cache;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.IconKit.Sources;

public sealed class IconCatalogProvider : IIconCatalogProvider, IDisposable
{
    private readonly IApplicationContext _applicationContext;
    private readonly IIconCacheLifecycle _lifecycle;
    private readonly IFileSystem _files;
    private readonly IDisposable _ownedSourceRoot;
    private readonly Lock _sync = new();
    private readonly List<IconChange> _pendingChanges = [];
    private IconCatalog _catalog;

    public IconCatalogProvider(
        IApplicationContext applicationContext,
        IOptions<IconKitOptions> options,
        IIconCacheLifecycle lifecycle)
    {
        _applicationContext = applicationContext;
        _lifecycle = lifecycle;

        var sourceRoot = applicationContext.ContentRoot;
        if (!PathUtility.PathIsWithinDirectory(options.Value.SourcePath, sourceRoot.Root, out _))
        {
            // Keep support for explicitly configured source folders outside the application.
            sourceRoot = new LocalFileSystem(Path.GetDirectoryName(options.Value.SourcePath));
            _ownedSourceRoot = (IDisposable)sourceRoot;
        }

        _files = new ExpandedFileSystem(Path.GetRelativePath(sourceRoot.Root, options.Value.SourcePath), sourceRoot);
    }

    /// <inheritdoc />
    public IconCatalog Catalog
    {
        get
        {
            // Reuse the published catalog without locking while its sources are unchanged.
            var catalog = Volatile.Read(ref _catalog);
            if (catalog != null && !catalog.ChangeToken.HasChanged)
            {
                return catalog;
            }

            return RefreshCatalog();
        }
    }

    private IconCatalog RefreshCatalog()
    {
        lock (_sync)
        {
            // Another request may have refreshed the catalog while we waited for the lock.
            var catalog = _catalog;
            if (catalog != null && !catalog.ChangeToken.HasChanged)
            {
                return catalog;
            }

            _pendingChanges.AddRange(_lifecycle.CollectChanges());

            // Keep pending changes if loading fails, so the next attempt can apply them.
            var token = _lifecycle.StartWatchingSources(_files);
            var next = IconCatalog.Load(_files, CreateProviders(), _applicationContext.ContentRoot,
                token, catalog, _pendingChanges);

            // Publish only the completed catalog. Existing readers can finish with the old one.
            Volatile.Write(ref _catalog, next);
            _pendingChanges.Clear();

            return next;
        }
    }

    public void Dispose()
    {
        _ownedSourceRoot?.Dispose();
    }

    private IEnumerable<IIconProvider> CreateProviders()
    {
        if (!_applicationContext.IsInstalled)
        {
            yield break;
        }

        var types = _applicationContext.TypeScanner.FindTypes<IIconProvider>()
            .OrderBy(x => x.FullName, StringComparer.Ordinal).ToArray();
        if (types.Length == 0)
        {
            yield break;
        }

        using var scope = _applicationContext.Services.BeginLifetimeScope();
        foreach (var type in types)
        {
            var provider = (IIconProvider)scope.ResolveUnregistered(type);
            try
            {
                yield return provider;
            }
            finally
            {
                // ResolveUnregistered does not enroll the instance in Autofac's disposal tracking.
                (provider as IDisposable)?.Dispose();
            }
        }
    }

}
