---
description: Start and initialize the application
---

# ✔️ Bootstrapping

## Overview

Bootstrapping is the process of starting and initializing the application. This can take some time (anywhere from 2 to 10 seconds), depending on the quality of the hardware and the number of loaded modules. When the application shuts down, the very first request usually triggers the application to start.

During the application startup, the following actions take place:

* All **core assemblies** are loaded into the application domain.
* All **installed module assemblies** are detected and loaded into the application domain.
* All **services** are registered in the DI service container.
* The HTTP request **pipeline** is configured.
* Route **endpoints** are mapped.

In a traditional ASP.NET Core application, these actions are performed in `Program.cs` (or `Startup.cs` in earlier versions of ASP.NET), but this is no option for Smartstore because external modules need to hook into the bootstrapping process. This is where modular _Starters_ come into play.

## Modular starters

The application core only contains a very slim bootstrapper (similar to a kernel). After all module assemblies are loaded into the application domain, the type scanner looks for concrete subclasses of the [IStarter](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/Builders/IStarter.cs) interface in all assemblies. The starters are sorted and executed one after the other.

{% hint style="info" %}
Each project can have any number of starter classes or none. There are no restrictions at all.
{% endhint %}

### IStarter interface

Here is the definition of the `IStarter` interface:

```csharp
public interface IStarter : ITopologicSortable<string>
{
    int Order { get; }

    // Allow or suppress starter execution based on some 
    // conditions like app installation state for instance
    bool Matches(IApplicationContext appContext);

    // Add services to the container
    void ConfigureServices(IServiceCollection services, IApplicationContext appContext);

    // Configure MVC services
    void ConfigureMvc(IMvcBuilder mvcBuilder, IServiceCollection services, IApplicationContext appContext);
    
    // Configure the application's request pipeline with precise middleware ordering.
    void BuildPipeline(RequestPipelineBuilder builder);
    
    // Register endpoint routes
    void MapRoutes(EndpointRoutingBuilder builder);
}
```

### StarterBase abstract class

In Smartstore, the [StarterBase](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/Builders/StarterBase.cs) abstract class is used for convenience. It implements the `IStarter` interface with virtual overridable methods, so your starter should be derived from this class instead of the `IStarter` interface.

Besides the  `ConfigureServices` method, `StarterBase` class also provides the overridable `ConfigureContainer` method. It does the same, but in the `Autofac` way, using `ContainerBuilder` instead of `IServiceCollection`. it doesn’t matter if you override none, one or both.

{% hint style="info" %}
Following the convention, we:

* Place the `Startup.cs` file in the root of the module project.
* Name the class `Startup`.
* Derive it from `StarterBase` abstract class.
* Make it _internal_.
{% endhint %}

### Conditional execution

For conditional execution of the starter, override the `StarterBase.Matches()` method and return a value indicating whether the starter should be executed or skipped. This is useful, when you want to allow / suppress starter execution based on the application installation state.

```csharp
internal class Startup : StarterBase
{
    // Should NOT run when app is not fully installed yet
    public override bool Matches(IApplicationContext appContext)
        => appContext.IsInstalled;
}
```

### Order of execution

By default, starters are executed in the order they were detected by the type scanner (first core assemblies, then module assemblies, and so on). This is because the `StarterBase` class assigns the static `StarterOrdering` class’s `Default` value to the `IStarter.Order` property by default. However, this value can be overridden in your starter implementation.

If there are two starters with the same `Order` value and it is necessary to explicitly specify the order of execution, the `StarterBase.RunAfter()` method is used. Here is an example of such an implementation:

```csharp
internal class Startup : StarterBase
{
    public override int Order => StarterOrdering.BeforeStaticFilesMiddleware;

    public Startup()
    {
        RunAfter<MyFirstModule>();
    }
} 
```

### Middleware and Endpoint ordering

Sometimes even precise starter ordering is not enough. Middleware and endpoints require a bit more control, for example, if you need to be able to **precisely** define the order of a middleware component _within_ the request pipeline. Imagine you have developed two middleware components in a single module. One must come `BeforeStaticFilesMiddleware` and the other one `AfterRoutingMiddleware`. In Smartstore, you can accomplish this in the following way:

```csharp
internal class Startup : StarterBase
{
    public override void BuildPipeline(RequestPipelineBuilder builder)
    {
        builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
        {
            app.UseMiddleware<MyFirstMiddleware>();
        });
    
        builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
        {
            app.UseMiddleware<MySecondMiddleware>();
        });
    }
}
```

The static [StarterOrdering](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/Builders/StarterOrdering.cs) class comes in very handy here. It defines numerous constants that represent the order of well-known middleware components (like StaticFiles, Routing, Authentication, ExceptionHandlers, ...). You just need to hook-in before or after a component.

### Startup class full implementation example

{% code title="Starter.cs" %}
```csharp
internal class Startup : StarterBase
{
    public Startup() 
    {
        RunAfter<MvcStarter>();
    }
    
    // Should NOT run when app is not fully installed yet
    public override bool Matches(IApplicationContext appContext)
        => appContext.IsInstalled;
    
    public override void ConfigureServices(
        IServiceCollection services, 
        IApplicationContext appContext)
    {
        // Override "ConfigureServices" for things that only
        // ASP.NET DI can do, like option configuration
        services.Configure<SomeOptions>(o => 
        {
            // ... configure options
        });
    }
    
    public override void ConfigureMvc(
        IMvcBuilder mvcBuilder, 
        IServiceCollection services, 
        IApplicationContext appContext)
    {
        // MVC configuration could be done in "ConfigureServices",
        // but isn't this well organized? :-)
        
        // ... configure some MVC stuff
    }
    
    public override void ConfigureContainer(
        ContainerBuilder builder, 
        IApplicationContext appContext)
    {
        // You can't do this in "ConfigureServices", because
        // ASP.NET DI does not support registration sources,
        // decorators, adapters, metadata, Lazy<>, container, modules etc.
        builder.RegisterSource(new MyAutofacRegistrationSource());
    }
    
    public override void BuildPipeline(RequestPipelineBuilder builder)
    {
        builder.Configure(StarterOrdering.BeforeStaticFilesMiddleware, app =>
        {
            app.UseMiddleware<MyFirstMiddleware>();
        });
    
        builder.Configure(StarterOrdering.AfterRoutingMiddleware, app =>
        {
            app.UseMiddleware<MySecondMiddleware>();
        });
    }
    
    public override void MapRoutes(EndpointRoutingBuilder builder)
    {
        if (builder.ApplicationContext.IsInstalled)
        {
            builder.MapRoutes(StarterOrdering.LateRoute, endpoints =>
            {
                endpoints.MapBlazorHub();
            });
        };
    }
}
```
{% endcode %}

## Initializers

Implementations of [IApplicationInitializer](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/Initialization/IApplicationInitializer.cs) are used to execute _application initialization code_ during the very **first HTTP request** and **very early** in the request lifecycle. This distinguishes them from starters that are executed earlier (before `HttpContext` is initialized).

But some initialization logic like `HttpContext` needs a valid scope to resolve services from. Since you can't just access scoped or transient dependencies in a starter, unless you spawn a custom dependency scope, which is a very bad idea and pure evil 😄.

{% hint style="info" %}
We won't cover this topic here, as plenty of material on the subject can be found online.
{% endhint %}

By default, an initializer is executed only once unless you specify a higher value in the `MaxAttempts` property. However, this setting has no effect if `ThrowOnError` is set to _true._ The`ThrowOnError` property indicates whether to throw any error and stop execution of subsequent initializers. If the value is _false_, the initializer will be executed and `OnFailAsync` is invoked to give you the chance to do some logging and fix things.

{% hint style="info" %}
There is no need to register an initializer in the DI, as all types implementing `IApplicationInitializer` are detected automatically and resolved during the application initialization. Thus, initializers can take any dependency.
{% endhint %}

### Smartstore built-in initializers

<table><thead><tr><th width="286">Initializer</th><th>Description</th></tr></thead><tbody><tr><td><a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Data/Bootstrapping/ApplicationDatabasesInitializer.cs">ApplicationDatabasesInitializer</a></td><td><p>Initializes the application database(s).</p><p>It’s the very first initializer to run.</p></td></tr><tr><td><a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Scheduling/Bootstrapping/TaskSchedulerInitializer.cs">TaskSchedulerInitializer</a></td><td><p>Activates the web scheduler after checking for valid hostnames.</p><p>Returns a warning if no scheduler or store is registered.</p></td></tr><tr><td><a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Security/Bootstrapping/InstallPermissionsInitializer.cs">InstallPermissionsInitializer</a></td><td>Checks for new permission records and seeds them.</td></tr><tr><td><a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Modularity/ModulesInitializer.cs">ModulesInitializer</a></td><td>Among other things, it detects and refreshes changed module locale resources.</td></tr></tbody></table>
