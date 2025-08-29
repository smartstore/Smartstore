---
description: Improve performance and scalability
---

# 🐥 Output Cache

## Concept

Output caching is a technique used to improve the performance and scalability of Smartstore by storing the generated output of pages on the server side in memory, relational database or in a REDIS database. When a client requests a store page, the server first checks if a cached copy of the page exists. If it does, the server can serve the cached copy to the client without generating the page again, reducing the processing and network overhead.

Smartstore also ensures that cached content is invalidated or refreshed when it becomes stale or outdated, and that private or sensitive data is not inadvertently cached. Proper caching can significantly reduce the response time and resource usage of a store, improving the user experience and reducing server load.

### "Donut Hole" Caching

Smartstore follows the so-called _Donut Hole Caching_ strategy. In this strategy, a dynamic web page is divided into two parts: a non-dynamic or semi-static outer layer, and a dynamic inner layer. The outer layer is cached, while the inner layer is not, and is generated on each request.

The term "donut hole" refers to the inner dynamic layer, which is surrounded by the static outer layer. The outer layer is cached in its entirety, including its HTML, CSS, and JavaScript files. When a user requests the web page, the server first checks the cache for the outer layer, and if it is found, returns it to the user. This can result in a significant performance improvement since the server does not need to generate the outer layer on each request.

However, the inner dynamic layer, which contains content that changes frequently, such as user-specific data or real-time information, is not cached. This ensures that the content remains fresh and up-to-date. When a request for the inner layer is received, the server generates it dynamically and inserts it into the cached outer layer, creating the final page that is returned to the user.

Donut Hole Caching is a balance between the performance benefits of caching and the need for up-to-date, dynamic content.

### Availability

In Smartstore, output caching is handled by a commercial module that is **not** part of the open-source Community Edition. The module builds on the caching infrastructure provided by the Smartstore core and provides all the necessary implementations to actually enable and operate the output cache.

However, when developing custom modules for Smartstore, it's important to take output caching into account. This means that you need to ensure that your module works properly with the output caching system. This may involve configuring the caching settings for the module or using cache tags to ensure that the cache is cleared when necessary.

## Cacheable routes

{% hint style="info" %}
Clarification of the terminology relating to the [Donut Hole](output-cache.md#donut-hole-caching):

* **Page** is the static outer layer.
* **Component** is the dynamic inner layer.
{% endhint %}

### Route identifiers

A _cachable route_ represents the stringified route to a page or view component. It is the route identifier and structured as follows:

<table><thead><tr><th width="181.33333333333331">Element</th><th width="231">Route pattern</th><th>Example</th></tr></thead><tbody><tr><td><strong>Full page</strong></td><td><code>[{Module}/]{ControllerShortName}/{Action}</code></td><td><code>Smartstore.Blog/Blog/List</code>, <code>Catalog/Category</code></td></tr><tr><td><strong>View component</strong></td><td><code>vc:[{Module}/]{ComponentShortName}</code></td><td><code>vc:SearchBox</code>, <code>vc:Smartstore.Blog/BlogSummary</code></td></tr></tbody></table>

{% hint style="warning" %}
**Module must be omitted** if the controller / component is part of the application core.
{% endhint %}

{% hint style="info" %}
Output caching implements the _opt-in_ approach. This means that only **explicitly specified** routes are recognized as candidates. If you develop a module and do not include any caching functionality, nothing will be cached.
{% endhint %}

If any **full page** route pattern matches the current request route, the generated page is cached.

Several environmental elements are used to generate the cache entry key. The cached entry is different for each of these variants: The path and query string; the current language, the current currency; the current store; the current theme; all customer roles; the version of the application.

### Substitution

The output of view components, contained within the page, that do not match any registered **component** route patterns are removed from the outer layer. Their content is dynamically generated on each request later down the line. The deferred replacement of the _donut holes_ is called _substitution_.

This procedure can only work if you ensure that the method arguments of the component’s `Invoke` method do not depend on any parent or outer view models. After the first request, the models will no longer be accessible because the content will be fetched from the cache. Additionally, the arguments must all be serializable to JSON. The component’s output is replaced by a JSON representation of its metadata and actual `Invoke` arguments in the cached outer layer.

If the component is made cacheable, there is no need to worry about parameter restrictions. Their output will **not** be removed by later substitutions, but will be cached along with the outer layer.

{% hint style="warning" %}
**Never make user-related components cacheable.** If they display personal information and are cached, that information could be served to another user.
{% endhint %}

{% hint style="info" %}
Backend pages are **never** cached, only the frontend pages.
{% endhint %}

### Apply cacheable routes

To specify cacheable routes for your module, simply add an `internal` class that implements the [ICacheableRouteProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/OutputCache/ICacheableRouteProvider.cs). By convention, the class is called `CacheableRoutes`. No DI registration is required.

Here is an example of a module’s cacheable routes:

```csharp
internal sealed class CacheableRoutes : ICacheableRouteProvider
{
    public int Order => 0;

    public IEnumerable<string> GetCacheableRoutes()
    {
        return new string[]
        {
            // Full pages: {ModuleSystemName}/{Controller}/{Action}
            "BlogModule/Blog/List",
            "BlogModule/Blog/BlogByTag",
            "BlogModule/Blog/BlogByMonth",
            "BlogModule/Blog/BlogPost",
            // View components: vc:/{ModuleSystemName}/{ComponentShortName}
            "vc:BlogModule/BlogSummaryList",
            "vc:BlogModule/BlogSummary"
        };
    }
}
```

## Display control

The most complicated issue with output caching is determining when cached content becomes stale or outdated. For example, if _product A_ is updated or deleted in the backend, **any** page that displays _product A_ **in any way** must be removed from the cache. The same goes for categories, brands, blog, news, etc.

### Announcing

By **announcing** the display of entities, Output Cache can automate this invalidation. The best point to make this announcement is when you are preparing a view model to be rendered by a view.

The [IDisplayControl](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/OutputCache/IDisplayControl.cs) service collects these announcements. You simply pass it to the constructor of your controller. After that, you call its `Announce` method wherever you need it, passing the entity instance you want to display as a parameter.

```csharp
private readonly IDisplayControl _displayControl;

public MyAnnouncer(IDisplayControl displayControl)
{
    _displayControl = displayControl;
}

public void AnnounceProduct(Product product)
{
    _displayControl.Announce(product);
}
```

The `Announce` method creates **tags** for all passed entities. They are collected later by the cache service during the cache entry generation and are also cached. This way, any time an entity is edited or deleted, all cached entries containing the entity’s tag are invalidated.

### Tag generation handlers

In order for `IDisplayControl` to generate tags for custom entity types, you must register tag generation handlers. To do this, call the static `DisplayControl.RegisterHandlerFor(Type, DisplayControlHandler)` method.

It is best to register a handler in the `ConfigureServices` method of your starter class:

```csharp
public override void ConfigureServices(
    IServiceCollection services, 
    IApplicationContext appContext)
{
    // ...
    
    // x is entity instance
    // d is SmartDbContext instance
    // c is the dependency scope to resolve services from
    
    // For a BlogPost entity with Id 5, create tag "b5"
    DisplayControl.RegisterHandlerFor(typeof(BlogPost), (x, d, c)
        => Task.FromResult<IEnumerable<string>>(new[] { "b" + x.Id }));
    
    // For a related BlogComment entity of a BlogPost with Id 5, also create tag "b5"
    DisplayControl.RegisterHandlerFor(typeof(BlogComment), (x, d, c)
        => Task.FromResult<IEnumerable<string>>(new[] { "b" + ((BlogComment)x).BlogPostId }));
}
```

### Uncacheable Requests

If the generated output contains content that should not be cached, even though it has a matching route identifier, you can mark the request as `uncacheable`. Call the `IDisplayControl.MarkRequestAsUncacheable()` method, and the cache provider will no longer intercept the action after this call.

## Invalidation observer

The invalidation observer provides handler registration for custom output cache invalidation used in more complex scenarios. The registrable handlers are Entity and Setting observe handlers.

### IOutputCacheInvalidationObserver

[IOutputCacheInvalidationObserver](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/OutputCache/IOutputCacheInvalidationObserver.cs) is the required singleton service, which has two methods for registering observers.

`ObserveEntity(Func<ObserveEntityContext, Task>)` registers an observer for an **entity type**. The passed observer is responsible for invalidating the cache by calling an invalidation method on the `IOutputCacheProvider` instance. The observer must set the `ObserveEntityContext.Handled` property to `true` to signal the framework to skip execution of subsequent observers.

`ObserveSetting(string, Func<IOutputCacheProvider, Task>)` registers an observer for a **setting key**. When the value of the passed setting key changes, the framework calls the `invalidationAction` handler. The key can either be fully qualified like "CatalogSettings.ShowProductSku", or prefixed like "CatalogSettings.\*". The latter will call the invalidator whenever **any** _CatalogSetting_ changes.

The best place to register observers is in the `BuildPipeline` method of your starter class:

```csharp
public override void BuildPipeline(RequestPipelineBuilder builder)
{
    // Resolve instance of singleton IOutputCacheInvalidationObserver
    var observer = builder.ApplicationBuilder.ApplicationServices
        .GetRequiredService<IOutputCacheInvalidationObserver>();
    
    // If any blog setting changes, invalidate all blog pages
    observer.ObserveSettings<BlogSettings>(PurgeBlog);
    
    // If the BlogSettings.Enabled property changes, then invalidate ALL
    // cached pages (whether they are blogs or not). Because it is very likely
    // that a global menu item in the page header is affected.
    observer.ObserveSettingProperty<BlogSettings>(x => x.Enabled);
    
    // Register BlogPost entity change observer
    observer.ObserveEntity(BlogPostObserver);
}

private static async Task BlogPostObserver(ObserveEntityContext context)
{
    // We are going to check if any visibility affecting property name
    // has been changed, and if so, invalidate ALL blog list pages as well.
    
    if (context.Entity is not BlogPost)
        return;
        
    if (context.EntityEntry.InitialState == Data.EntityState.Modified)
    {
        var toxicPropNames = BlogPost.GetVisibilityAffectingPropertyNames();
        var modProps = context.EntityEntry.Entry.GetModifiedProperties();
        if (modProps.Keys.Any(x => toxicPropNames.Contains(x)))
        {
            await context.OutputCacheProvider.InvalidateByRouteAsync(
                "BlogModule/Blog/List",
                "BlogModule/Blog/BlogByTag",
                "BlogModule/Blog/BlogByMonth",
                "BlogModule/Blog/BlogPost");
        }
    }
}

// Delete all pages that are associated with blog routes from cache
private static Task PurgeBlog(IOutputCacheProvider provider)
{
    return provider.InvalidateByRouteAsync(
        "BlogModule/Blog/List",
        "BlogModule/Blog/BlogByTag",
        "BlogModule/Blog/BlogByMonth",
        "BlogModule/Blog/BlogPost");
}
```
