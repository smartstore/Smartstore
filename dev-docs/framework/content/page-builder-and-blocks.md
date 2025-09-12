# Page Builder & Blocks

## Overview
Smartstore's Page Builder lets editors compose "stories" by arranging content blocks on a responsive grid. Each story is stored in the database and rendered into the configured [widget zone](/framework/content/widgets#zones). Blocks encapsulate pieces of content such as HTML snippets, images or product listings. Blocks can be dragged, configured and reordered in the Page Builder UI.

## Block architecture
Every block model implements `IBlock` and carries a `[Block]` attribute that supplies metadata like a system name, label and FontAwesome icon. A matching `IBlockHandler` drives the block's lifecycle: creating instances, loading and saving the serialized model and rendering it. Most handlers inherit from `BlockHandlerBase<TBlock>` which offers hooks like `BeforeRender`, `AfterSaveAsync` and `CloneAsync` in addition to default load and save logic. Registered blocks expose their metadata through `IBlockMetadata` so the Page Builder can list them grouped by module.

## Templates
Blocks render through Razor views under `Views/Shared/BlockTemplates/<systemName>/`:

- `Edit.cshtml` – configuration form shown in the editor.
- `Preview.cshtml` – optional lightweight markup used in the editor preview.
- `Public.cshtml` – markup returned to the storefront.

Additional variants can be selected via the block's `Template` property. Handlers may alternatively output a widget by overriding `RenderCoreAsync`.

## Data binding
Blocks that implement `IBindableBlock` can bind to existing entities such as products or categories. Their handler must implement `IBindableBlockHandler` and provide a `TemplateMappingConfiguration` so the Page Builder knows how to map entity fields to template tokens. During rendering the binding source is applied automatically.

## Assets and localization
Annotate properties referencing media or other assets with `StoryAsset` so referenced files are included when exporting a story. Use `LocalizedDisplay` and FluentValidation rules for localized labels and validation messages inside block models.

## Creating a custom block
```csharp
[Block("helloworld", FriendlyName = "Hello World", Icon = "fa fa-eye")]
public class HelloWorldBlockHandler : BlockHandlerBase<HelloWorldBlock>
{
}

public class HelloWorldBlock : IBlock
{
    [LocalizedDisplay("Plugins.HelloWorld.Name")]
    public string Name { get; set; }
}
```
The handler metadata defines how the block appears in the palette. Place `Edit.cshtml` and `Public.cshtml` in `Views/Shared/BlockTemplates/helloworld/` to render its configuration UI and public output. Validators ensure the model is valid before saving.

## Further reading
See the [Creating a Block](../../compose/modules/examples/creating-a-block.md) tutorial for a step‑by‑step walkthrough.