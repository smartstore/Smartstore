# ✔️ Creating a Widget provider

{% hint style="info" %}
For a more in-depth view on widgets, zones and invokers, please refer to [widgets.md](../../../framework/content/widgets.md "mention").
{% endhint %}

There are many ways to display or inject content in Smartstore. One of the methods is to use a Widget. Following the tutorial [adding-tabs.md](adding-tabs.md "mention") tutorial, you'll be adding a widget to your project.

## Implementing the IActivatableWidget

Using the module from the [adding-tabs.md](adding-tabs.md "mention") tutorial, we'll start with the `Module.cs` file. You'll need to add the interface `IActivatableWidget` to the implementation.

```csharp
public class Module : ModuleBase, IConfigurable, IActivatableWidget
```

This will force you to implement the following two Methods:

```csharp
public Widget GetDisplayWidget(string widgetZone, object model, int storeId)

public string[] GetWidgetZones()
```

`GetWidgetZones` is a string array containing every widget zone we want to access.

```csharp
public string[] GetWidgetZones()
{
    return new string[] { "target_widget_zone_name" };
}
```

In this tutorial we'll be using the `productdetails_pictures_top` widget zone. It is placed above the product picture when using the Frontend.

{% hint style="info" %}
More examples of widget zone names can be found under [List of all core widget zone names](../../../framework/content/widgets.md#list-of-all-core-widget-zone-names).
{% endhint %}

{% hint style="info" %}
If you want to see the widget zones in your store, you can use the _DevTools_ plugin.

1. Install the _Smartstore Developer Tools_ plugin.
2. Click on **Configure**.
3. Activate the option to **Display Widget Zones**.
{% endhint %}

A simple implementation for `GetDisplayWidget` would be

```csharp
public Widget GetDisplayWidget(string widgetZone, object model, int storeId)
    => new ComponentWidget(typeof(HelloWorldViewComponent), new {widgetZone, model, storeId});
```

which creates a `ComponentWidget` for all widget zones we specified in `GetWidgetZones`. Your code should look something like this:

{% code title="Module.cs" %}
```csharp
public class Module : ModuleBase, IConfigurable, IActivatableWidget
{
    // ...

    public Widget GetDisplayWidget(string widgetZone, object model, int storeId)
        => new ComponentWidget(typeof(HelloWorldViewComponent), new {widgetZone, model, storeId});

    public string[] GetWidgetZones()
        => new string[] { "productdetails_pictures_top" };

    // ...
}
```
{% endcode %}

## Create the ViewComponentModel

For the widget to access `MyTabValue` from the [previous tutorial](adding-tabs.md), you'll need to create a new model.

1. Right click on the _Models_ folder in the Solution Explorer.
2. Place a new class called `ViewComponentModel.cs` in this folder.

Then add the following lines:

<pre class="language-csharp" data-title="ViewComponentModel.cs"><code class="lang-csharp"><strong>using Smartstore.Web.Modelling;
</strong>
namespace MyOrg.HelloWorld.Models
{
    public class ViewComponentModel : ModelBase
    {
        public string MyTabValue { get; set; }
    }
}
</code></pre>

## Adding the ViewComponent

1. Right click on the project in the Solution Explorer.
2. Add a new folder. According to our guidelines we call it _Components_.
3. Place a new class called _HelloWorldViewComponent.cs_ in this folder.

This class implements `SmartViewComponent`.

```csharp
public class HelloWorldViewComponent : SmartViewComponent
```

{% hint style="info" %}
By implementing `SmartViewComponent` we have access to a Logger, Localization and other common utilities and services.
{% endhint %}

To use the database context, add it to the constructor using dependency injection.

```csharp
private readonly SmartDbContext _db;

public HelloWorldViewComponent(SmartDbContext db)
{
    _db = db;
}
```

Next you'll add the `InvokeAsync` method. It is called each time the widget zones specified in `GetWidgetZones` is about to get rendered. The model is passed from `GetDisplayWidget`.

<pre class="language-csharp"><code class="lang-csharp"><strong>public async Task&#x3C;IViewComponentResult> InvokeAsync(string widgetZone, object model)
</strong></code></pre>

In case you're handling multiple widget zones and need to differentiate between them, you might add an if- or a switch-block for `widgetZone`. If you just want to make sure that your widget isn't displaying anything, when it is not supposed to, add the following lines:

```csharp
if (widgetZone != "productdetails_pictures_top")
{
    return Empty();
}
```

After checking whether the correct model is being used, you fetch the product from the database and get the specified `MyTabValue`.

```csharp
if (model.GetType() != typeof(ProductDetailsModel))
{
    return Empty();
}

var productModel = (ProductDetailsModel)model;
var product = await _db.Products.FindByIdAsync(productModel.Id);
var attributeValue = product.GenericAttributes.Get<string>("HelloWorldMyTabValue");
```

And finally you create the `ViewComponentModel` and return the View.

```csharp
var viewComponentModel = new ViewComponentModel
{
    MyTabValue = attributeValue
};

return View(viewComponentModel);
```

For you to use `ProductDetailsModel`, add these lines to you project file:

```xml
<ItemGroup>
    <ProjectReference Include="..\..\Smartstore.Web\Smartstore.Web.csproj">
        <Private>False</Private>
        <CopyLocal>False</CopyLocal>
        <CopyLocalSatelliteAssemblies>False</CopyLocalSatelliteAssemblies>
    </ProjectReference>
</ItemGroup>
```

{% hint style="info" %}
To get to the project file, simply click on your project in the Solution Explorer.
{% endhint %}

The final code looks like this:

{% code title="HelloWorldViewComponents.cs" %}
```csharp
public class HelloWorldViewComponent : SmartViewComponent
{
    private readonly SmartDbContext _db;

    public HelloWorldViewComponent(SmartDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object model)
    {
        if (widgetZone != "productdetails_pictures_top")
        {
            return Empty();
        }

        if (model.GetType() != typeof(ProductDetailsModel))
        {
            return Empty();
        }

        var productModel = (ProductDetailsModel)model;
        var product = await _db.Products.FindByIdAsync(productModel.Id);
        var attributeValue = product.GenericAttributes.Get<string>("HelloWorldMyTabValue");

        var viewComponentModel = new ViewComponentModel
        {
            MyTabValue = attributeValue
        };

        return View(viewComponentModel);
    }
}
```
{% endcode %}

## Adding the cacheable routes

You must add this code to your module if your ViewComponent should be cacheable. For more information see [output cache](../../../framework/platform/output-cache.md).

1. Right click on the project in the Solution Explorer.
2. Place a new class called _CacheableRoutes.cs_ in this folder.

Then add these lines:

{% code title="CacheableRoutes.cs" %}
```csharp
using Smartstore.Core.OutputCache;

namespace MyOrg.HelloWorld
{
    internal sealed class CacheableRoutes : ICacheableRouteProvider
    {
        public int Order => 0;

        public IEnumerable<string> GetCacheableRoutes()
        {
            return new string[]
            {
                "vc:MyOrg.HelloWorld/HelloWorld"
            };
        }
    }
}
```
{% endcode %}

## Adding the View

1. Right click on the _Views_ folder of your module in the Solution Explorer.
2. Add a new folder. According to the guidelines we call it _Shared/Components/HelloWorld_.
3. Place a new _Razor View_ called `Default.cshtml` in this folder.

Add the following lines for a simple output:

{% code title="Default.cshtml" %}
```cshtml
@model ViewComponentModel

@{
    Layout = "";
}

<span>Widget content: @Model.MyTabValue</span>
```
{% endcode %}

## Conclusion

Now you should be able to define a property in your product catalog and see it displayed above the product picture.

{% hint style="warning" %}
Don't forget to activate your widget!

1. Go to the Smartstore admin settings
2. Navigate to CMS / Widgets
3. You should see your widget listed. Press **Activate**.
{% endhint %}

In this tutorial you built a widget using the `IActivatableWidget` interface and specified your widget zones. You created a `ViewComponent` and bound it to different widget zones. Hopefully this will get you started with widgets and enable you to build more complex modules.

{% hint style="info" %}
The code for [this tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.WidgetTutorial) can be found in the examples repository.
{% endhint %}
