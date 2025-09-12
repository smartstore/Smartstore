# DI best practices

Smartstore relies on [Autofac](https://autofac.readthedocs.io/en/latest/) for dependency injection but modules typically register services in a `Startup` class derived from `StarterBase`. Inside `ConfigureServices` you can access the ASP.NET Core `IServiceCollection` and the `IApplicationContext`:

```csharp
internal class Startup : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.Configure<MvcOptions>(o =>
        {
            o.Filters.AddEndpointFilter<StripeScriptIncludeFilter, PublicController>().WhenNonAjax();
        });

        if (appContext.IsInstalled)
        {
            services.AddScoped<StripeHelper>();
        }

        // override an existing service: last registration wins
        services.AddScoped<IPriceCalculationService, CustomPriceCalculationService>();
    }
}
```

The following tips help keeping components testable and scopes well defined.

## ICommonServices

`ICommonServices` bundles frequently used helpers such as `IWorkContext`, `StoreContext`, and the `SmartDbContext`. In controllers or view components deriving from `SmartController` or `SmartViewComponent`, it is property-injected and available via the `Services` member without constructor parameters:

```csharp
public class SampleController : SmartController
{
    public IActionResult Index()
    {
        var customer = Services.WorkContext.CurrentCustomer;
        return View();
    }
}
```

Inject it manually only in other classes that need many of its helpers to keep dependencies explicit.

## ILifetimeScopeAccessor

Use `ILifetimeScopeAccessor` to obtain or create scopes outside the request pipeline, e.g. for background jobs or CLI tools. `BeginContextAwareScope` returns a scope that reuses the HTTP scope when available or creates a new one otherwise.

```csharp
using var _ = _scopeAccessor.BeginContextAwareScope(out var scope);
var service = scope.Resolve<IMyService>();
```

## Work<T>

`Work<T>` lazily resolves services from the current scope. It is useful when a service is required only occasionally or within loops, avoiding unnecessary constructions.

```csharp
public class Sample
{
    private readonly Work<IMyService> _work;
    public Sample(Work<IMyService> work) => _work = work;

    public void DoSomething() => _work.Value.Execute();
}
```

By following these guidelines, modules stay lightweight and lifetimes remain predictable.