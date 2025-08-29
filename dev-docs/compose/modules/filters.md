# 🐥 Filters

Smartstore modules are pure MVC projects. This means that normal action filters, provided by the ASP.NET Core framework, can be implemented. Implementing filters in modules is the best way to extend, intercept and modify existing functionality in Smartstore.

{% hint style="info" %}
For more information, see the [different filters that can be implemented with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-7.0).
{% endhint %}

## Basic example

Let’s say a module needs to render a link in the header navigation of the store. This link leads to a page provided by the module. To do this, create a filter class that renders the link in the `header_menu_special` widget zone.

{% hint style="info" %}
For more information on widgets and widget-zones, see [Widgets](../../framework/content/widgets.md).
{% endhint %}

The filter might look like this:

```csharp
public class MyFilter : IResultFilter
{
    private readonly IWidgetProvider _widgetProvider;   
    private readonly IUrlHelper _urlHelper;

    public MyFilter(IWidgetProvider widgetProvider, IUrlHelper urlHelper)
    {
        _widgetProvider = widgetProvider;
        _urlHelper = urlHelper;
    }

    public void OnResultExecuting(ResultExecutingContext filterContext)
    {
        // Should only run on a full view rendering result or HTML ContentResult.
	if (!filterContext.Result.IsHtmlViewResult())
	{
	    return;
	}

	// Menu item in global header
	var html = $"<a class='menubar-link' href='{_urlHelper.RouteUrl("MyRoute")}'>My Link</a>";
	_widgetProvider.RegisterHtml("header_menu_special", new HtmlString(html), 100);
    }

    public void OnResultExecuted(ResultExecutedContext filterContext)
    {
    }
}
```

### Register a filter in Startup

The \`StartUp' class of the module is used to register a filter.

```csharp
internal class Startup : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        // ...
        services.Configure<MvcOptions>(o =>
        {
	    o.Filters.Add<MyFilter>();
        });
    }
}
```

Now the filter will be applied to every `Action` in the entire project.

## Endpoint filter

The `Add` method assigns filters to all endpoints, enabling them to run on all controllers and actions, and adds the filters to the global list. Therefore, each filter must decide whether to run on a controller or an action. Since all filters are evaluated at runtime, performance worsens as more filters are added.

To avoid cluttering the global filter list and evaluating each filter at runtime, it is better to use endpoint filters. Assigned to controllers and actions at startup, these filters are then removed from the global filter list via [EndpointFilterModelConvention](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Web.Common/Filters/EndpointFilterModelConvention.cs). This improves performance and makes the code more manageable.

```csharp
// Add a filter that is assigned to the PublicController.
// Example: Add a widget to the frontend.
o.Filters.AddEndpointFilter<MyFrontendFilter, PublicController>();
```

Unlike the `Add` method, endpoint filters are not limited to the global scope but can simulate the same behavior:

```csharp
o.Filters.AddEndpointFilter<MyFilter, Controller>();
// This is equivalent to:
o.Filters.Add<MyFilter>();
```

Since every controller inherits from the `Controller` class, `MyFilter` will be assigned to each one upon startup.

### ForAction and ForController

The assignment can be specified further using the `ForAction` and `ForController` methods. For example, you can limit the filter to the `Confirm` action.

```csharp
o.Filters.AddEndpointFilter<MyFilter, Controller>().ForAction("Confirm");
```

This assigns the filter to the `Action` method instead of the controller, making it behave as if it were annotated with a filter attribute.

### Examples

There are multiple ways to assign filters to controllers and actions:

```csharp
// Add a filter that is assigned to the PublicController.
o.Filters.AddEndpointFilter<MyFrontendWidgetFilter, PublicController>();

// Add a filter to the customer and identity frontend controllers.
o.Filters.AddEndpointFilter<CustomerInfoFilter, PublicController>()
    .ForController("Customer")
    .ForController("Identity");

// Alternatively written as:
var _usedControllers = ["Customer", "Identity"];
o.Filters.AddEndpointFilter<CustomerInfoFilter, PublicController>()
    .ForController(x => _usedControllers.Contains(x.ControllerName));

// Add a customer profile frontend filter.
o.Filters.AddEndpointFilter<CustomerProfileFilter, PublicController>()
    .ForController("Identity")
    .ForAction("CustomerProfile");

// Alternatively written as:
o.Filters.AddEndpointFilter<CustomerProfileFilter, PublicController>()
    .ForAction("Identity.CustomerProfile");

// Add a filter that is assigned to the ProductDetails action for every product.
o.Filters.AddEndpointFilter<MyPaymentMethodDisplayFilter, ProductController>()
    .ForAction(x => x.ProductDetails(0, null));

// Add a filter that is assigned to specific frontend actions.
o.Filters.AddEndpointFilter<MyTrackingFilter, SearchController>()
    .ForAction(x => x.InstantSearch(null))
    .ForAction(x => x.Search(null));
o.Filters.AddEndpointFilter<MyTrackingFilter, CatalogController>()
    .ForAction(x => x.CompareProducts());

// Add a filter, validating the controller and action names yourself.
o.Filters.AddEndpointFilter<MyLactoseFilter, AdminController>()
    .ForAction(x =>
    {
        var controllerName = x.Controller.ControllerName;
        var actionName = x.ActionName;
        // This custom method returns a boolean.
        return IsThisMethodDairyFree($"{controllerName}.{actionName}");
    });
```

### Conditional filtering

One major advantage of the `AddEndpointFilter` method is that it supports conditional filtering. The `When` method allows you to assign filters to an endpoint. Whether the filters are executed depends on the condition, which is evaluated at runtime.

{% hint style="info" %}
Internally, the conditional filtering is performed by the `FilterProvider`. This approach replaces the obsolete `AddConditional` method, which performed the same function but kept the filter global.
{% endhint %}

#### Examples

```csharp
// Add a filter if it is a certain request.
o.Filters.AddEndpointFilter<MyRequestFilter, SmartController>()
    .When(context => IsMySpecificRequest(context.HttpContext.Request));

// Add a filter dependent on a setting.
o.Filters.AddEndpointFilter<MyBusyFilter, MyController>()
    .When(context => numberOfVisitors > _settings.MaxThreshold);

// Do not add a filter if an AJAX request is being made.
o.Filters.AddEndpointFilter<MyUserInteractionFilter, PublicController>()
    .WhenNonAjax();

// Do not add a filter if an AJAX GET request is being made.
o.Filters.AddEndpointFilter<MySensitiveUserInputFilter, MyController>()
    .ForAction("Edit")
    .WhenNonAjaxGet();
```

### So, which method should I use to register my filter? :person\_shrugging:

If possible, use the `AddEndpointFilter` method to register your filter. Only use the `Add` method if your filter **must** be global and very flexible.
