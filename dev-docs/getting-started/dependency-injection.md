# ✔️ Dependency injection

## Overview

Smartstore uses Microsoft dependency injection (DI) to achieve inversion of control (IoC) between classes and their dependencies. Since Microsoft's DI lacks some features like adapters, decorators, registration sources etc. Autofac is used in addition. Internally, however, Autofac also works with Microsoft DI.

> "The idea behind inversion of control is that, rather than tie the classes in your application together and let classes "new up" their dependencies, you switch it around so dependencies are instead passed in during class construction."\
> — _Autofac_

A simple output writer example in the [Autofac](https://autofac.readthedocs.io/en/latest/getting-started/index.html#structuring-the-application) documentation illustrates the basic idea behind IoC and de-coupled architecture very well.

## Registering services

In order for the dependencies to be resolved, the corresponding service must be registered. The registration is done via a startup class inheriting from [StarterBase](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/Builders/StarterBase.cs). It should be declared as _internal_. In the Smartstore core, the startup classes are located in a bootstrapping folder of the related code section (for instance _DataExchange_). Override the `ConfigureContainer` method to add services using Autofac's `ContainerBuilder` or override the `ConfigureServices` method to add services using Microsoft's `IServiceCollection`. Either way, the resolved services end up in the same DI container.

```csharp
internal class DataExchangeStarter : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.AddDownloadManager();
    }

    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<ExportProfileService>().As<IExportProfileService>().InstancePerLifetimeScope();
        builder.RegisterType<ImportProfileService>().As<IImportProfileService>().InstancePerLifetimeScope();
        builder.RegisterType<DataExporter>().As<IDataExporter>().InstancePerLifetimeScope();
        builder.RegisterType<DataImporter>().As<IDataImporter>().InstancePerLifetimeScope();
        builder.RegisterType<MediaImporter>().As<IMediaImporter>().InstancePerDependency();
        // Additional code omitted for clarity.
    }
}
```

The `DataExchangeStarter` registers import and export related services, such as the `DataExporter` and `DataImporter`, using a type name and a dependency scope. This is the most common kind of registration, although Autofac's `ContainerBuilder` offers a number of [other possibilities](dependency-injection.md#special-registrations).

You can use `ConfigureServices` to conveniently register your own HTTP clients.

```csharp
public override void ConfigureServices(IServiceCollection services,
    IApplicationContext appContext)
{
    // Register a HTTP client to communicate with the PayPal API.
    services.AddHttpClient<PayPalHttpClient>()
        .AddSmartstoreUserAgent()
        .ConfigurePrimaryHttpMessageHandler(c => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip
        })
        .ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
}
```

{% hint style="info" %}
By convention the startup class of a module is called `Startup` and is located in the root of the module project. This gives modules a uniform structure with code that is easier to find.
{% endhint %}

### Dependency scopes

The most common dependency scopes are:

<table><thead><tr><th width="247">Via ContainerBuilder</th><th width="202">Via IServiceCollection</th><th>Results in</th></tr></thead><tbody><tr><td>InstancePerDependency</td><td>AddTransient</td><td>Every dependent component or call to <code>Resolve</code> gets a new, unique instance (default).</td></tr><tr><td>InstancePerLifetimeScope</td><td>AddScoped</td><td>Every dependent component or call to <code>Resolve</code> within a single <code>ILifetimeScope</code> - usually the HTTP request - gets the same, shared instance. Dependent components in different lifetime scopes will get different instances.</td></tr><tr><td>SingleInstance</td><td>AddSingleton</td><td>Every dependent component or call to <code>Resolve</code> gets the same, shared instance.</td></tr></tbody></table>

### Special registrations

Use Autofac's `ContainerBuilder` for special service registrations. For example, an [IIndexScopeManager](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/DefaultIndexScopeManager.cs) uses named metadata to support registrations of search index scopes like this:

```csharp
public override void ConfigureContainer(
    ContainerBuilder builder, 
    IApplicationContext appContext)
{
    builder.RegisterType<CatalogIndexScope>()
        .As<IIndexScope>()
        .Named<IIndexScope>("Catalog")
        .WithMetadata<IndexScopeMetadata>(m => m.For(em => em.Name, "Catalog"))
        .InstancePerLifetimeScope();

    builder.RegisterType<ForumIndexScope>()
        .As<IIndexScope>()
        .Named<IIndexScope>("Forum")
        .WithMetadata<IndexScopeMetadata>(m => m.For(em => em.Name, "Forum"))
        .InstancePerLifetimeScope();
}

public class DefaultIndexScopeManager : IIndexScopeManager
{
    private readonly IEnumerable<Lazy<IIndexScope, IndexScopeMetadata>> _scopes;

    public DefaultIndexScopeManager(IEnumerable<Lazy<IIndexScope, IndexScopeMetadata>> scopes)
    {
        _scopes = Guard.NotNull(scopes, nameof(scopes));
    }

    public IEnumerable<string> EnumerateScopes()
    {
        return _scopes.Select(x => x.Metadata.Name).OrderBy(x => x);
    }

    public IIndexScope GetIndexScope(string scope)
    {
        Guard.NotEmpty(scope, nameof(scope));

        var indexScope = _scopes.FirstOrDefault(x => x.Metadata.Name.EqualsNoCase(scope));
        if (indexScope == null)
        {
            throw new InvalidOperationException($"An index scope implementation for '{scope}' is not registered in the service container.");
        }

        return indexScope.Value;
    }
}
```

Components registered by key (a parameter of type `object`) can be resolved by a function delegate.

```csharp
builder.RegisterType<ProductImporter>().Keyed<IEntityImporter>(ImportEntityType.Product).InstancePerLifetimeScope();
builder.RegisterType<CategoryImporter>().Keyed<IEntityImporter>(ImportEntityType.Category).InstancePerLifetimeScope();
// More importers being registered...

builder.Register<Func<ImportEntityType, IEntityImporter>>(c =>
{
    var cc = c.Resolve<IComponentContext>();
    return key => cc.ResolveKeyed<IEntityImporter>(key);
});
```

Whenever a registered service is needed as part of a strongly-typed setting configuration, you need to use `IConfigureOptions<T>` or `IConfigureNamedOptions<T>`. By implementing these interfaces, you can configure an options object `T` using any required service from the DI container.

```csharp
builder.RegisterType<ODataOptionsConfigurer>()
    .As<IConfigureOptions<ODataOptions>>()
    .SingleInstance();

// Place class in Bootstrapping directory.
internal class ODataOptionsConfigurer : IConfigureOptions<ODataOptions>
{
    private readonly IApplicationContext _appContext;
    private ODataOptions _prevOptions;

    public ODataOptionsConfigurer(IApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Configure(ODataOptions options)
    {
        // "Resolve" required. Do not get via ctor.
        var settings = _appContext.Services.Resolve<WebApiSettings>();

        if (_prevOptions == null)
        {
            // Do initial configuration of "options".
            var modelProviders = _appContext.TypeScanner
                .FindTypes<IODataModelProvider>()
                .Select(x => (IODataModelProvider)Activator.CreateInstance(x))
                .ToList();
                
            var modelBuilder = new ODataConventionModelBuilder();

            foreach (var provider in modelProviders)
            {
                provider.Build(modelBuilder, 1);
            }

            var edmModel = modelBuilder.GetEdmModel();
            // TODO: more configuration. Additional code omitted for clarity.
        }
        else
        {
            // Is already configured.
            // TODO: apply updated settings etc. to configuration (if required).
        }
        
        _prevOptions = options;
    }
}
```

The above configuration must be retriggered if any changes are made to it. This can be done as follows:

```csharp
private readonly Lazy<IConfigureOptions<ODataOptions>> _odataOptionsConfigurer;
private readonly IOptions<ODataOptions> _odataOptions;

[HttpPost]
public async Task<IActionResult> Configure(ConfigurationModel model)
{
    // TODO: detect reconfigurationRequired somehow.
    bool reconfigurationRequired = true;
    if (reconfigurationRequired)
    {
        _odataOptionsConfigurer.Value.Configure(_odataOptions.Value);
    }
    //...
}
```

## Resolving services

After a service has been registered it can be resolved from the IoC container or from child lifetime scopes.

> "While it is possible to resolve components right from the root container, doing this through your application in some cases may result in a memory leak. It is recommended you always resolve components from a lifetime scope where possible to make sure service instances are properly disposed and garbage collected."\
> — _Autofac_

You can use Autofac's `ILifetimeScope` or Microsoft's `IServiceProvider` to resolve dependencies. In places where dependency resolution is often necessary, an instance of these is usually provided as a parameter. You can also get such an instance via [constructor injection](dependency-injection.md#constructor-injection).

{% hint style="warning" %}
**Never resolve scoped dependencies from IApplicationContext.Services**, because it is the root application services container! Only singleton dependencies should be resolved from it.

**Avoid resolving dependencies via EngineContext.Current.Scope** whenever possible. It can make writing unit tests difficult or even impossible.
{% endhint %}

If your code is always executed in the context of an HTTP request, dependencies can also be resolved the "ASP.NET way" using `HttpContext.RequestServices`. It grants access to the request's service container.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisallowRobotAttribute : Attribute, IAuthorizationFilter/*, IOrderedFilter*/
{
    // Additional code omitted for clarity.
    
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userAgent = context.HttpContext.RequestServices.GetRequiredService<IUserAgent>();
        if (userAgent.IsBot)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}
```

### Constructor injection

Constructor injection is the preferred way to resolve dependencies. Your component must be DI registered to use it.

```csharp
public partial class DataExportTask : ITask
{
    private readonly SmartDbContext _db;
    private readonly IDataExporter _dataExporter;
    private readonly IProviderManager _providerManager;

    public DataExportTask(
        SmartDbContext db,
        IDataExporter dataExporter,
        IProviderManager providerManager)
    {
        _db = db;
        _dataExporter = dataExporter;
        _providerManager = providerManager;
    }
    // Process using above dependencies...
}
```

{% hint style="info" %}
Only resolve what you really need. Avoid dependencies to components that themselves have many dependencies, such as `ICommonServices` (it is primarily intended for controllers). It can make unit testing work more difficult.
{% endhint %}

### Property injection

While constructor injection is the preferred method of passing dependencies to a component being constructed, you can also use the `PropertiesAutowired` method to have properties injected automatically.

{% hint style="info" %}
It is recommended to avoid property injection if possible and to use it only for special cases (like abstract classes) or very simple services (like `Logger` or `Localizer`). Auto injected properties must be public, although in most cases a component dependency should not be.
{% endhint %}

### "Work\<T>" dependency

Sometimes a dependency needs to be resolved when it is accessed for the first time, rather than when the component's constructor is called (e.g. when it is called at a very early stage, before the DI container can resolve services). A solution for this is the [Work](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/Work.cs) class. The dependency is resolved from `ILifetimeScope` whenever its `Value` property is accessed.

```csharp
public class MyComponent
{
    private readonly Work<ILanguageService> _languageService;

    public MyComponent(Work<ILanguageService> languageService)
    {
        // ILanguageService not resolved yet.
        _languageService = Guard.NotNull(languageService);
    }
    
    public void Process()
    {
        // ILanguageService resolved via "Value" property.
        var languages = _languageService.Value.GetAllLanguages();
    }
}
```

### Custom dependency scopes

Use the [ILifetimeScopeAccessor](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/ILifetimeScopeAccessor.cs) to create custom dependency scopes. The `DataImporter` uses this possibility to separate the dependencies of the respective importer from its own.

```csharp
while (context.Abort == DataExchangeAbortion.None && segmenter.ReadNextBatch())
{
    // _scopeAccessor is of type ILifetimeScopeAccessor.
    using var batchScope = _scopeAccessor.LifetimeScope.BeginLifetimeScope();

    // Apply changes made by TaskContextVirtualizer.VirtualizeAsync (e.g. required for checking permissions).
    batchScope.Resolve<IWorkContext>().CurrentCustomer = _services.WorkContext.CurrentCustomer;
    batchScope.Resolve<IStoreContext>().CurrentStore = _services.StoreContext.CurrentStore;

    var importerFactory = batchScope.Resolve<Func<ImportEntityType, IEntityImporter>>();
    var importer = importerFactory(profile.EntityType);

    await importer.ExecuteAsync(context, cancelToken);
}
```

The `batchScope` in this example, resides inside the segmenter loop and is disposed after the batch has been processed. This way, the entities loaded per batch (by the importer) are automatically removed from the Entity Framework's change tracker, freeing up memory and ensuring that nothing is left.
