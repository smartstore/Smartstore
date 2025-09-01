# Logging

## Overview
Smartstore uses Microsoft.Extensions.Logging with [Serilog](https://serilog.net/) for structured logging. Configuration lives in `appsettings.json` and can be overridden in `Config/usersettings.json`. Each log event is enriched with request data such as IP address, URL, customer ID, and user name. Events are written to rolling text files under `App_Data/Logs` and to the database table `Log` via a custom Serilog sink. Logs are viewable in the admin area under **System > Log**.

## Writing log entries
Inject `ILogger<T>` into any service or controller to write structured messages:

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.LogInformation("Processing started");
    }
}
```

Smartstore can also inject a logger via a public property. Add the property with a
default [`NullLogger`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.abstractions.nulllogger) so it is
always non-null and the container will replace it with a contextual logger at runtime:

```csharp
public ILogger Logger { get; set; } = NullLogger.Instance;
```

Controllers inheriting from `SmartController` already declare this `Logger` property, so you usually only add it to custom services that need optional logging without constructor injection.

## Request logging
The platform enables request logging middleware through `UseRequestLogging()`. It logs each HTTP request once with method, path, status code, elapsed time, and user context:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseRequestLogging();
    // other middleware
}
```

## Activity log
`IActivityLogger` records high-level user actions such as logins, checkouts, or admin changes. Entries are stored in the `ActivityLog` table and surfaced in the admin UI:

```csharp
_activityLogger.LogActivity(KnownActivityLogTypes.EditSettings,
    "Settings updated for store {0}", store.Name);
```

## Notifications
Use `INotifier` to queue toast messages for the current request. They appear on the next page load:

```csharp
_notifier.Add(NotifyType.Success, "Order processed successfully");
```
