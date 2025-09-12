# 🐥 Creating a Block

A `Block` is used to quickly create content within the Page Builder. Smartstore has already implemented several blocks such as HTML output, images, iframes, product lists and many more. The possibilities for new blocks are endless.

{% hint style="info" %}
If you want to dive deeper into the topic, see [Blocks](../../../framework/content/page-builder-and-blocks.md).
{% endhint %}

{% hint style="info" %}
You can find well commented source code of a sample block at

_Smartstore.DevTools/Blocks/SampleBlock.cs_
{% endhint %}

## Write a simple Block

First you create a directory called _Blocks_ and add a new file called `HelloWorldBlock.cs`.

### Implement IBlock

Then you create a class that implements the `IBlock` interface.

```csharp
namespace MyOrg.HelloWorld.Blocks
{
    public class HelloWorldBlock : IBlock
    {
    }
}
```

This is where you add block-settings and values you may need for model preparation. Treat it like a model.

```csharp
[LocalizedDisplay("Plugins.MyOrg.HelloWorld.Name")]
public string Name { get; set; }
```

If you don't want a property to be stored in the database, use `IgnoreDataMember`.

```csharp
[IgnoreDataMember]
public Object NotForTheDatabase { get; set; }
```

Use `BindNever` to manually bind the model.

```csharp
[IgnoreDataMember, BindNever]
public MediaFileInfo mediaFile { get; set; }
```

### Add a BlockHandler

Next, add the new `HelloWorldBlockHandler` class to your file and inherit from the `BlockHandlerBase<T>` base class.

```csharp
public class HelloWorldBlockHandler : BlockHandlerBase<HelloWorldBlock>
{
    // Doing nothing means default behavior.
}
```

Place the following attribute on top of your `BlockHandler` definition.

```csharp
[Block("helloworld", FriendlyName = "Hello World", Icon = "fa fa-eye")]
```

The input for this attribute represents the metadata of your block. Three things are accomplished here:

* `helloworld`: Defines the [location of the views](creating-a-block.md#add-some-views) for your `Block`. A lowercase value is recommended by our guidelines.
* `Hello World`: Specifies the label text of your block as it will appear in Page Builder.
* `fa fa-eye`: Specifies the Font Awesome icon that appears next to the label in the Page Builder.

You don't need to add any code to your `BlockHandler`. It will have the default behavior you want at this point. See [Advanced topics](creating-a-block.md#advanced-topics) for other uses.

### Add a validator

To ensure that all user input is correct, add a validator to your file that inherits from the `AbstractValidator<T>` base class.

```csharp
public partial class HelloWorldBlockValidator : AbstractValidator<HelloWorldBlock>
{
    public HelloWorldBlockValidator()
    {
    }
}
```

Here you can define your rules using `FluentValidation`. Add the following to ensure that `Name` is never empty.

```csharp
RuleFor(x => x.Name).NotEmpty();
```

If you have more properties you want to check, simply add more rules.

```csharp
RuleFor(x => x.Name).NotEmpty();
RuleFor(x => x.ASmallValue).GreaterThan(0);
RuleFor(x => x.AShortText).MaximumLength(255);
```

Your final code should look something like this:

{% code title="HelloWorldBlock.cs" %}
```csharp
using FluentValidation;
using Smartstore.Core.Content.Blocks;
using Smartstore.Web.Modelling;

namespace MyOrg.HelloWorld.Blocks
{
    [Block("helloworld", Icon = "fa fa-eye", FriendlyName = "Hello World")]
    public class HelloWorldBlockHandler : BlockHandlerBase<HelloWorldBlock>
    {
        // Doing nothing means default behavior.
    }
    
    public class HelloWorldBlock : IBlock
    {
        [LocalizedDisplay("Plugins.MyOrg.HelloWorld.Name")]
        public string Name { get; set; }
    }
    
    public partial class HelloWorldBlockValidator : AbstractValidator<HelloWorldBlock>
    {
        public HelloWorldBlockValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
```
{% endcode %}

### Add Views

Create the _BlockTemplates/helloworld/_ directory in the _Views/Shared/_ directory of your module.

{% hint style="warning" %}
The folder name _helloworld_ corresponds to the attribute you set in your [BlockHandler](creating-a-block.md#add-a-blockhandler)!
{% endhint %}

Create the two files _Edit.cshtml_ and _Public.cshtml_ for simple output.

{% hint style="info" %}
Add the `@using MyOrg.HelloWorld.Blocks` reference to \_ViewImports. This way you can access the `HelloWorldBlock` model in all your views.
{% endhint %}

#### Edit.cshtml

This is your configuration view. It appears when you are configuring the block. It works the same way as the configuration views of modules.

```cshtml
@model HelloWorldBlock

<div class="adminContent">
    <div class="adminRow">
        <div class="adminTitle">
            <smart-label asp-for="Name" />
        </div>
        <div class="adminData">
            <input asp-for="Name" />
            <span asp-validation-for="Name"></span>
        </div>
    </div>
</div>
```

#### Public.cshtml

The HTML structure defined in this view is rendered publicly. You can also see this by clicking on **Preview** in the Page Builder.

```cshtml
@model HelloWorldBlock

<div>My block name: @Model.Name</div>
```

{% hint style="info" %}
Create a _Preview.cshtml_ file to define an alternate display for the Grid Edit view. This may be necessary if the content contains external resources. For example, we've used it in our _IFrame_ block to prevent the external resource from loading when arranging blocks on the grid.
{% endhint %}

Now you can use your `Block` in the Page Builder.

## Advanced topics

Taking a closer look at the `BlockHandler`, there is some more functionality you can add to your module.

### Handling StoryViewMode

`StoryViewMode` allows you to distinguish between the four different view modes of a `Block`:

* `Edit`: Appears when editing `Block` properties.
* `GridEdit`: Shown when arranging blocks on the grid.
* `Preview`: Shown when you preview your story.
* `Public`: Shown on the front end when your story is published.

Create a new block-setting `MyLocalVar` in `HelloWorldBlock`.

```csharp
public string MyLocalVar { get; set; } = "Initialised in Block";
```

Add the `Load` function to the `HelloWorldBlockHandler`.

```csharp
protected override HelloWorldBlock Load(
    IBlockEntity entity, 
    StoryViewMode viewMode)
```

To access the model of the current block, use the following line.

```csharp
var block = base.Load(entity, viewMode);
```

Now you can use `viewMode` to get the current view mode for your block.

```csharp
if (viewMode == StoryViewMode.Edit)
{
    // This is called only in Edit mode.
    block.MyLocalVar += " - Running in Edit-Mode";
}
else if (viewMode == StoryViewMode.Preview)
{
    // This is called only in Preview-Mode
    block.MyLocalVar += " - Running in Preview-Mode";
}
else if (viewMode == StoryViewMode.GridEdit)
{
    // This is called only in Grid-Edit-Mode
    block.MyLocalVar += " - Running in Grid-Edit-Mode";
}
else if (viewMode == StoryViewMode.Public)
{
    // This is called only in Public-Mode
    block.MyLocalVar += " - Running in Public-Mode";
}

return block;
```

Add the following to _Edit.cshtml_, _Preview.cshtml_ and _Public.cshtml_.

```cshtml
<div>My local variable: @Model.MyLocalVar</div>
```

{% hint style="info" %}
Use _Preview.cshtml_ to access the grid edit mode.
{% endhint %}

Run your code and see how `MyLocalVar` responds to each view mode.

### Using a widget as a view

If you want to display a widget instead of a view, you can use the following methods.

```csharp
protected override Task RenderCoreAsync(
    IBlockContainer element, 
    IEnumerable<string> templates, 
    IHtmlHelper htmlHelper, 
    TextWriter textWriter)
```

`RenderCoreAsync` gives you the ability to change the rendering behavior. This means that you can display the HelloWorld widget instead of using your views.

Add these lines to the `RenderCoreAsync` function in the `HelloWorldBlockHandler`.

```csharp
if (templates.First() == "Edit")
{
    return base.RenderCoreAsync(element, templates, htmlHelper, textWriter);
}
else
{
    return RenderByWidgetAsync(element, templates, htmlHelper, textWriter);
}
```

This will allow the `GetWidget` method to be called so that you can render your widget.

```csharp
protected override Widget GetWidget(
    IBlockContainer element, 
    IHtmlHelper htmlHelper, 
    string template)
```

`GetWidget` works much like `GetDisplayWidget` in _Module.cs_. To call it, add these lines to the `GetWidget` method in `HelloWorldBlockHandler`:

```csharp
return new ComponentWidget(typeof(HelloWorldViewComponent), new
{
    widgetZone = "productdetails_pictures_top",
    model = new ProductDetailsModel { Id = 1 }
});
```

This will display your widget, rendering the content for the product with the Id `1`.

{% hint style="info" %}
If you want to pass data from the block to your widget, use the following code in the `GetWidget` method to get the block model.

`var block = (HelloWorldBlock)element.Block;`
{% endhint %}

### Final code

Your complete code should look something like this:

{% code title="HelloWorldBlock.cs" %}
```csharp
namespace MyOrg.HelloWorld.Blocks
{
    [Block("helloworld", Icon = "fa fa-eye", FriendlyName = "Hello World")]
    public class HelloWorldBlockHandler : BlockHandlerBase<HelloWorldBlock>
    {
        protected override HelloWorldBlock Load(
            IBlockEntity entity,
            StoryViewMode viewMode)
        {
            var block = base.Load(entity, viewMode);

            if (viewMode == StoryViewMode.Edit)
            {
                // This is called only in Edit mode.
                block.MyLocalVar += " - Running in Edit-Mode";
            }
            else if (viewMode == StoryViewMode.Preview)
            {
                // This is called only in Preview-Mode
                block.MyLocalVar += " - Running in Preview-Mode";
            }
            else if (viewMode == StoryViewMode.GridEdit)
            {
                // This is called only in Grid-Edit-Mode
                block.MyLocalVar += " - Running in Grid-Edit-Mode";
            }
            else if (viewMode == StoryViewMode.Public)
            {
                // This is called only in Public-Mode
                block.MyLocalVar += " - Running in Public-Mode";
            }

            return block;
        }
        
        protected override Task RenderCoreAsync(
            IBlockContainer element, 
            IEnumerable<string> templates, 
            IHtmlHelper htmlHelper, 
            TextWriter textWriter)
        {
            if (templates.First() == "Edit")
            {
                return base.RenderCoreAsync(element, templates, htmlHelper, textWriter);
            }
            else
            {
                return RenderByWidgetAsync(element, templates, htmlHelper, textWriter);
            }
        }

        protected override Widget GetWidget(
            IBlockContainer element, 
            IHtmlHelper htmlHelper, 
            string template)
        {
            var block = (HelloWorldBlock)element.Block;
            
            return new ComponentWidget(typeof(HelloWorldViewComponent), new
            {
                widgetZone = "productdetails_pictures_top",
                model = new ProductDetailsModel { Id = 1 }
            });
        }
    }
    
    public class HelloWorldBlock : IBlock
    {
        [LocalizedDisplay("Plugins.MyOrg.HelloWorld.Name")]
        public string Name { get; set; }
        
        public string MyLocalVar { get; set; } = "Initialised in Block";
    }
    
    public partial class HelloWorldBlockValidator : AbstractValidator<HelloWorldBlock>
    {
        public HelloWorldBlockValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
```
{% endcode %}

## Conclusion

In this tutorial, you created your own `Block`. You learned how to access the different view modes and how to use a widget as output for a block.

{% hint style="info" %}
The code for [the simple tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.BlockTutorial) and [the advanced tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.BlockTutorialAdvanced) can be found in the examples repository.
{% endhint %}
