# ✔ Creating an Export provider

Export providers allow you to export your store data in many different formats. Smartstore mainly uses CSV and XML. In this tutorial you will write an export provider for the product catalog.

{% hint style="info" %}
To learn more about exporting, see [Export](../../../framework/platform/export.md).
{% endhint %}

## Create a configuration

If you want make your export provider customizable, you will need to add some resources. This step is optional. There are three things to configure:

1. The `ProfileConfigurationModel` that describes the configurable data.
2. The `HelloWorldConfigurationViewComponent`, which converts stored data into usable formats for your view.
3. The view shown to the user. This acts like a widget view and must adhere to the same directory structure.

This configuration allows you to limit the number of rows that are exported.

### The Model

Create the `ProfileConfigurationModel.cs` class and add it to the _Models_ directory.

```csharp
namespace MyOrg.HelloWorld.Models
{
    [Serializable, CustomModelPart]
    [LocalizedDisplay("Plugins.MyOrg.HelloWorld.")]
    public class ProfileConfigurationModel
    {
        [LocalizedDisplay("*NumberOfExportedRows")]
        public int NumberOfExportedRows { get; set; } = 10;
    }
}
```

Add the resources to your localization file.

```xml
<LocaleResource Name="NumberOfExportedRows">
    <Value>Number of rows</Value>
</LocaleResource>
<LocaleResource Name="NumberOfExportedRows.Hint">
    <Value>Number of rows to be exported.</Value>
</LocaleResource>
```

### The ViewComponent

Create the `HelloWorldConfigurationViewComponent.cs` class and add it to the _Components_ directory.

```csharp
namespace MyOrg.HelloWorld.Components
{
    public class HelloWorldConfigurationViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(object data)
        {
            var model = data as ProfileConfigurationModel;
            return View(model);
        }
    }
}
```

### The View

Create a Razor view `Default.cshtml` and add it to _Views / Shared / Components / HelloWorldConfiguration_.

```cshtml
@model ProfileConfigurationModel

@{
    Layout = null;
}

<div class="adminContent">
    <div class="adminRow">
        <div class="adminTitle">
            <smart-label asp-for="NumberOfExportedRows" />
        </div>
        <div class="adminData">
            <input asp-for="NumberOfExportedRows" />
            <span asp-validation-for="NumberOfExportedRows"></span>
        </div>
    </div>
</div>
```

## Add export providers

### The CSV export provider

Create the `HelloWorldCsvExportProvider.cs` class and add it to the new _Providers_ directory.

```csharp
namespace MyOrg.HelloWorld.Providers
{
    public class HelloWorldCsvExportProvider : ExportProviderBase
    {
        protected override Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken)
        {
        }
    }
}
```

This class inherits from the abstract base class `ExportProviderBase`, so you must override the `ExportAsync` method.

#### Add Attributes

Smartstore uses the following attributes to correctly integrate the providers: `SystemName`, `FriendlyName` and the optional `ExportFeatures`.

Add these attributes to your class definition.

```csharp
[SystemName("MyOrg.HelloWorld.ProductCsv")]
[FriendlyName("Hello world CSV product feed")]
[ExportFeatures(Features =
    ExportFeatures.CreatesInitialPublicDeployment |
    ExportFeatures.OffersBrandFallback)]
```

{% hint style="info" %}
`SystemName` is the system name of the provider, not the system name of the module.
{% endhint %}

| Export feature                  | Description                                                                                                                                                               |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CreatesInitialPublicDeployment  | Automatically create a file-based public deployment when an export profile is created.                                                                                    |
| CanOmitGroupedProducts          | Offer option to include/exclude grouped products.                                                                                                                         |
| CanProjectAttributeCombinations | Provide the ability to export attribute combinations as products.                                                                                                         |
| CanProjectDescription           | Offer more options to manipulate the product description.                                                                                                                 |
| OffersBrandFallback             | Offer the option to enter a brand fallback.                                                                                                                               |
| CanIncludeMainPicture           | Provide an option to set an image size and get the URL of the main image.                                                                                                 |
| UsesSkuAsMpnFallback            | Use SKU as manufacturer part number if MPN is blank.                                                                                                                      |
| OffersShippingTimeFallback      | Provide an option to enter a shipping time fallback.                                                                                                                      |
| OffersShippingCostsFallback     | Offer the option to enter a shipping cost fallback and a free shipping threshold.                                                                                         |
| CanOmitCompletionMail           | Automatically send a completion email.                                                                                                                                    |
| UsesAttributeCombination        | Provide additional data for attribute combinations.                                                                                                                       |
| UsesAttributeCombinationParent  | Export attribute combinations as products, including the parent product. This is only effective in conjunction with the _CanProjectAttributeCombinations_ export feature. |
| UsesRelatedDataUnits            | Provide additional data units for related data.                                                                                                                           |

To tell the provider that you want to export a CSV file, override the `FileExtension` property. The `Localizer` is used to localize messages. `CsvConfiguration` specifies CSV format details.

```csharp
public override string FileExtension => "CSV";

public Localizer T { get; set; } = NullLocalizer.Instance;

private CsvConfiguration _csvConfiguration;

private CsvConfiguration CsvConfiguration
{
    get
    {
        _csvConfiguration ??= new CsvConfiguration
        {
            Delimiter = ';',
            SupportsMultiline = false
        };

        return _csvConfiguration;
    }
}
```

#### Add configuration

Next, you need to tell the provider how it should be configured (_ViewComponent_ and _Model_). This is done with the `ConfigurationInfo` method.

```csharp
public override async ExportConfigurationInfo ConfigurationInfo => new()
{
    ConfigurationWidget = new ComponentWidget<HelloWorldConfigurationViewComponent>(),
    ModelType = typeof(ProfileConfigurationModel)
};
```

#### Export data

You are now ready to begin exporting your data. Going back to the `ExportAsync` method, start by retrieving the profile configuration data.

```csharp
var config = (context.ConfigurationData as ProfileConfigurationModel) ?? new ProfileConfigurationModel();
```

Get a `CsvWriter` and write the first row of column names.

```csharp
using var writer = new CsvWriter(new StreamWriter(context.DataStream, Encoding.UTF8, 1024, true));

writer.WriteFields(new string[]
{
    "ProductName",
    "SKU",
    "Price",
    "Savings",
    "Description"
});
writer.NextRow();
```

Now iterate over the data segments.

```csharp
while (context.Abort == DataExchangeAbortion.None && await context.DataSegmenter.ReadNextSegmentAsync())
{
    var segment = await context.DataSegmenter.GetCurrentSegmentAsync();
}
```

Inside the while loop, we get the product and it's entity.

```csharp
foreach (dynamic product in segment)
{
    if (context.Abort != DataExchangeAbortion.None)
    {
        break;
    }

    Product entity = product.Entity;
}
```

{% hint style="info" %}
The difference between Entity and Product is as follows:

* Entity: Represents the original entity read from the database.
* Product: A dynamic object that encapsulates the entity and enriches it with computed data.
{% endhint %}

Add a try-catch block for error handling.

```csharp
try
{
    // Export Product data
}
catch (OutOfMemoryException ex)
{
    context.RecordOutOfMemoryException(ex, entity.Id, T);
    context.Abort = DataExchangeAbortion.Hard;
    throw;
}
catch (Exception ex)
{
    context.RecordException(ex, entity.Id);
}
```

Now calculate the savings within the try block.

```csharp
var calculatedPrice = (CalculatedPrice)product._Price;
var saving = calculatedPrice.Saving;
```

Next, we write the fields in the order of our columns. Then we increment the number of rows.

```csharp
writer.WriteFields(new string[]
{
    product.Name,
    product.Sku,
    ((decimal)product.Price).FormatInvariant(),
    saving.HasSaving ? saving.SavingPrice.Amount.FormatInvariant() : string.Empty,
    ((string)product.FullDescription).Truncate(5000)
});

writer.NextRow();
context.RecordsSucceeded++;
```

Finally, you want to limit your row exports to `NumberOfExportedRows` from the profile configuration data.

```csharp
if (context.RecordsSucceeded >= config.NumberOfExportedRows)
{
    context.Abort = DataExchangeAbortion.Soft;
}
```

Your code could look like this:

{% code title="HelloWorldCsvExportProvider.cs" %}
```csharp
namespace MyOrg.HelloWorld.Providers
{
    [SystemName("MyOrg.HelloWorld.ProductCsv")]
    [FriendlyName("Hello world CSV product feed")]
    [ExportFeatures(Features =
        ExportFeatures.CreatesInitialPublicDeployment |
        ExportFeatures.OffersBrandFallback)]
    public class HelloWorldCsvExportProvider : ExportProviderBase
    {
        public override string FileExtension => "CSV";
        
        public Localizer T { get; set; } = NullLocalizer.Instance;

        private CsvConfiguration _csvConfiguration;

        private CsvConfiguration CsvConfiguration
        {
            get
            {
                _csvConfiguration ??= new CsvConfiguration
                {
                    Delimiter = ';',
                    SupportsMultiline = false
                };

                return _csvConfiguration;
            }
        }

        public override ExportConfigurationInfo ConfigurationInfo => new()
        {
            ConfigurationWidget = new ComponentWidget<HelloWorldConfigurationViewComponent>(),
            ModelType = typeof(ProfileConfigurationModel)
        };

        protected override async Task ExportAsync(ExportExecuteContext context, CancellationToken cancelToken)
        {
            var config = (context.ConfigurationData as ProfileConfigurationModel) ?? new ProfileConfigurationModel();

            using var writer = new CsvWriter(new StreamWriter(context.DataStream, Encoding.UTF8, 1024, true));

            writer.WriteFields(new string[]
            {
                "ProductName",
                "SKU",
                "Price",
                "Savings",
                "Description"
            });
            writer.NextRow();
            
            while (context.Abort == DataExchangeAbortion.None && await context.DataSegmenter.ReadNextSegmentAsync())
            {
                var segment = await context.DataSegmenter.GetCurrentSegmentAsync();

                foreach (dynamic product in segment)
                {
                    if (context.Abort != DataExchangeAbortion.None)
                    {
                        break;
                    }

                    Product entity = product.Entity;

                    try
                    {
                        var calculatedPrice = (CalculatedPrice)product._Price;
                        var saving = calculatedPrice.Saving;

                        writer.WriteFields(new string[]
                        {
                            product.Name,
                            product.Sku,
                            ((decimal)product.Price).FormatInvariant(),
                            saving.HasSaving ? saving.SavingPrice.Amount.FormatInvariant() : string.Empty,
                            ((string)product.FullDescription).Truncate(5000)
                        });
                        writer.NextRow();
                        ++context.RecordsSucceeded;

                        if (context.RecordsSucceeded >= config.NumberOfExportedRows)
                        {
                            context.Abort = DataExchangeAbortion.Soft;
                        }
                    }
                    catch (OutOfMemoryException ex)
                    {
                        context.RecordOutOfMemoryException(ex, entity.Id, T);
                        context.Abort = DataExchangeAbortion.Hard;
                        throw;
                    }
                    catch (Exception ex)
                    {
                        context.RecordException(ex, entity.Id);
                    }
                }
            }
        }
    }
}
```
{% endcode %}

### The XML export provider

XML export is very similar to CSV export. First, you need to change the file extension to `XML`.

```csharp
public override string FileExtension => "XML";
```

The `Localizer` and `ConfigurationInfo` stay the same. Now you just need to change `ExportAsync`. For XML, you get the writer from an `ExportXmlHelper`.

```csharp
using var helper = new ExportXmlHelper(context.DataStream);
var writer = helper.Writer;
```

Next, you start the document and write the grouping tag.

```csharp
writer.WriteStartDocument();
writer.WriteStartElement("products");
```

As with the CSV provider, you fetch the next data segment, iterating through the products.

```csharp
while (context.Abort == DataExchangeAbortion.None && await context.DataSegmenter.ReadNextSegmentAsync())
{
    var segment = await context.DataSegmenter.GetCurrentSegmentAsync();

    foreach (dynamic product in segment)
    {
        if (context.Abort != DataExchangeAbortion.None)
        {
            break;
        }

        Product entity = product.Entity;
    }
}
```

Write a new Product XML node with the values you want to export.

```csharp
writer.WriteStartElement("product");

try
{
    var calculatedPrice = (CalculatedPrice)product._Price;
    var saving = calculatedPrice.Saving;

    writer.WriteElementString("product-name", (string)product.Name);
    writer.WriteElementString("sku", (string)product.Sku);
    writer.WriteElementString("price", ((decimal)product.Price).FormatInvariant());

    if (saving.HasSaving)
    {
        writer.WriteElementString("savings", saving.SavingPrice.Amount.FormatInvariant());
    }

    writer.WriteCData("desc", ((string)product.FullDescription).Truncate(5000));

    context.RecordsSucceeded++;
    
    // Row limitation and catch block left out for brevity
}

writer.WriteEndElement(); // product
```

And when the while loop is complete, the grouping and the document must be closed.

```csharp
writer.WriteEndElement(); // products
writer.WriteEndDocument();
```

The source code can be found in `HelloWorldXmlExportProvider.cs`.

## Delete export profiles

Finally, you just need to clean up any existing profiles on `UnistallAsync` in `Module.cs`. Pass `SmartDbContext` and `IExportProfileService` to the module class constructor.

```csharp
private readonly SmartDbContext _db;
private readonly IExportProfileService _exportProfileService;

public Module(SmartDbContext db, IExportProfileService exportProfileService)
{
    _db = db;
    _exportProfileService = exportProfileService;
}
```

Then add the following lines to the top of your `UninstallAsync` method

```csharp
// Read the export profile entities associated with your export provider
var profiles = await _db.ExportProfiles
    .Include(x => x.Deployments)
    .Include(x => x.Task)
    .Where(x => x.ProviderSystemName == "MyOrg.HelloWorld.ProductCsv" || x.ProviderSystemName == "MyOrg.HelloWorld.ProductXml")
    .ToListAsync();

// Now delete the entities and any related file
await profiles.EachAsync(x => _exportProfileService.DeleteExportProfileAsync(x, true));
```

## Conclusion

In this tutorial, you created an export provider. You have created a configuration profile and a CSV export provider.

{% hint style="info" %}
The code for [this tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.ExportTutorial) can be found in the examples repository.
{% endhint %}
