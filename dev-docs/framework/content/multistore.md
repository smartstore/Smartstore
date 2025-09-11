---
description: Running multiple storefronts from a single Smartstore installation
---

# Multistore

## Overview
Smartstore can host several independent stores using one database and application instance. Each store has its own host name, theme, languages, currencies and catalog scope. Admins manage stores under **Configuration → Stores** and assign a primary domain to each one. Incoming requests are mapped to the matching store. If no match is found the first store becomes the default.

## IIS host mapping
To expose multiple stores on a single Windows server:

1. In **IIS Manager** create a site or additional binding for each store and point all of them to the same physical path of the Smartstore web application.
2. Set the **Host name** of the binding to the store’s domain.
3. In Smartstore admin under **Configuration → Stores**, create a store record and enter the same value in **Host values** (one per line if multiple domains should reach the store).

At runtime Smartstore reads the `Host` header from the request and selects the store whose host values contain that domain.

## Store context
At runtime the current store is exposed through `IStoreContext`. Inject the service where store information is needed.

```csharp
public class StoreBannerService
{
    private readonly IStoreContext _storeContext;

    public StoreBannerService(IStoreContext storeContext)
    {
        _storeContext = storeContext;
    }

    public string GetBannerText()
    {
        return $"Welcome to {_storeContext.CurrentStore.Name}";
    }
}
```

Controllers and view components deriving from `SmartController` or `SmartViewComponent` already expose `Services.StoreContext`.

## Store specific settings
Most settings can vary per store. Configuration pages show an *Override for store* checkbox next to each property. To read or persist a store-specific setting, pass the store identifier:

```csharp
var settings = await _settingService.LoadSettingAsync<MySettings>(storeId);
settings.SomeValue = "foo";
await _settingService.SaveSettingAsync(settings, x => x.SomeValue, storeId);
```

When `storeId` is `0`, the value applies to all stores.

You can also inject any settings class via Dependency Injection. The container automatically resolves the instance for the active store so no explicit `storeId` is necessary:

```csharp
public class CheckoutService
{
    private readonly OrderSettings _settings;

    public CheckoutService(OrderSettings settings)
    {
        _settings = settings; // values already match the current store
    }
}
```

MVC controller actions can load and persist settings automatically through the `LoadSetting` and `SaveSetting` filters. Decorate a GET action with `[LoadSetting]` to resolve settings and `[SaveSetting]` on the POST action to store updates for the selected store scope:

```csharp
[LoadSetting]
public IActionResult Configure(MySettings settings, int storeScope) => View(settings);

[HttpPost, SaveSetting]
public IActionResult Configure(MySettings settings, int storeScope)
{
    if (!ModelState.IsValid)
        return View(settings);

    return RedirectToAction(nameof(Configure));
}
```

See the [configuration guide](../platform/configuration.md) for more details on these attributes.

## Store mapping
Content entities (categories, manufacturers, topics, etc.) often implement `IStoreMappingSupported` so they can be limited to particular stores. Use `IStoreMappingService` to authorize access:

```csharp
var category = await _categoryService.GetByIdAsync(id);
if (!await _storeMappingService.AuthorizeAsync(category, _storeContext.CurrentStore.Id))
{
    return AccessDenied();
}
```

## Modules and themes
Modules and themes can be restricted to specific stores in the admin UI. This allows delivering different functionality and appearance per store while sharing the same code base.