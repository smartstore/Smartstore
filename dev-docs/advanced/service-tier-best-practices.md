# Service tier best practices

The service layer encapsulates business operations and orchestrates data access for controllers, scheduled tasks, and other components. Keep it lean and testable by following these guidelines.

Services run in the same dependency injection scope as controllers and tasks. They work with domain entities and leave presentation concerns to higher layers.

## Keep services purposeful
- Only introduce a service when logic spans multiple repositories or requires cross‑cutting concerns like caching or messaging.
- Do not wrap a single repository call in a service method; expose the repository directly instead.

## Design clean interfaces
- Pair interfaces with implementations (e.g., `IProductService`/`ProductService`) so features can be swapped or mocked.
- Keep methods cohesive and asynchronous. Suffix async methods with `Async` and accept a `CancellationToken` for I/O operations.
- Separate read models from commands when it improves clarity.

## Minimize dependencies
- Inject only required collaborators through the constructor and avoid the service locator pattern.
- Never inject controllers, Razor helpers, or other presentation types.
- Prefer working with domain models and repositories from `SmartDbContext`.

## Avoid service chains
- Services should be stateless and independent. When one service needs functionality from another, extract a shared helper or domain method.
- Fetch data in batches rather than looping over items and calling another service per item.

## Example implementation
A minimal service reveals the interface, uses asynchronous data access, caching, and the optional logger property:

```csharp
public interface IPriceService
{
    Task<decimal> GetPriceAsync(int productId, CancellationToken cancelToken = default);
}

public class PriceService : IPriceService
{
    private readonly IRepository<Product> _productRepo;
    private readonly ICache _cache;

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public PriceService(IRepository<Product> productRepo, ICache cache)
    {
        _productRepo = productRepo;
        _cache = cache;
    }

    public async Task<decimal> GetPriceAsync(int productId, CancellationToken cancelToken = default)
    {
        var cacheKey = $"product-price-{productId}";
        return await _cache.GetAsync(cacheKey, async () =>
        {
            var product = await _productRepo.GetByIdAsync(productId, cancelToken);
            return product.Price;
        });
    }
}
```

## Logging and caching
- Expose an optional logger so the container can inject a contextual instance:

  ```csharp
  public ILogger Logger { get; set; } = NullLogger.Instance;
  ```

- Use `ICache` for expensive queries and invalidate entries when underlying data changes.

## Testing
- Keep interfaces small to simplify mocking and unit testing.
- Avoid static state unless it is thread‑safe and intentionally shared.