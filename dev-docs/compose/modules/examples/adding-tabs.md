# 🐥 Adding tabs

_Smartstore_ uses tabs in different places in the backend and frontend. The `TabTagHelper` is used for this exact purpose.

The following markup can be used to add tabs in a _Razor_ view.

```html
<tabstrip id="my-tab-config" sm-nav-style="Material" sm-nav-position="Top">
    <tab sm-title="Tab title 1" sm-selected="true">
        Tab content 1
    </tab>
    <tab sm-title="Tab title 2">
        Tab content 2
    </tab>
</tabstrip>
```

You can use this _Tag Helper_ in any view. To do this, you must make the tag helper known in the view. Do this by either adding the following line to the view or use the \_`ViewImports.cshtml` file in the root of the views folder.

```html
@addTagHelper Smartstore.Web.TagHelpers.Shared.*, Smartstore.Web.Common
```

## Adding a tab

If you as a developer are faced with the task to extend an existing entity e.g.: `Product`, `Category` or `Manufacturer`, you should not do this in the core code itself but attach it to the tab of a module. In the course of this tutorial, you will extend the _HelloWorld_ module, created in the [last tutorial](../tutorials/building-a-simple-hello-world-module.md), adding an `Events.cs` class to the root of the module.

When a tab strip is created, the Tag Helper publishes the `TabStripCreated` event. Its event message contains everything you need to add a custom tab to the tab strip. The code to add a custom tab in the product detail configuration in the admin area looks like this:

```csharp
using System.Threading.Tasks;
using Smartstore.Events;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering.Events;

namespace MyOrg.HelloWorld
{
    public class Events : IConsumer
    {
        public async Task HandleEventAsync(TabStripCreated eventMessage)
        {
            var tabStripName = eventMessage.TabStripName;

            if (tabStripName == "product-edit")
            {
                var entityId = ((TabbableModel)eventMessage.Model).Id;
                
                // Add a custom tab
                await eventMessage.TabFactory.AppendAsync(builder => builder
                    .Text("My Tab")
                    .Name("tab-MyTab")
                    .Icon("star", "bi")
                    .LinkHtmlAttributes(new { data_tab_name = "MyTab" })
                    .Action("AdminEditTab", "HelloWorldAdmin", new { entityId })
                    .Ajax());
            }
        }
    }
}
```

The event message stores the tab id in `TabStripName`. The tab of interest has the id `product-edit`. The event message contains the model of the containing view as well. This will give you access to the ID of the entity for which the detail view was requested.

Using the event message’s `TabFactory`, you can inject a new tab.

<table><thead><tr><th width="198">Method</th><th>Description</th></tr></thead><tbody><tr><td><strong>Text</strong></td><td>The text of the tab item that appears in the tab strip</td></tr><tr><td><strong>Name</strong></td><td>Unique name/id of tab item</td></tr><tr><td><strong>Icon</strong></td><td>The icon of the tab</td></tr><tr><td><strong>LinkHtmlAttributes</strong></td><td>HTML attributes to be added to the tab's link</td></tr><tr><td><strong>Action</strong></td><td>The MVC <code>action</code> that should be invoked to display the tab</td></tr><tr><td><strong>Ajax</strong></td><td>Specifies whether the tab content should be lazy loaded via Ajax when clicked</td></tr></tbody></table>

## Adding an action

Now that the tab has been added, you need to a corresponding action that returns a view. Open the admin controller and add the following action:

```csharp
public async Task<IActionResult> AdminEditTab(int entityId)
{
    var product = await _db.Products.FindByIdAsync(entityId, false);

    var model = new AdminEditTabModel
    {
        EntityId = entityId,
        MyTabValue = product.GenericAttributes.Get<string>("HelloWorldMyTabValue")
    };
    
    // Very important for proper model binding.
    ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[MyTab]";
    return View(model);
}
```

The value to populate the model is taken from the `GenericAttributes` property of the product. Since this has not yet been saved, it is empty at first, but more on that later.

{% hint style="info" %}
To learn more about generic attributes, please refer to [generic-attributes.md](../../../framework/advanced/generic-attributes.md "mention")
{% endhint %}

To get the instance of the product you just edited, use an instance of `SmartDbContext`. Make it known to the controller via _dependency injection_ by adding the following code at the very top:

```csharp
private readonly SmartDbContext _db;

public HelloWorldAdminController(SmartDbContext db)
{
    _db = db;
}
```

## Adding a view

Since the action we just added uses a model that has two simple properties and returns a view, we'll need to create the model and the view next. The `AdminEditTabModel.cs` class belongs in the _Models_ folder.

```csharp
using Smartstore.Web.Modelling;

namespace MyOrg.HelloWorld.Models
{
    // This attribute must be added for proper model binding.
    // We've implemented it for security reasons. 
    // Explaining this is beyond the scope of this tutorial.
    [CustomModelPart]
    public class AdminEditTabModel : ModelBase
    {
        public int EntityId { get; set; }

        [LocalizedDisplay("Plugins.MyOrg.HelloWorld.MyTabValue")]
        public string MyTabValue { get; set; }
    }
}

```

Please also place the localized values for this model in the [corresponding XML files](../tutorials/building-a-simple-hello-world-module.md#adding-localization). The view for the model belongs in the _HelloWorldAdmin_ view folder and contains the following code:

```html
@model AdminEditTabModel

@{
    Layout = "";
}

<!-- Very important for proper model binding. -->
<input type="hidden" name="CustomProperties[MyTab].__Type__" value="@Model.GetType().AssemblyQualifiedName" />
<input type="hidden" asp-for="EntityId" />

<div class="adminContent">
    <div class="adminRow">
        <div class="adminTitle">
            <smart-label asp-for="MyTabValue" />
        </div>
        <div class="adminData">
            <input asp-for="MyTabValue" />
            <span asp-validation-for="MyTabValue"></span>
        </div>
    </div>
</div>
```

When the project is compiled and the product configuration is opened in the admin area, the new tab is displayed.

## Using the ModelBoundEvent

Next, make sure that the entered value is also saved when the product is saved. To do this, listen to the `ModelBoundEvent` that is published whenever a form is posted and the _MVC model binder_ has bound the model.

The code to save the value of the tab belongs in the `Events.cs` class and looks like this:

```csharp
public async Task HandleEventAsync(ModelBoundEvent message, SmartDbContext db)
{
    if (!message.BoundModel.CustomProperties.ContainsKey("MyTab"))
        return;

    if (message.BoundModel.CustomProperties["MyTab"] is not AdminEditTabModel model)
        return;

    var product = await db.Products.FindByIdAsync(model.EntityId);
    product.GenericAttributes.Set("HelloWorldMyTabValue", model.MyTabValue);

    await db.SaveChangesAsync();
}
```

The value is stored in the product's `GenericAttributes`. A generic attribute is a separate entity that stores any simple value for each entity. For more complex data structures you should provide your own domain objects in your module.

{% hint style="info" %}
To learn more about events, please refer to [events.md](../../../framework/platform/events.md "mention")
{% endhint %}

## Conclusion

In this tutorial you added a tab to the product's edit tabstrip using the `TabFactory` and used the `ModelBoundEvent` to save a product specific value.

{% hint style="info" %}
The code for [this tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.TabsTutorial) can be found in the examples repository.
{% endhint %}
