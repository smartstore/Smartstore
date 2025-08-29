---
description: Inject content into zones
---

# ✔️ Widgets

## Overview

Widgets are pieces/snippets of HTML content that can be injected into the [widget zones](widgets.md#zones) of a page. The ability to inject external content into existing pages is essential for modular applications like Smartstore. Common scenarios include:

* Place further JavaScript code into the HEAD section of your page.
* Extend the navigation bar by adding menu items.
* Implement a custom sidebar.
* Add more content to:
  * data grids or pages
  * product listings and details
  * the shopping cart

Smartstore has a widget abstraction called [Widget](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Widgets/Widget.cs). This is because ASP.NET Core _view components_ and _partial views_ are technically two different things, though they behave very similarly to widgets in Smartstore. In order to use them in Smartstore, they need to be unified so that widgets can be fed from different content sources.

* `HtmlWidget`: Renders any `IHTMLContent` instance
* `ComponentWidget`: Invokes and renders an ASP.NET Core view component
* `PartialViewWidget`: Invokes and renders an ASP.NET Core partial view

{% hint style="info" %}
By deriving from `Widget` and overriding the `InvokeAsync` method, you can implement a custom widget class. You can just return your content directly if the output is simple enough. Otherwise, implement the rendering portion as an [`IWidgetInvoker<TWidget>`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Widgets/Services/IWidgetInvoker.cs). Then the `Widget.InvokeAsync` method should resolve and call the invoker.
{% endhint %}

In addition, the [widget](widgets.md#widget-tag-helper) Tag Helper allows you to compose HTML content in any view template and inject it into any zone, much like the _section_ directive in ASP.NET.

## Zones

Zones allow you to define places in any view file where widgets should be able to inject custom markup. They are similar to ASP.NET _Sections_, but much more powerful.

There are hundreds of zones in the Smartstore view templates. Here are some of the areas that these zones fall into:

* Homepage header / footer
* Navigation bar
* Category details
* Product details
* Account menu
* Search
* Reviews
* Checkout

{% hint style="info" %}
See below for a [complete list](widgets.md#list-of-all-core-widget-zone-names) of widget zone names.
{% endhint %}

You can define your own zones in any Razor view by using the [zone Tag Helper](../../compose/theming/tag-helpers.md#zone-tag-helper).

```cshtml
<zone name="wishlist_items_top" />
```

You can specify default content for zones and whether to replace it when a widget injects content.

```cshtml
@* 
    The "replace-content" attribute specifies whether
    the default content should be removed if at least one
    widget is rendered in the zone:
        true: yes, remove default content.
        false: no, keep content and place widget before or after 
            content, according to "WidgetInvoker.Prepend" option.
*@
<zone name="wishlist_items_top" replace-content="true">
    <div>Lorem</div>
    <div>Ipsum</div>
</zone>
```

There are other HTML tags that can function as zones: `div`, `span`, `p`, `section`, `aside`, `header`, `footer`.

```cshtml
@* 
    The "remove-if-empty" attribute specifies whether
    to remove the tag when it has no content. Default: false.
*@
<div name="wishlist_items_top" remove-if-empty="true"></div>
```

Sometimes you may need to check whether a zone has content and suppress it if it has none, _before_ declaring the `zone` tag. There are two ways of doing this depending on what you need. You can wrap the `zone` tag in a conditional block and suppress the wrapper HTML output.

```aspnet
@if (await Display.ZoneHasContentAsync("wishlist_items_top")) 
{
    <div class="some-wrapper">
        <zone name="wishlist_items_top" />
    </div>
}
```

Or you can suppress surrounding content by using the `sm-suppress-if-empty-zone` Tag Helper. This will pre-render a given zone and suppress the output of the parent tag if the zone content is empty or consists only of whitespace.

```aspnet
<div sm-suppress-if-empty-zone="wishlist_items_top" class="some-wrapper">
    <div class="inner-wrapper m-4">
        <zone name="wishlist_items_top" />
    </div>
</div>
```

## Widget Tag Helper

The `widget` Tag Helper allows you to compose HTML content in any view template and to inject it into any zone. It behaves much like the _section_ directive in ASP.NET.

```cshtml
@*
    target-zone: Required,
    order: Sort order within target zone. Optional, 
    prepend: Whether to insert BEFORE existing zone content (instead AFTER). Optional,
    key: When set, ensures uniqueness within a particular zone. Optional.
*@
<widget target-zone="wishlist_items_top" order="10" prepend="false" key="MyWidgetInstanceKey">
    @await Component.InvokeAsync("MyComponent1")
    <div>Lorem ipsum</div>
    @await Component.InvokeAsync("MyComponent2")
</widget>
```

These HTML tags can also act as widgets: `div`, `span`, `section`, `form`, `script`, `style`, `link`, `meta`, `ul`, `ol`, `svg`, `img`, `a`. The widget attribute names must be prefixed with **sm-**, for example `sm-target-zone` and `sm-key`.

The difference between a _widgetized_ HTML tag and a `widget` is, that it is moved completely to its designated zone, whereas `widget` only moves the child content, removing the `widget` root tag from the output.

```cshtml
@*
    Specifying "data-origin" attribute is good practice, because
    it gives you an idea about where a piece of code came from
    while inspecting the document markup in a browser. 
*@
<script sm-target-zone="scripts" data-origin="blog-list">
    $(function () {
        $(".blogposts").masonryGrid(".bloglist-item");
    });
</script>

<style type="text/css" sm-target-zone="stylesheets" >
    .some-selector { }
</style>

<script src="~/bundle/js/fileuploader.js" 
    sm-target-zone="scripts" 
    sm-key="fileuploader"></script>
```

## Widget class

The [widget](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Widgets/Widget.cs) class unifies _view components_, _partial views_ and the `IHtmlContent` interface, so that widgets can be instantiated using each. Before injecting or rendering a widget an instance needs to be created.

```csharp
// From view component: by type.
// -----------------------------
var widget = new ComponentWidget<WeatherViewComponent>() 
{ 
    // sort order within target zone. Optional.
    Order = 10,
    // Whether to insert BEFORE existing zone content (instead AFTER). Optional.  
    Prepend = false,
    // When set, ensures uniqueness within a particular zone. Optional. 
    Key = "MyWidgetInstanceKey" 
};

// From view component: by name + arguments passed.
// ------------------------------------------------
// The second parameter "My.Module" is the system name of the module
// where the view component is located. This must be specified, 
// otherwise component resolution by name will fail.
var widget = new ComponentWidget("Weather", "My.Module", new 
{
    // Pass arguments to the view component's "Invoke" method
    arg1 = "Hello",
    arg2 = "World"
});

// From partial view by name.
// --------------------------
// The second parameter "My.Module" is the system name of the module
// where the partial view is located. This must be specified, 
// otherwise view resolution will fail.
var widget = new PartialViewWidget("Weather", "My.Module");

// From any HTML content.
// ----------------------
var tag = new TagBuilder("div");
tag.InnerHtml.SetContent("Lorem ipsum");
var widget = new HtmlWidget(tag);
```

Using the [IWidgetProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Widgets/Services/IWidgetProvider.cs) Interface is the most common way to inject a widget into a zone. It’s a request scoped registrar for widget instances.

```csharp
internal class CookieConsentFilter : IResultFilter
{
    private readonly IWidgetProvider _widgetProvider;

    public CookieConsentFilter(IWidgetProvider widgetProvider)
    {
        _widgetProvider = widgetProvider;
    }
    
    public void OnResultExecuting(ResultExecutingContext context)
    {
        _widgetProvider.RegisterWidget(
            "end", // The zone name to render widget into
            new ComponentWidget("CookieManager", null));
    }
    
    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Too late for widgets here: page is rendered already.
    }
}
```

<table><thead><tr><th width="225">Method</th><th>Description</th></tr></thead><tbody><tr><td><code>RegisterWidget</code></td><td>Registers a custom widget for widget zones. The zones can also be passed as an array or a regular expression.</td></tr><tr><td><code>HasWidgets</code></td><td>Checks whether a zone contains at least one injected widget.</td></tr><tr><td><code>GetWidgets</code></td><td>Enumerates all injected widgets in a given zone.</td></tr></tbody></table>

## Static widgets (aka widget providers)

You can also handle widgets implementing a widget provider, although this is the legacy way of doing so. Implementing the [IActivatableWidget](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Widgets/IActivatableWidget.cs) interface turns an application feature provider into a widget (see [Modularity & Providers](../platform/modularity-and-providers.md) for more information about providers).

The provider class defines what to render (`GetDisplayWidget`), and where to render it (`GetWidgetZones`). Here's an excerpt from the [Google Analytics Module](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Modules/Smartstore.Google.Analytics/Module.cs) that injects script content into the head zone using those methods.

```csharp
internal class Module : ModuleBase, IConfigurable, IActivatableWidget, ICookiePublisher
{
    // Other code is omitted for brevity.

    public Widget GetDisplayWidget(string widgetZone, object model, int storeId)
        => new ComponentWidget<GoogleAnalyticsViewComponent>(model);

    public string[] GetWidgetZones()
        => new[] { "head" };

    // Other code is omitted for brevity.
}
```

{% hint style="info" %}
Static widgets need to be explicitly enabled by the user in the backend (**CMS / Widgets**), otherwise they will not be rendered. However, by decorating a non-widget provider with the [DependentWidgetsAttribute](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Widgets/DependentWidgetsAttribute.cs), you can specify widget providers, that should be automatically (de)activated when the provider is. This is useful in scenarios where separate widgets are responsible for displaying provider data.
{% endhint %}

## List of all core widget zone names

A list of common zone names are recorded in the file _App\_Data/widgetzones.json_.
