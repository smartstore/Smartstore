# ✔️ Export

## Overview

Exports allow to exchange data between Smartstore and other organizations. Products can be displayed, compared and specifically advertised on the Internet to reach a wider audiences using a product export feed like Google Merchant Center.

Smartstore uses a data exporter, export profiles and export providers for this purpose.

* The [data exporter](export.md#data-exporter) collects the data and provides it in segments to an export provider.
* The [export provider](export.md#export-provider) converts the data provided by the data exporter into a specific format and writes it to a stream.
* [Export profiles](export.md#export-profile) are entities that bind the export to an export provider and offer settings and configuration.
* [Export deployments](export.md#deployment) are entities that can optionally be assigned to an export profile to specify how to proceed with the export files, for example to send them to an e-mail address.

When an export is executed, a task associated with the export profile is started, to perform the actual export via the data exporter and an export provider. The task can be scheduled using _cron expressions_ or triggered manually.

## Data exporter

The data exporter is an [IDataExporter](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Export/IDataExporter.cs) implementation and the main core component of the export infrastructure. Its purpose is to provide the [export providers](export.md#export-provider) with the export data in a high-performance way and to manage general tasks such as file management and data preview.

### Events

The [RowExportingEvent](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Events/RowExportingEvent.cs) is fired before an entity is exported. It can be used to attach and export additional data.

## Export provider

The provider implements [IExportProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Export/IExportProvider.cs) or it inherits from [ExportProviderBase](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Export/ExportProviderBase.cs).

{% hint style="info" %}
This documentation refers to a provider that inherits from `ExportProviderBase`.
{% endhint %}

The provider declares `SystemName`, `FriendlyName` and `Order` using attributes. It also specifies the file data format (e.g. CSV or XML).

{% hint style="info" %}
Set the property `FileExtension` to `null` if you do not want to export to files (_on-the-fly in-memory_ export).&#x20;
{% endhint %}

It is recommended to give your provider a friendly localized name and description using string resources and the localization XML files of your module:

<table><thead><tr><th width="209">Property to localize</th><th width="223">String resource name</th><th>Example</th></tr></thead><tbody><tr><td>Provider name</td><td>Plugins.FriendlyName.&#x3C;ProviderSystemName></td><td>Plugins.FriendlyName.Exports.MyCompanyProductCsv</td></tr><tr><td>Provider description</td><td>Plugins.Description.&#x3C;ProviderSystemName></td><td>Plugins.Description.Exports.MyCompanyProductCsv</td></tr></tbody></table>

{% hint style="info" %}
The description is displayed when adding a new export profile and selecting your provider.
{% endhint %}

`ExportAsync` is the main method to export data. Depending on the configuration it is called several times by the data exporter during an export. Typically, this happens once for each exported file, depending on the partition settings of the profile. Though it never encounters files at any time because file related aspects are all handled internally by the data exporter. The provider simply writes the data into a stream.

Use `ExportExecuteContext.CustomProperties` for any custom data required across the entire export. If additional files are required independently of the actual export files, they can be requested via `ExportExecuteContext.ExtraDataUnits`. The data exporter calls `OnExecutedAsync` for each extra data unit added by a provider.

{% hint style="info" %}
`OnExecutedAsync` is always called after the actual export, even if no extra data unit was requested. You can use it for all kinds of finalization work.
{% endhint %}

`ExportFeatures` flags are used to specify certain data processing and projection items supported by a provider. `CanProjectAttributeCombinations` for example indicates that the provider can export attribute combinations as products.

A simple export provider could look like this:

```csharp
[SystemName("Exports.MyCompanyProductCsv")]
[FriendlyName("MyCompany product CSV Export")]
public class MyCompanyProductExportProvider : ExportProviderBase
{
    public override ExportEntityType EntityType => ExportEntityType.Product;
    public override string FileExtension => "CSV";

    protected override async Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken)
    {
        using var writer = new CsvWriter(new StreamWriter(context.DataStream, Encoding.UTF8, 1024, true), CsvConfiguration.ExcelFriendlyConfiguration);

        writer.WriteField("Id");
        writer.WriteField("Name");
        // Write more columns...
        writer.NextRow();

        while (context.Abort == DataExchangeAbortion.None && await context.DataSegmenter.ReadNextSegmentAsync())
        {
            var segment = await context.DataSegmenter.GetCurrentSegmentAsync();

            foreach (dynamic product in segment)
            {
                if (context.Abort != DataExchangeAbortion.None)
                    break;

                Product entity = product.Entity;

                writer.WriteField(entity.Id.ToString());
                // Get and export localized product name from dynamic object.
                writer.WriteField((string)product.Name);
                // Write more fields...

                writer.NextRow();
                ++context.RecordsSucceeded;
            }
        }
    }
}
```

If done correctly, your provider will be displayed in the provider select box when adding a new export profile. See the [Google Merchant Center export provider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Modules/Smartstore.Google.MerchantCenter/Providers/GmcXmlExportProvider.cs) as another example.



### Provider specific configuration

You need to add a ViewComponent if you want to enable users to configure the export provider. To do this, override the `ExportProviderBase.ConfigurationInfo` method and provide both a `ComponentWidget` and a model type:

```csharp
public override ExportConfigurationInfo ConfigurationInfo => new()
{
    ConfigurationWidget = new ComponentWidget<MyConfigurationViewComponent>(),
    ModelType = typeof(MyProviderConfigurationModel)
};

protected override async Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken)
{
    var config = (context.ConfigurationData as MyProviderConfigurationModel) ?? new MyProviderConfigurationModel();
    // ...
}
```

{% hint style="info" %}
Your configuration model must be decorated with `Serializable` and `CustomModelPart` attributes so that it can be saved with the export profile.
{% endhint %}

```csharp
[Serializable, CustomModelPart]
[LocalizedDisplay("Plugins.MyCompany.MyModule.")]
public class MyProviderConfigurationModel
{
    [LocalizedDisplay("*MyConfigProperty")]
    public string MyConfigProperty { get; set; }
    //...
}
```

Your configuration will be displayed in the **Configuration** tab on the export profile edit page.

### Export data

The export data is provided in segments by the data exporter. A data item is a dynamic object of type [DynamicEntity](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Export/Internal/DynamicEntity.cs) which wraps the actual entity and has extra properties attached to it. The entity is accessible via `dynamicObject.Entity`. Additional data is generally prefixed with an underscore, e.g. `dynamicObject._BasePriceInfo`. See the [appendix](export.md#properties-of-dynamicentity) for a complete list.

Projection and configuration of the export profile is applied to the `DynamicEntity`. If a certain language is selected in the profile's projection tab, the `DynamicEntity` would contain the localized property values (e.g. a localized product name) instead of the actual property value of the entity.

### Export related data

All export profiles have the`ExportRelatedData` option. If activated and the provider supports `ExportFeatures.UsesRelatedDataUnits`, the data exporter adds additional data units to `ExportExecuteContext.ExtraDataUnits`. The provider can use them to export related data into a separate file, like additionally exporting tier prices utilizing a product export provider.

At the moment only _tier prices_, _variant attribute values_ and _variant attribute combinations_ are supported when exporting products, see `RelatedEntityType`. This mechanism is mainly intended to update prices via flat data formats such as CSV, that can then be edited by end users.

{% hint style="info" %}
Related data files are automatically imported together with the main data file(s) using file naming conventions. If the name of the related data file ends with a `RelatedEntityType` value (e.g. `TierPrice` or `ProductVariantAttributeCombination`), the product importer uses its data to update _tier prices_, _variant attribute values_ or _variant attribute combinations_.
{% endhint %}

## Export profile

Export profiles combine all aspects of an export, making them configurable by the user:

* Provider
* Task
* Partition
* Filters
* Projections
* Configurations
* Deployments

There are two types of profiles used in Smartstore:

* Built-in system profiles, used among others, for exporting orders via the backend order grid.
* User profiles, added subsequently by the user.

{% hint style="info" %}
An export provider can be assigned to several profiles with different configurations and settings.
{% endhint %}

To manage the export profiles use [IExportProfileService](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Export/IExportProfileService.cs). For instance, if you want to delete all profiles that your export provider is assigned to, when your module is uninstalled. Use the `ExportProfileInfoViewComponent` to display a list of all profiles your provider is assigned to. It renders a link to the profile and task, information about the last execution and a button to start the export:

```csharp
@await Component.InvokeAsync("ExportProfileInfo",
    new { providerSystemName = "Exports.MyCompanyProductCsv" })
```

### Deployment

_Publishing profiles_ are implementations of [IFilePublisher](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Export/Deployment/IFilePublisher.cs) and define how to process the export files. The built-in profiles publish via:

* File system
* Email
* HTTP POST
* FTP
* Public folder

{% hint style="info" %}
Any number of publishing profiles can be assigned to an export profile.
{% endhint %}

After a successful export the data exporter instantiates every publisher associated with the export profile and calls its `PublishAsync` method together with the [ExportDeployment](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/DataExchange/Domain/ExportDeployment.cs) entity containing all details of the deployment.

## Data grid and exports

Sometimes it's useful to show a button or menu above a data grid, to let the user export all or selected orders via the orders grid immediately.

To implement this you need to add an `IResultFilter` to register your `PartialViewWidget` or view component:

```csharp
internal class Startup : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.Configure<MvcOptions>(o =>
        {
            o.Filters.AddConditional<MyOrdersGridToolbarFilter>(
                context => context.ControllerIs<OrderController>(x => x.List()) && !context.HttpContext.Request.IsAjax());
        });
    }
}

public class MyOrdersGridToolbarFilter : IResultFilter
{
    private readonly IPermissionService _permissionService;
    private readonly Lazy<IWidgetProvider> _widgetProvider;

    public MyOrdersGridToolbarFilter(
        IPermissionService permissionService,
        Lazy<IWidgetProvider> widgetProvider)
    {
        _permissionService = permissionService;
        _widgetProvider = widgetProvider;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result.IsHtmlViewResult() 
            && _permissionService.Authorize(Permissions.Configuration.Export.Execute))
        {
            _widgetProvider.Value.RegisterWidget("datagrid_toolbar_omega",
                new PartialViewWidget("_MyOrdersGridExportMenu", null, "MyCompany.MyModule"));
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
```

Your partial view with the toolbar menu may look like this:

```cshtml
<div class="dg-toolbar-group">
    <div class="dropdown">
        <button type="button" class="btn btn-light btn-flat dropdown-toggle" 
            data-toggle="dropdown" data-boundary="window">
            @T("Plugins.MyCompany.MyModule.Export")
        </button>
        <div class="dropdown-menu">
            <a href="javascript:;" class="dropdown-item mycompany-order-export 
               export-selected-data"
               v-bind:class="{ disabled: !grid.hasSelection }"
               data-grid-id="orders-grid"
               data-url="@Url.Action("Export", "MyController", new { all=false })">
                <i class="fa fa-fw fa-code"></i>
                <span>@T("Plugins.MyCompany.MyModule.ExportSelected")</span>
            </a>

            <a href="javascript:;" class="dropdown-item mycompany-order-export"
               data-url="@Url.Action("Export", "MyController", new { all=true })">
                <i class="fa fa-fw fa-code"></i>
                <span>@T("Plugins.MyCompany.MyModule.ExportAll")</span>
            </a>
        </div>
    </div>
</div>

<script sm-target-zone="scripts" data-origin="mycompany-order-export">
    $(function () {
        $('.mycompany-order-export').on('click', function (e) {
            e.preventDefault();

            var link = $(this);
            var selectedIds = '';

            if (!link.hasClass('disabled')) {
                if (link.hasClass('export-selected-data')) {
                    var elGrid = $('#' + link.data('grid-id'));

                    if (!elGrid.length) {
                        alert2('Cannot find order grid.');
                        return false;
                    }

                    selectedIds = elGrid.parent().data('datagrid').selectedRowKeys;
                }

                $({}).postData({
                    url: link.data('url'),
                    data: selectedIds != "" ? { selectedIds } : null,
                    ask: @T("Admin.Common.AskToProceed").JsValue,
                });
            }

            return false;
        });
    });
</script>
```

## Appendix

### Properties of DynamicEntity

#### All entity types

<table><thead><tr><th width="167">Property name</th><th width="148">Type</th><th>Description</th></tr></thead><tbody><tr><td>_Localized</td><td>List&#x3C;dynamic></td><td><p>List of all localized values of all languages for an entity. <code>null</code> if the entity has no localized properties. Properties of an item are:</p><ul><li><em>Culture</em>: the language culture.</li><li><em>LocaleKey</em>: the key of the localized value (usually the property name of the entity).</li><li><em>LocaleValue</em>: the localized value.</li></ul></td></tr></tbody></table>

#### Products

<table><thead><tr><th width="266">Property name</th><th width="153">Type</th><th>Description</th></tr></thead><tbody><tr><td>_UniqueId</td><td>string</td><td>A unique ID consisting of product ID and attribute combination ID, if an attribute combination is exported as a product.</td></tr><tr><td>_IsParent</td><td>bool</td><td>A value indicating whether the product is a <strong>parent</strong> to exported attribute combinations, considered to be their <strong>children</strong>.</td></tr><tr><td>_AttributeCombinationId</td><td>int</td><td>The attribute combination ID, if exported as a product.</td></tr><tr><td>_AttributeCombinationValues</td><td>IList&#x3C;ProductVariantAttributeValue></td><td>A list of all attribute values if an attribute combination is exported as a product.</td></tr><tr><td>_Brand</td><td>string</td><td>Name of the first assigned manufacturer.</td></tr><tr><td>_CategoryName</td><td>string</td><td>Name of the first assigned category.</td></tr><tr><td>_CategoryPath</td><td>string</td><td>The breadcrumb (path) of the first assigned category.</td></tr><tr><td>_DetailUrl</td><td>string</td><td>URL to the product detail page.</td></tr><tr><td>_Price</td><td>CalculatedPrice</td><td>The calculated product price.</td></tr><tr><td>_BasePriceInfo</td><td>string</td><td>Base price information of the product.</td></tr><tr><td>_MainPictureUrl</td><td>string</td><td>Absolute URL of the product's main picture.</td></tr><tr><td>_MainPictureRelativeUrl</td><td>string</td><td>Relative URL of the product's main picture.</td></tr><tr><td>_FreeShippingThreshold</td><td>decimal?</td><td>Free shipping threshold taken from the projection settings.</td></tr><tr><td>_ShippingCosts</td><td>decimal?</td><td>Shipping costs or the projected costs, if the product price is greater or equal to the free shipping threshold.</td></tr><tr><td>_ShippingTime</td><td>string</td><td>Delivery time name or projected time if none is specified for the product.</td></tr><tr><td>_ProductTemplateViewPath</td><td>string</td><td>The view path of the assigned product template.</td></tr></tbody></table>

#### Customers

<table><thead><tr><th width="269">Property name</th><th width="147">Type</th><th>Description</th></tr></thead><tbody><tr><td>_FullName</td><td>string</td><td>Full customer name.</td></tr><tr><td>_AvatarPictureUrl</td><td>string</td><td>Absolute URL of the avatar, if it exists.</td></tr><tr><td>_RewardPointsBalance</td><td>int</td><td>Current reward point balance.</td></tr><tr><td>_HasNewsletterSubscription</td><td>bool</td><td>A value indicating whether the customer is subscribed to the newsletter.</td></tr><tr><td>_GenericAttributes</td><td>List&#x3C;dynamic></td><td>List of associated generic attributes. Only <em>VatNumber</em> and <em>ImpersonatedCustomerId</em> are exported. The dynamic items are of the type <code>GenericAttribute</code>.</td></tr></tbody></table>

#### MediaFile

<table><thead><tr><th width="270">Property name</th><th width="146">Type</th><th>Description</th></tr></thead><tbody><tr><td>_FileName</td><td>string</td><td>Name of the file.</td></tr><tr><td>_RelativeUrl</td><td>string</td><td>Relative URL.</td></tr><tr><td>_ThumbImageUrl</td><td>string</td><td>Absolute URL of the thumbnail.</td></tr><tr><td>_ImageUrl</td><td>string</td><td><p>Absolute URL of the image in the size shown on a detail page.</p><p>If not available, the thumbnail is used.</p></td></tr><tr><td>_FullSizeImageUrl</td><td>string</td><td>Absolute URL of the full sized image.</td></tr></tbody></table>
