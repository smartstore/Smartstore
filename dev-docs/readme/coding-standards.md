---
description: Coding conventions and style rules for Smartstore
---

# Coding standards

Consistency keeps the Smartstore codebase approachable and maintainable. This guide
summarizes the most important rules developers should follow when contributing code.

## Naming conventions

- **Types, methods, properties:** `PascalCase`.
- **Fields and locals:** `camelCase`; prefix private fields with `_`.
- **Interfaces:** prefix with `I` (e.g., `IPriceCalculator`).
- **Async methods:** suffix with `Async`.
- **Generics:** use `T` prefixes (`TContext`, `TEntity`).
- Avoid abbreviations; if necessary, capitalize like `HttpClient` or `DbContext`.

```csharp
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancelToken = default);
}
```

## Layout and formatting

- Indent with four spaces, never tabs.
- Brace style: **Allman** – opening braces on their own line.
- Place `using` directives at the top, sorted alphabetically with `System` first.
- Keep file length reasonable; split large classes into partials.
- Enable nullable reference types in every file: `#nullable enable`.

```csharp
namespace Smartstore.Shipping
{
    public class RateRequest
    {
        #nullable enable

        public Address? Destination { get; set; }
    }
}
```

## Guard clauses and exceptions

Validate arguments early to keep methods small and predictable. Throw
`ArgumentException` derivatives for bad input and domain‑specific exceptions for
business rules.

```csharp
public Task<Shipment> CreateShipmentAsync(Order order)
{
    Guard.NotNull(order);

    if (!order.Items.Any())
    {
        throw new InvalidOperationException("Order has no items.");
    }

    // ...
}
```

## Asynchronous programming

All I/O operations should be asynchronous.

- Use `async`/`await`; never block with `.Result` or `.Wait()`.
- Propagate `CancellationToken` parameters.
- Prefer value tasks for hot paths returning synchronously.

```csharp
public async Task<string> LoadAsync(int id, CancellationToken cancelToken)
{
    var entity = await _db.Entities.FindAsync([id], cancelToken);
    return entity?.Name ?? string.Empty;
}
```

## Dependency injection

Prefer constructor injection and keep services focused on a single responsibility.
Avoid service locators or static access. Controllers deriving from `SmartController`
receive `ICommonServices` via the `Services` property and do not need manual
injection.

```csharp
public class PriceController : SmartController
{
    public async Task<IActionResult> Index()
    {
        var customer = Services.WorkContext.CurrentCustomer;
        // ...
        return View();
    }
}
```

## Logging

Expose an optional logger property so DI can inject a contextual `ILogger`.

```csharp
public class ShippingService : IShippingService
{
    public ILogger Logger { get; set; } = NullLogger.Instance;

    // ...
}
```

## Client code

- Use Bootstrap utility classes where possible.
- Write JavaScript with progressive enhancement in mind and keep dependencies minimal.
- Keep comments short and in **English**.

Following these standards helps ensure that Smartstore remains a clean,
predictable, and enjoyable platform to work with.