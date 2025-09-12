# Performance guide

High performance keeps stores responsive and lowers infrastructure cost. The following practices help you squeeze the most out of Smartstore.

## Build and deployment

* Compile the application in **Release** configuration.
* Use [view precompilation](view-precompilation.md) to remove first-request delays caused by Razor compilation.
* Enable compression and HTTP/2 on the web server or reverse proxy.

## Caching

Smartstore provides static and request level caches through the [`ICache` abstractions](../framework/platform/caching.md). Cache expensive results and invalidate them when underlying data changes.

```csharp
var key = CacheKey.With("homepage:featured-products");
var products = await _cache.GetAsync(key, () => LoadFeaturedProductsAsync());
```

For multi-node setups configure a distributed cache in `appsettings.json` so cached entries are shared across servers.

### Output caching

Cache whole page responses with the [output cache](../framework/platform/output-cache.md) module to avoid regenerating markup for anonymous visitors.

Mark pages or view components as cacheable by registering their routes. Implement `ICacheableRouteProvider` in your module and return the route patterns:

```csharp
internal sealed class CacheableRoutes : ICacheableRouteProvider
{
    public int Order => 0;

    public IEnumerable<string> GetCacheableRoutes()
    {
        return new[]
        {
            "Catalog/Category",        // full page
            "vc:SearchBox"             // view component
        };
    }
}
```

When data changes inject `IOutputCacheInvalidationObserver` and call `InvalidateByRouteAsync` to purge affected entries.

```csharp
public override void BuildPipeline(RequestPipelineBuilder builder)
{
    var observer = builder.ApplicationBuilder.ApplicationServices
        .GetRequiredService<IOutputCacheInvalidationObserver>();

    // Invalidate cached category pages when SKU visibility toggles
    observer.ObserveSettingProperty<CatalogSettings>(
        s => s.ShowProductSku,
        p => p.InvalidateByRouteAsync("Catalog/Category"));
}
```

## Database access

* Prefer eager loading to avoid `N+1` query patterns.
* Use [`DbContextScope` with `forceNoTracking`](../framework/advanced/data-access-deep-dive/dbcontextscope.md) for read-only operations.
* Pool contexts via the [pooled `DbContextFactory`](../framework/advanced/data-access-deep-dive/dbcontextfactory.md) when processing large batches.

## Static assets

Bundle and minify JavaScript and CSS in production. The built‑in bundler emits hashed file names that can be cached by CDNs and browsers for long periods.

Processed media files are stored in the thumbs cache so the original image is resized only once. See [imaging](../framework/content/imaging.md) for available resize tokens.

## Profiling and diagnostics

Use the MiniProfiler [diagnostics tool](../framework/platform/diagnostics.md) to inspect slow requests. The widget shows executed queries and timings, helping you track down bottlenecks.

## General tips

* Keep logging at `Information` level in production and periodically purge old rows with `IDbLogService`.
* Use async APIs throughout to free up threads.
* Avoid synchronous network calls or blocking I/O inside request handlers.
