# Distributed locking

Distributed locks coordinate access to shared resources across threads or
servers. Smartstore exposes a small abstraction consisting of
`IDistributedLockProvider` and `IDistributedLock` that you can use whenever a
critical section must not run concurrently on multiple nodes.

## Obtaining and acquiring locks

Inject `IDistributedLockProvider` and ask it for a lock keyed by any unique
string. Acquire the lock inside a `using`/`await using` block to ensure it is
released when the handle is disposed.

```csharp
public class FeedService
{
    private readonly IDistributedLockProvider _locks;

    public FeedService(IDistributedLockProvider locks) => _locks = locks;

    public async Task GenerateAsync()
    {
        var @lock = _locks.GetLock("feeds:generate");

        await using (await @lock.AcquireAsync())
        {
            // Critical section. Only one node can execute this at a time.
        }
    }
}
```

Use `Acquire`/`AcquireAsync` with an optional timeout to wait for a lock. When
you only want to try once without blocking, the extension methods
`TryAcquire`/`TryAcquireAsync` return a boolean or `AsyncOut` result.

## Implementations

`AddDistributedSemaphoreLockProvider` registers the built‑in provider that
relies on an in‑memory semaphore. It is sufficient for single‑node setups but
does not coordinate across processes. For multi‑node deployments implement
`IDistributedLockProvider` yourself, e.g. using Redis or SQL, and register it
as a singleton:

```csharp
public class RedisLockProvider : IDistributedLockProvider
{
    public IDistributedLock GetLock(string key) => new RedisDistributedLock(key);
}

builder.Services.AddSingleton<IDistributedLockProvider, RedisLockProvider>();
```

Components such as the caching infrastructure and URL service make use of
distributed locks to avoid race conditions. Your code can do the same by
requesting locks with descriptive keys.