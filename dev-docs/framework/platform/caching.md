# ✔️ Caching

## Overview

Cache is used to store application data for faster performance. This is a critical factor in achieving better performance. Smartstore uses two different types of cache: static and request.

### Static cache

The static or singleton cache is used for persistent objects that should live as long as the application runs or for a specified period of time. It can be utilized with the [ICacheManager](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Caching/ICacheManager.cs), which is a composite multi-level cache manager. Although it uses `IMemoryCache` under the hood by default, it provides a unified API for both memory and distributed cache.

If a distributed cache provider (such as [REDIS](https://redis.io/)) is installed, it is accessed in the same way as the memory cache. This is different from how _.NET Core_ handles cache access because it exposes two different APIs: `IMemoryCache` and `IDistributedCache`. Smartstore unifies the two to take advantage of the multi-level character (see below).

### Request cache

The request cache is used to store items that you want to be removed when the request is complete. To access the request cache, use [IRequestCache](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Caching/IRequestCache.cs), which accesses the `HttpContext.Items` dictionary under the hood. If no `HttpContext` exists, a local dictionary is created instead.

## Application cache

### Multi-level cache

`ICacheManager` is a container for multiple cache stores represented by [ICacheStore](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Caching/ICacheStore.cs). Each CRUD method traverses all registered stores to find, update or delete items. Memory stores are prioritized, distributed stores follow. The default store used by `ICacheManager` is [MemoryCacheStore](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Caching/MemoryCacheStore.cs), but modules can provide new stores, such as the REDIS module which provides the `RedisCacheStore`.

The multi-level cache design guarantees:

* A single API for both memory and distributed store.
* **Performance**: A memory store is _MUCH_ faster than a distributed store. The `ICacheManager` will always query memory stores first, and will only fall back to distributed stores if an item does not exist in memory. After retrieving items from a distributed store, the item is also placed in the memory store, so subsequent reads will return the object from memory.
* The `IMessageBus` is responsible for synchronizing all memory stores across web-farm nodes. For example, if server **A** causes an item to be deleted from the distributed store, a notification is sent to server **B** to delete the item from its memory store as well.

{% hint style="warning" %}
Because of the unified API, you must be careful with object types. Your cached objects must be serializable via JSON (_Newtonsoft_, **not** _System.Text.Json_). You must make sure that your object:

* Has a public constructor without parameters
* Does not have circular references
* Does not produce a large object graph
* Does not contain property types that are not serializable
* Is **not** an entity type deriving from `BaseEntity`. <mark style="color:red;">Never do that, really... it is dangerous 😀</mark>
{% endhint %}

To resolve the primary memory or distributed store explicitly, without relying on the composite multi-level manager, use the [ICacheFactory](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Caching/ICacheFactory.cs) service. There are two methods that resolve an instance of `ICacheManager`.

<table><thead><tr><th width="264">Method</th><th>Description</th></tr></thead><tbody><tr><td><code>GetMemoryCache()</code></td><td>The instance only interacts with the current implementation of <code>IMemoryCacheStore</code>.</td></tr><tr><td><code>GetDistributedCache()</code></td><td>The instance only interacts with the current <code>IDistributedCacheStore</code> implementation. If no distributed store exists, a memory store manager is returned instead.</td></tr></tbody></table>

#### Implementing a custom cache store

If you want to implement a custom cache store, you must do the following:

1. Create a class that implements [ICacheStore](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Caching/ICacheStore.cs).
2. Follow the interface contract and implement all members.
3. Register your store implementation in a service container.

```csharp
internal class Startup : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<MyDistributedCacheStore>()
            .As<ICacheStore>()
            .As<IDistributedCacheStore>() // OR as IMemoryCacheStore
            .SingleInstance();
    }
}
```

### Accessing the cache

Accessing the static application cache in Smartstore is simple. All you need to do is get the singleton `ICacheManager` dependency.

```csharp
ICacheManager cache;

return cache.GetAsync("MyCacheItemKey", async options =>
{
    // Set the entry absolute expiration relative to now
    options.ExpiresIn(TimeSpan.FromHours(2));
    
    // ... prepare some model
    var model = await PrepareSomeModelAsync();
    
    return model;
});
```

All calls are thread-safe and can be used concurrently from multiple threads. As described in the following table, `ICacheManager` provides several methods to read, add or update cache items.

| To do this                                                                                                          | Use this method                                    |
| ------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------- |
| Get an item if it exists.                                                                                           | `Get<T>(string, bool)`                             |
| Try to get an item if it exists.                                                                                    | `TryGet(string, out T)`                            |
| Get an item, and, if it doesn't exist, add it (by calling the acquirer function `Func<CacheEntryOptions, T>`).      | `Get<T>(string, Func<CacheEntryOptions, T>, bool)` |
| Get or create a provider-specific hash set. If the key does not exist, create a new set and cache it automatically. | `GetHashSet(string, Func<IEnumerable>)`            |
| Add or update an item if it already exists.                                                                         | `Put(string, object, CacheEntryOptions)`           |
| Remove an item if it exists.                                                                                        | `Remove(string)`                                   |
| Remove many items using a key pattern.                                                                              | `RemoveByPattern(string)`                          |
| Enumerate all existing item keys. Optionally pass a glob pattern for filtering.                                     | `Keys(string)`                                     |
| Set the absolute expiration date of an item.                                                                        | `SetTimeToLive(string, TimeSpan?)`                 |
| Get a lock object for a specified key, which is used to synchronize access to the underlying cache memory.          | `GetLock(string)`                                  |

{% hint style="info" %}
Almost all cache access methods have asynchronous overloads.
{% endhint %}

### Glob patterns

Some cache methods support [glob patterns](https://en.wikipedia.org/wiki/Glob_\(programming\)) in order to match keys. Smartstore supports the following patterns:

<table><thead><tr><th width="115">Wildcard</th><th width="166">Description</th><th width="107">Example</th><th>Matches</th><th>Doesn’t match</th></tr></thead><tbody><tr><td><strong>?</strong></td><td>Matches any single character.</td><td>h<strong>?</strong>llo</td><td><em>h<strong>e</strong>llo</em>, <em>h<strong>a</strong>llo</em> or <em>h<strong>x</strong>llo</em></td><td><em>h<strong>ao</strong>llo</em> or <em>hllo</em></td></tr><tr><td><strong>*</strong></td><td>Matches any string of characters.</td><td>h<strong>*</strong>llo</td><td><em>hllo</em> or <em>h<strong>eeao</strong>llo</em></td><td><em>hlo</em> or <em>llo</em></td></tr><tr><td><strong>[abc]</strong></td><td>Matches any bracketed character.</td><td>h<strong>[ae]</strong>llo</td><td><em>h<strong>e</strong>llo</em> and <em>h<strong>a</strong>llo</em></td><td><em>h<strong>i</strong>llo</em> or <em>h<strong>ae</strong>llo</em></td></tr><tr><td><strong>[^a]</strong></td><td>Matches any non-bracketed character.</td><td>h<strong>[^e]</strong>llo</td><td><em>h<strong>a</strong>llo</em> and <em>h<strong>b</strong>llo</em></td><td><em>h<strong>e</strong>llo</em></td></tr><tr><td><strong>[a-z]</strong></td><td>Matches any character in the given character range.</td><td>h<strong>[a-c]</strong>llo</td><td><em>h<strong>a</strong>llo</em>, <em>h<strong>b</strong>llo</em> and <em>h<strong>c</strong>llo</em></td><td><em>h<strong>e</strong>llo</em> or <em>h<strong>i</strong>llo</em></td></tr></tbody></table>

### Expiration policy

Every item stored in cache has infinite lifetime by default. To limit it you can configure an _absolute_ or _sliding_ expiration per item.

```csharp
ICacheManager cache;

return cache.Get("A", o =>
{
    // Set the entry absolute expiration relative to now
    o.ExpiresIn(TimeSpan.FromHours(2));
    
    // How long an entry can be inactive (e.g. not accessed) before
    // it will be removed. This will not extend the entry lifetime 
    // beyond the absolute expiration (if set)
    o.SetSlidingExpiration(TimeSpan.FromHours(1));
    
    // ... prepare some data and return
    return model;
});

// - OR, alternative to "CacheEntryOptions.ExpiresIn()" -
cache.SetTimeToLive(TimeSpan.FromHours(2));
```

### Item dependencies

By default, every removed cache item automatically invalidates items from parent closures.

```csharp
ICacheManager cache;

return cache.Get("A", () =>
{
    // If "B" is removed from cache or updated at some point in the future,
    // "A" will also be removed, because "A" depends on "B"
    var model = cache.Get("B", o =>
    {
        // ... prepare some data
    });
    
    return model;
});
```

To disable this hierarchy dependency chain, pass the _independent_ parameter to the `Get` method and set its value to `true`. No attempt will be made to invalidate the parent cache entries. Specify custom dependency keys for an item, using the `CacheEntryOptions.DependsOn(string[])` method:

```csharp
ICacheManager cache;

return cache.Get("A", o =>
{
    // Remove "A" from cache whenever "B", "C" or "D" are removed or updated.
    o.DependsOn("B", "C", "D");
    
    // ... prepare some data and return
    return model;
});
```

## Request cache

Accessing the application’s request cache in Smartstore works the same way. Just get the `IRequestCache` dependency (request scoped).

```csharp
IRequestCache requestCache;

return requestCache.Get("A", () =>
{
    // ... prepare some data and return
    return model;
});
```
