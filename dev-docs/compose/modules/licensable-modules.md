# ✔️ Licensable modules

## Overview

If you are offering your module on the [SmartStore Community Marketplace](http://community.smartstore.com/index.php?/files/), you can easily make it licensable. A licensed module behaves as follows:

* The module can be used for free for 30 days (demonstration mode). After this period, it has to be licensed.
* A shop administrator receives a license key after purchasing the module on the [SmartStore Marketplace](http://community.smartstore.com/index.php?/files/). To activate the license key, it has to be entered in the administration backend (_Plugins > Manage Plugins_).
* The license status is periodically checked to ensure that nobody is misusing your module.

{% hint style="info" %}
A license key is only valid for the IP address that activated the key.
{% endhint %}

## LicensableModule attribute <a href="#howtomakeapluginlicensable-thelicensablemoduleattribute" id="howtomakeapluginlicensable-thelicensablemoduleattribute"></a>

A module can be marked as a licensable piece of code by decorating the module class (inherited from `ModuleBase`) with the `LicensableModuleAttribute`.

{% hint style="info" %}
There must be a dependency on the _Smartstore.Web.Common_ project for the module to use the licensing component.
{% endhint %}

`LicensableModuleAttribute` has a `HasSingleLicenseForAllStores` option related to multi-stores. If set to _True_, the license key has no domain binding and is therefore valid for all stores. Recommended is the default value _False_, i.e. a new license key is required for each store.

## License check

The module should check the license status when executing important functions and disable or restrict them if there is no valid license. It is recommended not to do this permanently but throttled via `Throttle.Check`.

{% hint style="warning" %}
Always use the system name of the module to check a license, not of a provider!
{% endhint %}

```csharp
[LicensableModule]
internal class Module : ModuleBase, IConfigurable
{
    public static string SystemName => "MyCompany.MyModule";

    public async static Task<bool> CheckLicense(bool async)
    {
        // TODO: set key. Must be globally unique!
        var key = "any random characters";
        // Check once per hour.
        var interval = TimeSpan.FromHours(1);

        if (async)
        {
            return await Throttle.CheckAsync(key, interval, true,
                async () => await LicenseChecker.CheckStateAsync(SystemName) > LicensingState.Unlicensed);
        }
        else
        {
            return Throttle.Check(key, interval, true,
                () => LicenseChecker.CheckState(SystemName) > LicensingState.Unlicensed);
        }
    }
}
```

`LicensingState` is an `enum` with the following values:

<table><thead><tr><th width="146">State</th><th width="89" align="center">Value</th><th>Decsription</th></tr></thead><tbody><tr><td><strong>Unlicensed</strong></td><td align="center">0</td><td>Invalid license key or demo period expired.</td></tr><tr><td><strong>Demo</strong></td><td align="center">10</td><td>Demonstration period valid for 30 days. After expiration, the status changes to <em>Unlicensed</em>. An unlimited demonstration period is given to developers on <code>localhost</code>.</td></tr><tr><td><strong>Licensed</strong></td><td align="center">20</td><td>The license is valid.</td></tr></tbody></table>

{% hint style="info" %}
The license check is always performed against the current request URL or as a fallback against the current store URL (if there is no HTTP request object, which is usually the case in background tasks). If you want to check against another domain, you have to pass the corresponding URL via the second parameter of `CheckStateAsync`.
{% endhint %}

### License Checker

The license checker is a set of static methods for license validation within `Smartstore.Licensing.dll`.

<table><thead><tr><th width="232">Method</th><th>Description</th></tr></thead><tbody><tr><td><strong>IsLicensableModule</strong></td><td>Gets a value indicating whether a module is a licensed piece of code where the user has to enter a license key that has to be activated.</td></tr><tr><td><strong>GetLicense</strong></td><td>Gets information about a license for a given module system name such as the remaining demo days.</td></tr><tr><td><strong>Activate</strong></td><td>Activates a license key. This is done when the user enters their license key in the administration backend.</td></tr><tr><td><strong>Check</strong></td><td>Checks the state of a license for a given module system name and returns various information about the license. If you need a stringified version of the result (German and English localization supported), call the <code>ToString()</code> method of the returned object.</td></tr><tr><td><strong>CheckState</strong></td><td>Same as <em>Check</em> or <em>CheckAsync</em> but just returns the license state.</td></tr><tr><td><strong>ResetState</strong></td><td>The license status is cached internally. Only after a certain period a live check against the server is made. With this method, the cached status is reset and immediately checked again live.</td></tr></tbody></table>

### LicenseRequired filter attribute <a href="#howtomakeapluginlicensable-thelicenserequiredfilterattribute" id="howtomakeapluginlicensable-thelicenserequiredfilterattribute"></a>

You can decorate a whole controller class or just a single action method with the `LicenseRequiredAttribute`. This attribute calls `LicenseChecker.CheckAsync()` internally right before your action is processed, giving it the opportunity to block the action. If the license isn't valid, the _LicenseRequired_ view will be rendered by default, which you can override by setting the property `ViewName` on the attribute.

Alternatively, you could just display a notification for which you can use the properties `NotifyOnly`, `NotificationMessage` and `NotificationMessageType`. AJAX requests will be recognized automatically, and a suitable response will be generated according to the negotiated content type (either JSON or HTML).

If you want to block certain methods or even your entire module when it is in demo mode, you need to set the property `BlockDemo` to _True_. Otherwise everything will be accessible in demo mode, as the value of `BlockDemo` is _False_ by default. All properties of `LicenseRequiredAttribute`:

<table><thead><tr><th width="252">Property</th><th>Description</th></tr></thead><tbody><tr><td><strong>ModuleSystemName</strong></td><td>Gets or sets the module system name.</td></tr><tr><td><strong>LayoutName</strong></td><td>Gets or sets the name of the layout view for the <em>License Required</em> message. <em>~/Views/Shared/Layouts/_Layout.cshtml</em> by default.</td></tr><tr><td><strong>ViewName</strong></td><td>Gets or sets the name of the view for the <em>License Required</em> message. <em>LicenseRequired</em> by default.</td></tr><tr><td><strong>Result</strong></td><td><p>Gets or sets the action result if the module is in an unlicensed state. Possible values are:</p><ul><li><em>Block</em>: return a result with a license required warning.</li><li><em>NotFound</em>: return a not found result.</li><li><em>Empty</em>: return an empty result.</li></ul></td></tr><tr><td><strong>BlockDemo</strong></td><td>A value indicating whether to block the request if the license is in demo mode.</td></tr><tr><td><strong>NotifyOnly</strong></td><td>A value indicating whether to only output a notification message and not to replace the action result.</td></tr><tr><td><strong>NotificationMessage</strong></td><td>Gets or sets the message to output if the module is in an unlicensed state.</td></tr><tr><td><strong>NotificationMessageType</strong></td><td><p>Gets or sets the type of the notification message. Possible values are:</p><ul><li><em>error (Default)</em></li><li><em>success</em></li><li><em>info</em></li><li><em>warning</em></li></ul></td></tr></tbody></table>

## Usage scenario <a href="#howtomakeapluginlicensable-usagescenario" id="howtomakeapluginlicensable-usagescenario"></a>

Imagine you have developed a module to communicate with an ERP system. Furthermore, your module transmits data to a web service whenever an order is placed in your shop and consumes another web service to keep your product data up-to-date. If you decide to allow the product data to be updated completely in the demo mode of your plugin, it may be sufficient for the plugin user to import the product data only once. Therefore, you should interrupt the routine that's responsible for updating product data after a certain number of products have been updated. To do so, you would use the `CheckStateAsync()` method, which checks whether the state is _Demo_ and stops the routine accordingly (see code example 1). This way, the user can see a demonstration of the actual function without losing the motivation to purchase the full version.

Order events should, of course, be processed and transmitted to the ERP system completely for demonstration purposes, as it's way too difficult to keep track of the number of processed orders. However, when the demonstration period is over, no more orders should be processed. Therefore, you would use the `CheckStateAsync()` method to check whether the state is _Unlicensed_ and to stop the event accordingly (see code example 2).

{% code title="Code example 1" %}
```csharp
private async Task ProcessProducts()
{
    var state = await LicenseChecker.CheckStateAsync("MyCompany.MyModule");
    // ...
    if (state == LicensingState.Demo)
    {
        // Leave after 5 products if module is in demo mode.
        products = products.Take(5);
    }
    
    foreach (var product in products)
    {
        await UpdateProduct(product);
    }
    // ...
```
{% endcode %}

{% code title="Code example 2" %}
```csharp
public class Events : IConsumer
{
    public async Task HandleEventAsync(OrderPlacedEvent message)
    {
        var state = await LicenseChecker.CheckStateAsync("MyCompany.MyModule");
        if (state == LicensingState.Unlicensed)
            return;
        // ...
```
{% endcode %}

## Examples and special case <a href="#howtomakeapluginlicensable-examplesandspecialcases" id="howtomakeapluginlicensable-examplesandspecialcases"></a>

In background tasks there is no HTTP request object, so a license is checked against the current store URL. But for an export provider this is a bit inaccurate. The provider will want to check the license against the URL of the store whose data is being exported. It can use the `ExportExecuteContext` to get the URL.

```csharp
protected override async Task ExportAsync(ExportExecuteContext context,
    CancellationToken cancelToken)
{
    var license = await LicenseChecker.CheckAsync("MyCompany.MyModule",
        (string)context.Store.Url);

    if (license.State == LicensingState.Unlicensed)
    {
        context.Log.Error(HtmlUtility.ConvertHtmlToPlainText(
            license.ToString(), true, true));
        context.Abort = DataExchangeAbortion.Hard;
        return;
    }    
    // TODO: export...
}
```

Additionally, a payment provider can hide its payment methods at checkout if there is no active license. To enable this, you must override the `PaymentMethodBase.IsActive` property.

```csharp
public override bool IsActive
{
    get
    {
        try
        {
            var state = LicenseChecker.CheckState("MyCompany.MyModule");
            return (state != LicensingState.Unlicensed);
        }
        catch (Exception ex)
        {
            // _logger is an instance of ILogger
            _logger.Error(ex);
        }
        return true;
    }
}

// Or when using the mentioned throttle check simply:
public override bool IsActive
    => Module.CheckLicense(false).Await();
```
