# ✔️ Modularity & Providers

## Providers

A provider is a design pattern to integrate and swap a code extension more easily and flexibly. A good example are payment methods. If a developer wants to implement multiple payment methods of one payment company, he can create a single module (representing the payment company), which contains a provider for each payment method. Besides [IPaymentMethod](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Checkout/Payment/Service/IPaymentMethod.cs), there are other providers like [IExportProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Export/IExportProvider.cs), [IShippingRateComputationMethod](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Checkout/Shipping/Services/IShippingRateComputationMethod.cs), [IExchangeRateProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Common/Services/IExchangeRateProvider.cs), [IExternalAuthenticationMethod](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Identity/Services/IExternalAuthenticationMethod.cs) etc.

Each of these provider interfaces is derived from the marker interface [IProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Engine/Modularity/IProvider.cs) in order to be able to identify it uniformly as a provider. The [generic provider class](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Modularity/Provider.cs) encapsulates the respective interface and enriches it with general properties like [ProviderMetadata](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Modularity/ProviderMetadata.cs). As a result you get an abstract, handy and API friendly construct such as `Provider<IPaymentMethod>`.

### Metadata

<table><thead><tr><th width="202">Name</th><th width="219">Implement</th><th>Description</th></tr></thead><tbody><tr><td><strong>FriendlyName</strong></td><td>FriendlyNameAttribute</td><td>The English name of the provider. Localizable by using resource key <em>Plugins.FriendlyName.&#x3C;ProviderSystemName></em></td></tr><tr><td><strong>Description</strong></td><td>FriendlyNameAttribute</td><td>The English description of the provider. Localizable by using resource key <em>Plugins.Description.&#x3C;ProviderSystemName></em></td></tr><tr><td><strong>DisplayOrder</strong></td><td>OrderAttribute</td><td>The display order in the providers list.</td></tr><tr><td><strong>DependentWidgets</strong></td><td>DependentWidgetsAttribute</td><td>Widgets which are automatically (de)activated when the provider gets (de)activated. Useful in scenarios where separate widgets are responsible for displaying provider data.</td></tr><tr><td><strong>ExportFeatures</strong></td><td>ExportFeaturesAttribute</td><td>Data processing types supported by an export provider.</td></tr><tr><td><strong>IsConfigurable</strong></td><td>IConfigurable</td><td>A value indicating whether the provider is <a href="modularity-and-providers.md#configuration">configurable</a>.</td></tr><tr><td><strong>IsEditable</strong></td><td>IUserEditable</td><td>A value indicating whether the metadata is editable by the user.</td></tr><tr><td><strong>IsHidden</strong></td><td>IsHiddenAttribute</td><td>A value indicating whether the provider is hidden. A hidden provider can only be used programmatically but not by the user through the user interface.</td></tr><tr><td><strong>SystemName *</strong></td><td>SystemNameAttribute</td><td>Unique SystemName of the provider, e.g. <em>Payments.AmazonPay.</em></td></tr></tbody></table>

{% hint style="info" %}
A provider can be activated or deactivated using the provider list. For example, a deactivated payment provider does not appear in the checkout payment method list.
{% endhint %}

### Configuration

Providers are configurable via `IConfigurable`. It specifies a `RouteInfo` that points to an action method used for configuration.

```csharp
public RouteInfo GetConfigurationRoute()
    => new(nameof(MyModuleAdminController.Configure), "MyModuleAdmin", new { area = "Admin" });
```

Typically the configuration consists of two _Configure_ methods, one called by `HttpGet` to load the configuration and one called by `HttpPost` to save it. Since we are configuring a provider and its configuration view is just a portion of the actual Razor layout page, we need to tell it what provider we are configuring. This is done via the metadata and `ViewBag.Provider`.

```csharp
private readonly IProviderManager _providerManager;

[LoadSetting]
public IActionResult Configure(MyProviderSettings settings)
{
    var model = MiniMapper.Map<MyProviderSettings, MyConfigurationModel>(settings);
    
    ViewBag.Provider = _providerManager.GetProvider("MyProviderSystemName").Metadata;
    
    return View(model);
}
```

Use [IProviderManager](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Modularity/IProviderManager.cs) to load any provider (including its metadata) or all providers of a certain type. Since we are configuring settings inherited from `ISetting`, we can use the `LoadSettingAttribute` to load an instance of our settings as an action parameter. The `HttpPost` method for saving may look like this:

```csharp
[HttpPost, SaveSetting]
public IActionResult Configure(MyConfigurationModel model,
    MyProviderSettings settings)
{
    if (!ModelState.IsValid)
    {
        return Configure(settings);
    }

    MiniMapper.Map(model, settings);
    
    return RedirectToAction(nameof(Configure));
}
```

The `SaveSettingAttribute` automatically saves the updated settings to database. The configuration view may look like this:

```cshtml
@model MyConfigurationModel

@{
    Layout = "_ConfigureProvider";
}

<widget target-zone="admin_button_toolbar_before">
    <button id="SaveConfigButton" type="submit" name="save" value="save"
        class="btn btn-warning">
        <i class="fa fa-check"></i>
        <span>@T("Admin.Common.Save")</span>
    </button>
</widget>

@await Component.InvokeAsync("StoreScope")

<form asp-action="Configure">
    <div asp-validation-summary="All"></div>
    @* TODO: multi-store configuration using setting-editor tag helper. *@
</form>
```
