using Microsoft.Extensions.Options;
using Smartstore.IconKit.Cache;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.IconKit.Sources;

public sealed class IconCatalogAccessor : IIconCatalogAccessor
{
    private readonly IApplicationContext _appContext;
    private readonly IIconCacheLifecycle _lifecycle;
    private readonly IFileSystem _sourceFileSystem;
    private readonly Lock _sync = new();
    private readonly List<IconChange> _pendingChanges = [];
    private IconCatalog _catalog;

    public IconCatalogAccessor(
        IApplicationContext appContext,
        IOptions<IconKitOptions> options,
        IIconCacheLifecycle lifecycle)
    {
        _appContext = appContext;
        _lifecycle = lifecycle;

        _sourceFileSystem = IconFileHelper.CreateFileSystem(appContext, options.Value.SourcePath);
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
            var token = _lifecycle.StartWatchingSources(_sourceFileSystem);
            IconCatalog next;
            if (catalog != null && _pendingChanges.Count > 0
                && _pendingChanges.All(x => x.Kind == IconChangeKind.SearchMetadata))
            {
                catalog.RefreshSearchMetadata(_sourceFileSystem, _pendingChanges, token);
                next = catalog;
            }
            else
            {
                next = IconCatalog.Load(_sourceFileSystem, CreateProviders(), _appContext.ContentRoot,
                    token, catalog, _pendingChanges);
            }

            // Publish only the completed catalog. Existing readers can finish with the old one.
            Volatile.Write(ref _catalog, next);
            _pendingChanges.Clear();

            return next;
        }
    }

    private IEnumerable<IIconProvider> CreateProviders()
    {
        if (!_appContext.IsInstalled)
        {
            yield break;
        }

        var types = _appContext.TypeScanner.FindTypes<IIconProvider>()
            .OrderBy(x => x.FullName, StringComparer.Ordinal).ToArray();
        if (types.Length == 0)
        {
            yield break;
        }

        using var scope = _appContext.Services.BeginLifetimeScope();
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
