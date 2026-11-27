---
description: Use, configure and extend Smartstore icons in Razor and the browser
---

# Icons

Icon Kit is Smartstore's shared system for displaying, organizing and configuring icons. It gives Razor views, JavaScript components and plugins a common way to use icons, regardless of which icon library supplies the drawings.

Smartstore ships with **HugeIcons Free, Stroke Rounded** as the default library. **Bootstrap Icons** and **Font Awesome Free** are also included. You can add further libraries or your own icons by supplying compatible SVG files and a library configuration. The system is not tied to a particular icon vendor.

Icon Kit lets you organize icons into **kits** and reusable **collections**. A kit can combine icons from several libraries, so choosing a default library does not restrict every icon to that library. Shared names such as `cart` or `search` keep your templates independent of the underlying drawings.

The system provides:

- A **Razor Tag Helper** for server-rendered icons.
- A **Web Component** for icons created or updated in the browser.
- **Icon expressions** that combine an icon selection and presentation options in one string.
- **JSON configuration** for libraries, name mappings, kits, collections and defaults.
- **Automatic mirroring** of selected icons in right-to-left layouts.
- An **icon browser in DevTools** that previews icons and generates examples you can copy into your code.

Smartstore handles SVG loading, shared sprites and caching. You work with icon names and presentation options rather than generated filenames.

## Get started

### Understand libraries, kits and collections

| Term | Meaning |
| --- | --- |
| **Library** | A set of source SVGs, such as HugeIcons or Bootstrap Icons. |
| **Variant** | A style within a library, such as Stroke Rounded, Regular or Solid. |
| **Conceptual name** | A shared name such as `cart`, mapped to a drawing in a library. |
| **Collection** | A reusable group of icon definitions that kits can include. |
| **Kit** | A configured selection of icons used together, with its own defaults and applicable areas. |
| **Sprite** | A generated SVG file containing several icon drawings for reuse by the browser. |

A kit is not a library. For example, a frontend kit can use HugeIcons for most interface icons, Font Awesome for brand icons and a custom SVG for a plugin-specific symbol.

A collection does not produce its own sprite. It contributes its icons to the kits that include it. This is useful when the frontend and backend need the same group of common icons.

#### Included libraries

| Library | System name | Included variants |
| --- | --- | --- |
| HugeIcons Free | `hugeicons` | `stroke-rounded` |
| Bootstrap Icons | `bootstrap` | `default` |
| Font Awesome Free | `fontawesome-free` | `regular`, `solid`, `brands` |

Smartstore also includes a `system` library for its own drawings. Libraries can define short names, such as `hi`, `bi` and `fa`, which you may use in icon addresses.

### Start with the DevTools icon browser

The easiest way to find an icon and learn the syntax is the icon browser on the **DevTools plugin's configuration page** in the backend.

You can browse kits and libraries, select a variant, search for icons and experiment with presentation options. The generated example follows your selection.

Choose one of three output formats:

| Format | Use it for |
| --- | --- |
| **Tag Helper** | Razor views and templates |
| **Web Component** | Client-side HTML and JavaScript templates |
| **Icon Expression** | A single string containing the icon selection and presentation options |

Use the copy button to copy the generated example. You do not need to memorize library-specific names or write expressions by hand.

## Use icons in templates

### Use icons in Razor views

Use the `<icon>` Tag Helper:

```cshtml
<icon name="cart" />
```

For example, inside a button:

```cshtml
<button type="button" class="btn btn-primary">
    <icon name="cart" />
    Add to cart
</button>
```

The helper renders an SVG on the server. JavaScript is not required to display it.

#### Prefer conceptual names for common icons

A conceptual name describes the purpose of an icon:

```cshtml
<icon name="cart" />
<icon name="search" />
<icon name="check" />
```

Smartstore resolves the name using the applicable kit definitions, library mappings and configured defaults. For example, the HugeIcons mapping translates `cart` into `shopping-cart-02`. Your template can continue using `cart` when the selected library changes.

The current area also matters. Frontend and backend pages can use different kit selections. Normally, you can leave the area unspecified and let Smartstore use the current request.

#### Select a library and variant

Use `lib` and `variant` when you need a particular selection:

```cshtml
<icon name="cart" lib="hugeicons" variant="stroke-rounded" />
```

You can express the same selection in the name:

```cshtml
<icon name="hugeicons:cart/stroke-rounded" />
```

An icon address has this format:

```text
[library:]name[!][/variant]
```

The brackets indicate optional parts; they are not part of the address.

| Address | Meaning |
| --- | --- |
| `cart` | Resolve `cart` using the applicable defaults. |
| `hugeicons:cart` | Resolve `cart` in HugeIcons using its effective default variant. |
| `hugeicons:cart/stroke-rounded` | Select the library and variant explicitly. |
| `hi:cart/sr` | Use the configured short names for the same selection. |
| `hi:shopping-cart-02!/sr` | Request the original library icon without name mapping. |

The `!` bypasses name mapping and fallback to another library. Configured variant fallbacks still apply. Use an original library name when the exact drawing matters. Prefer conceptual names for common interface elements that should follow the site's icon configuration.

### Adjust the appearance

Both the Tag Helper and the Web Component support attributes for size, color, transformations and animation:

```cshtml
<icon name="cart" size="lg" />
<icon name="check" color="green" />
<icon name="arrow-right" rotate="90" />
<icon name="refresh" animation="spin" animation-duration="2s" />
```

Icons inherit the surrounding text color unless you specify another color:

```cshtml
<span class="text-danger">
    <icon name="alert-danger" class="mr-1" />
    Please check your input.
</span>
```

#### Presentation attributes

| Attribute | Purpose | Example |
| --- | --- | --- |
| `size` | Choose `2xs`, `xs`, `sm`, `lg`, `xl`, `2xl`, or `1x`–`10x`. | `size="2x"` |
| `font-scale` | Use a custom size multiplier instead of a predefined size. | `font-scale="1.5"` |
| `color` | Set a CSS color. | `color="green"` |
| `opacity` | Set opacity between `0` and `1`. | `opacity="0.5"` |
| `fw` | Reserve a fixed-width slot for aligned icons. | `fw="true"` |
| `rotate` | Rotate clockwise in degrees. | `rotate="90"` |
| `flip-h`, `flip-v` | Mirror horizontally or vertically. | `flip-h="true"` |
| `scale` | Resize the drawing without changing its layout size. | `scale="0.75"` |
| `shift-x`, `shift-y` | Move the drawing in units of `1/16 em`. Positive values move right or down. | `shift-y="1"` |
| `stroke-scale` | Adjust line thickness for icons that support it. | `stroke-scale="1.25"` |
| `mirror-rtl` | Override automatic mirroring in RTL layouts. | `mirror-rtl="false"` |
| `inverse` | Use the inverse foreground color. | `inverse="true"` |
| `animation` | Animate the icon. | `animation="spin"` |
| `animation-duration` | Set the duration of the animation. | `animation-duration="800ms"` |
| `animation-reverse` | Reverse animation playback. | `animation-reverse="true"` |

Available animations are `spin`, `pulse`, `beat`, `fade`, `throb`, `bounce`, `flip`, `shake`, `spin-snap`, `swing` and `wag`.

Ordinary HTML attributes such as `class`, `style`, `id` and `data-*` can also be supplied.

### Use icon expressions

An icon address selects an icon. An **icon expression** adds presentation options to that address:

```text
cart?size=lg&rotate=15
```

Expressions are useful when an icon selection must be stored or passed around as one string. The DevTools icon browser can generate them for you.

Use an expression in the `name` attribute:

```cshtml
<icon name="cart?size=lg&amp;rotate=15" />
```

Or write the same options separately:

```cshtml
<icon name="cart" size="lg" rotate="15" />
```

Separate attributes are often easier to read in handwritten templates. Expressions are convenient for values selected through an icon picker or stored in a model.

Explicit presentation attributes override expression values:

```cshtml
<icon name="cart?rotate=15" rotate="0" />
```

This resets the rotation to zero. The expression syntax is also accepted by the Web Component.

### Use icons in the browser

Use `<sm-icon>` for client-side templates, dynamically inserted HTML or icons that change after the page loads:

```html
<sm-icon name="cart" size="lg"></sm-icon>
```

It accepts the same name, library, variant and common presentation attributes as the Razor Tag Helper. Always write the closing tag. Unlike the Razor helper, `<sm-icon>` is a browser element and requires JavaScript.

Standard Smartstore layouts provide the icon script, styles and metadata. You do not need to initialize each icon.

#### Change an icon with JavaScript

```html
<sm-icon id="status-icon" name="refresh"></sm-icon>
```

```javascript
const icon = document.getElementById('status-icon');

// Indicate that an operation is running.
icon.setAttribute('animation', 'spin');

// Update the icon when it finishes.
icon.removeAttribute('animation');
icon.setAttribute('name', 'check');
```

The component reacts to attribute changes automatically. To create an icon dynamically:

```javascript
const icon = document.createElement('sm-icon');
icon.setAttribute('name', 'cart');

document.getElementById('icon-container').append(icon);
```

Use the Web Component when inserting HTML from JavaScript. A dynamically inserted `<icon>` tag will not pass through Razor and therefore will not render an icon.

#### Use icons with Vue

Register the icon elements before mounting your Vue application:

```javascript
const app = Vue.createApp({
    data() {
        return {
            iconName: 'cart'
        };
    }
});

Smartstore.Icons.install(app);
app.mount('#app');
```

Then bind the attributes:

```html
<div id="app">
    <sm-icon :name="iconName" size="lg"></sm-icon>
</div>
```

#### Handle loading errors

The component emits `icon-load` after loading a drawing and `icon-error` when loading or rendering fails:

```javascript
icon.addEventListener('icon-error', event => {
    console.warn('Could not display the icon.', event.detail.error);
});
```

### Configure automatic mirroring in RTL layouts

Some icons should follow the reading direction. Navigation arrows and chevrons are typical examples; logos and many other symbols should retain their orientation.

Configure these names centrally through `mirrorInRtl` in `config.json`:

```json
{
  "defaultLibrary": "hugeicons",
  "defaultVariant": "stroke-rounded",
  "fallbackToDefaultLibrary": true,
  "mirrorInRtl": [
    "arrow-left",
    "arrow-right",
    "chevron-left",
    "chevron-right"
  ]
}
```

With this configuration, a template needs no language-specific condition:

```cshtml
<icon name="arrow-right" />
```

The icon is mirrored automatically in an RTL layout. The same behavior applies to the Web Component. The policy applies to listed, unqualified concepts; explicit library or variant selections and direct `!` addresses do not inherit it.

Override the policy for a particular occurrence when necessary:

```cshtml
<icon name="arrow-right" mirror-rtl="false" />
```

```html
<sm-icon name="arrow-right" mirror-rtl="false"></sm-icon>
```

Leaving `mirror-rtl` unset preserves the configured policy. Setting it to `false` explicitly disables mirroring for that occurrence; `true` enables it, including for an explicit address.

### Combine icons in a stack

The first icon forms the background; subsequent icons appear above it. Set their sizes explicitly: a `2x` background fills the stack's default `2em × 2em` area, while a `1x` foreground occupies half its width and height.

```cshtml
<icon-stack size="lg" aria-label="Completed">
    <icon name="system:circle!/fill" size="2x" color="green" />
    <icon name="check" size="1x" color="white" />
</icon-stack>
```

Set `size` or `font-scale` on the stack to resize the complete composition while preserving these proportions. Adjust `size` or `scale` on an individual icon to change its size relative to the other layers. Layer order does not assign sizes automatically.

Layers are centered by default. Use `position` to align a layer vertically and horizontally, for example `start-end` for the top trailing corner or `end-end` for the bottom trailing corner. Horizontal `start` and `end` follow the text direction. Use `shift-x` and `shift-y` for small adjustments.

The Web Components follow the same rules:

```html
<sm-icon-stack size="lg" aria-label="Completed">
    <sm-icon name="system:circle!/fill" size="2x" color="green"></sm-icon>
    <sm-icon name="check" size="1x" color="white"></sm-icon>
</sm-icon-stack>
```

### Make icons accessible

Icons beside descriptive text are usually decorative. Unlabelled icons are hidden from assistive technology by default.

For an icon-only button, label the button:

```cshtml
<button type="button" aria-label="Search">
    <icon name="search" />
</button>
```

For a standalone icon that conveys information, label the icon:

```cshtml
<icon name="check" aria-label="Completed" />
```

```html
<sm-icon name="check" aria-label="Completed"></sm-icon>
```

Both rendering options support `aria-label` and `aria-labelledby`. Localize accessible labels just like visible text.

### Use shared sprites

Kits let Smartstore reuse groups of drawings through shared SVG sprites. Both server-rendered icons and the Web Component use the system's resolved icon selections.

Views should not construct sprite filenames, resolve physical paths or manage cache files. Use the Tag Helper or Web Component and let Icon Kit select the appropriate rendering path.

If a particular Razor icon must embed its drawing directly instead of referencing a sprite, use:

```cshtml
<icon name="cart" force-inline="true" />
```

This option belongs to the Razor Tag Helper. Normal usage does not require it.

## Configure libraries, kits and collections

Icon sources and configuration live under `App_Data/Icons` by default. Configuration is stored as JSON; the drawings are SVG files, usually packaged in a ZIP archive per variant.

A simplified structure looks like this:

```text
App_Data/
└── Icons/
    ├── config.json
    ├── kits.json
    ├── collections.json
    ├── hugeicons/
    │   ├── library.json
    │   ├── mapping.json
    │   ├── metadata.json
    │   └── stroke-rounded/
    │       ├── icons.zip
    │       ├── icons/                 Optional loose SVGs
    │       └── user/                  Optional custom SVGs
    ├── bootstrap/
    │   ├── library.json
    │   ├── mapping.json
    │   ├── metadata.json
    │   └── default/
    │       └── icons.zip
    └── fontawesome-free/
        ├── library.json
        ├── mapping.json
        ├── metadata.json
        ├── regular/
        │   └── icons.zip
        ├── solid/
        │   └── icons.zip
        └── brands/
            └── icons.zip
```

| File | Purpose |
| --- | --- |
| `config.json` | Global defaults, library fallback and automatic RTL mirroring. |
| `kits.json` | Kit definitions, included icons, collections, source selections and areas. |
| `collections.json` | Reusable groups of icon definitions. |
| `library.json` | Library information, variants, short names and variant settings. |
| `mapping.json` | Maps shared names to the library's original icon names or expressions. |
| `metadata.json` | Additional information used for searching, such as tags. |
| `icons.zip` | SVG drawings belonging to a library variant. |

The ZIP archives keep large libraries together without requiring thousands of loose files on disk. You do not need to extract them to use the icons. Archived SVGs belong at the ZIP root.

Generated sprites and manifests are stored separately, under `App_Data/.cache/icons` by default. Configure the source files rather than editing generated cache files.

### Define name mappings

A library's `mapping.json` connects conceptual names to its drawings. For example, HugeIcons uses:

```json
{
  "cart": "shopping-cart-02",
  "cart-add": "shopping-cart-add-02"
}
```

Templates can use `cart` and `cart-add`, while the library-specific names remain in configuration. Mappings can also include transformations when needed to match the intended meaning.

### Define collections

A collection groups icons that several kits need. For example, an entry in `collections.json` could define common shopping actions:

```json
{
  "shopping-actions": {
    "icons": [
      "cart",
      "cart-add",
      "check"
    ]
  }
}
```

Collections can include other collections through `use`. They contribute definitions to kits and do not generate separate sprites.

### Define kits

A kit includes its own icons and any collections it references. For example, an entry in `kits.json` could be:

```json
{
  "checkout": {
    "displayName": "Checkout",
    "defaultLibrary": "hugeicons",
    "defaultVariant": "stroke-rounded",
    "areas": [
      "public"
    ],
    "use": [
      "shopping-actions"
    ],
    "icons": [
      "search"
    ]
  }
}
```

This kit includes the three collection icons plus `search`. The library and variant provide defaults for resolving the kit's icons. They do not prevent the kit from containing icons from other libraries.

To select a particular source for a member, add a `sources` entry to the kit:

```json
"sources": {
  "cart": "fontawesome-free:cart-shopping!/solid"
}
```

Here, `cart` uses a Font Awesome drawing while other members can continue using HugeIcons.

The examples show individual definitions. Add them to the existing configuration rather than replacing all supplied kits or collections.

### Add custom SVGs and overrides

Within a variant, Smartstore looks for a drawing in this order:

1. `user/<name>.svg`
2. `icons/<name>.svg`
3. The matching entry in `icons.zip`

For example:

```text
hugeicons/stroke-rounded/user/shopping-cart-02.svg
```

This replaces that drawing without modifying the original archive. Existing mappings to `shopping-cart-02` continue to work.

You can integrate another SVG icon library by providing its library configuration, variants and drawings in this structure. The SVGs must be compatible with Smartstore's SVG parser; an icon font or a vendor's JavaScript package alone is not a library installation.

### Change the frontend's icon library

The frontend can use another icon library without rewriting templates that use conceptual names such as `cart`, `search` or `user`.

For example, after installing Font Awesome Pro, change these two properties inside the existing `public` entry in `kits.json`:

```json
"defaultLibrary": "fontawesome-pro",
"defaultVariant": "light"
```

Keep the kit's other properties, icons and collections unchanged. Leave the global defaults in `config.json` and the `admin` kit unchanged so backend icons continue using their existing defaults.

A template can still render:

```cshtml
<icon name="cart" />
```

The public kit now resolves that concept using Font Awesome Pro. Other frontend kits have their own defaults and may need the same adjustment. Explicit library addresses and pinned kit sources retain their selected library. Installing Pro as a replacement for Free still affects `fa:` references in either area; the kit-default change itself is limited to the frontend.

#### Check the mappings

The selected library's `mapping.json` must connect the frontend's concepts to available drawings. For example:

```json
{
  "cart": "cart-shopping",
  "search": "magnifying-glass",
  "user": "user"
}
```

Check that each target exists in the selected variant or its configured variant fallbacks. A mapping is unnecessary when the concept already matches the original icon name.

With `fallbackToDefaultLibrary` enabled, a missing icon may fall back to the global default library, HugeIcons in the supplied configuration. This keeps an icon visible but can unintentionally mix visual styles. Verify the frontend selection in DevTools and complete the mappings instead of relying on fallback to hide missing coverage.

## Provide icons from a plugin

A plugin can add icons to existing kits and collections through `IIconProvider`. It can reference installed libraries or supply SVG files of its own. Consumers use those icons by name, through the same Tag Helper and Web Component.

Place an `Icons.cs` file in the module project's root directory. Smartstore discovers icon providers in installed plugins automatically; no manual DI registration is required. See [Getting started with modules](../../compose/modules/getting-started-with-modules.md) for the module structure.

### Add icons from an existing library

This example adds two plugin-specific names to the backend kit:

```csharp
using Smartstore.IconKit;

namespace MyCompany.OrderTools;

/// <summary>
/// Adds icons used by the Order Tools plugin.
/// </summary>
internal sealed class Icons : IIconProvider
{
    public void Configure(IconKitBuilder builder)
    {
        builder.Kit("admin")
            .Add("ordertool-cart", "hi:shopping-cart-02!/sr")
            .Add("ordertool-confirm", "hi:check!/sr");
    }
}
```

The first argument to `Add` is the name exposed to templates. The second selects its concrete source. Both entries here use specific HugeIcons drawings. To contribute an existing concept using normal library mapping, omit the source, for example `.Add("cart")`.

In a backend view, render the contributed icons by name:

```cshtml
<icon name="ordertool-cart" />
<icon name="ordertool-confirm" />
```

Use a plugin-specific prefix for concepts that belong to your plugin to avoid collisions. The selected kit must already exist. Use `public` for frontend contributions or `admin` for backend contributions.

### Provide a custom icon

Keep custom drawings inside the module's `wwwroot/icons` folder:

```text
MyCompany.OrderTools/
├── Icons.cs
├── Module.cs
├── module.json
└── wwwroot/
    └── icons/
        └── order-review.svg
```

For example, `order-review.svg` could contain:

```xml
<svg xmlns="http://www.w3.org/2000/svg"
     viewBox="0 0 24 24"
     fill="none"
     stroke="currentColor"
     stroke-width="1.5"
     stroke-linecap="round"
     stroke-linejoin="round">
    <rect x="5" y="3" width="14" height="18" rx="2" />
    <path d="M8 8h8M8 12h8M8 16h4" />
</svg>
```

The `viewBox` allows the drawing to scale. Using `currentColor` makes it follow the surrounding text color.

Register it in `Icons.cs`:

```csharp
using Smartstore.IconKit;

namespace MyCompany.OrderTools;

/// <summary>
/// Provides icons used by the Order Tools plugin.
/// </summary>
internal sealed class Icons : IIconProvider
{
    public void Configure(IconKitBuilder builder)
    {
        builder.Kit("admin")
            .AddSvgFile(
                "ordertool-review",
                "~/Modules/MyCompany.OrderTools/wwwroot/icons/order-review.svg");
    }
}
```

`AddSvgFile` assigns the name `ordertool-review` to the SVG and adds it to the existing `admin` kit. Its path is an application-relative file path, including `wwwroot`, rather than a browser URL.

Use the contributed name in a backend view:

```cshtml
<icon name="ordertool-review" size="lg" />
```

Or in client-side HTML:

```html
<sm-icon name="ordertool-review" size="lg"></sm-icon>
```

There is no need to copy the SVG into `App_Data/Icons`, register a separate library or construct a sprite URL. Icon Kit handles its inclusion in the kit. Existing library icons and custom SVGs can be contributed by the same provider.

{% hint style="info" %}
Files supplied through `AddSvgFile` do not have their own change watcher. Changes are picked up when the icon catalog is rebuilt. Restart the application when checking a changed plugin drawing during development.
{% endhint %}

### Add icons to a shared collection

If several kits should receive the same contribution, add it to a collection they include:

```csharp
builder.Collection("shared")
    .AddSvgFile(
        "ordertool-review",
        "~/Modules/MyCompany.OrderTools/wwwroot/icons/order-review.svg");
```

Every kit that includes `shared` receives the icon. With the supplied configuration, this is useful for icons needed in both frontend and backend views. Kits select collections through their `use` configuration; creating a collection alone does not include it in every kit.

### Supply SVG markup directly

For a small drawing, `AddSvg` accepts a complete SVG document:

```csharp
builder.Kit("admin")
    .AddSvg(
        "ordertool-marker",
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
            <path fill="currentColor" d="M12 2 22 12 12 22 2 12Z" />
        </svg>
        """);
```

Use a separate SVG file when the drawing is substantial or should be editable in a graphics tool.

### Keep provider definitions independent of requests

`Configure` supplies application-wide icon definitions. Keep it independent of the current user, language or request, and do not resolve icons through `IIconService` while defining them.

Local JSON configuration takes precedence over provider defaults. This allows an installation to customize a plugin's icon selection without changing the plugin itself.

## Troubleshoot icons

**An `<icon>` tag appears in the output instead of an SVG.** The Tag Helper did not run. Check that the markup is in a Razor view and that its `_ViewImports.cshtml` registers the shared Smartstore helpers:

```cshtml
@addTagHelper Smartstore.Web.TagHelpers.Shared.*, Smartstore.Web.Common
```

**A Web Component stays empty.** Check that the page loads Smartstore's icon script and styles. Custom layouts must also preserve the icon metadata supplied by the standard layouts.

**An icon looks different from the expected drawing.** Check the applicable kit, selected library, variant and name mapping. Use the DevTools icon browser to inspect the selection and generate an explicit address if necessary.

**A custom drawing does not replace the original.** Check that the override is in the correct library and variant folder and uses the original drawing's filename. For example, a mapped concept named `cart` may resolve to a file named `shopping-cart-02.svg`.

**An arrow does not mirror in RTL.** Check `mirrorInRtl`, the page's text direction and whether the icon uses an explicit source or sets `mirror-rtl="false"`.

## Migrate existing icons

When updating a theme or plugin, replace legacy Font Awesome markup and Bootstrap icon helpers with Icon Kit. Update both the rendered markup and any code that supplies or changes the icon name.

The old font-based rendering is no longer available. Keeping classes such as `fa`, `fas` or `far` on a new element does not preserve the old behavior.

### Replace the rendering markup

For a common interface icon, use the corresponding concept:

```cshtml
@* Before *@
<i class="far fa-trash-can mr-2" aria-hidden="true"></i>
<bootstrap-icon name="trash" />

@* After *@
<icon name="trash" class="mr-2" />
```

If you deliberately want to retain a specific library's drawing, use a direct address instead:

```cshtml
<icon name="fa:trash-can!/r" />
<icon name="bi:trash!" />
```

Direct addresses retain the selected library. The requested icon and style must be available in the installation or through its configured variant fallbacks.

| Previous API or markup | Replacement |
| --- | --- |
| Font Awesome `<i class="…">` | `<icon name="…" />` in Razor; `<sm-icon name="…"></sm-icon>` in client-side HTML |
| `<bootstrap-icon>` | `<icon>` |
| `<bootstrap-iconstack>` | `<icon-stack>` with individual `<icon>` layers |
| `Html.Icon()` or `Html.BootstrapIcon()` | `@await Html.IconAsync("…")` |
| Vue `bootstrap-icon` | `sm-icon`; register `Smartstore.Icons` before mounting |
| JavaScript that changes `fa-*` glyph classes | Change the Web Component's `name` attribute |
| An address using `@variant` | Use `/variant` |

Keep existing IDs, application-specific classes, titles and accessible labels. Update selectors that depend on the old element type or Font Awesome classes.

### Replace presentation classes

Convert font-specific presentation together with the icon itself:

| Legacy presentation | Icon Kit |
| --- | --- |
| `fa-fw`, `bi-fw` | `fw="true"` or `class="icon-fw"` |
| `fa-lg`, `fa-2x`, … | `size="lg"`, `size="2x"`, … |
| `fa-spin` | `animation="spin"` |
| `fa-pulse` | `animation="pulse"` |
| `fa-rotate-90` | `rotate="90"` |
| `fa-flip-horizontal` | `flip-h="true"` |
| `fa-flip-vertical` | `flip-v="true"` |
| `fa-inverse` | `inverse="true"` |
| `fa-stack`, `fa-stack-1x`, `fa-stack-2x` | An `icon-stack` with explicit layer sizes |

For example:

```cshtml
@* Before *@
<i class="fas fa-spinner fa-spin fa-fw"></i>

@* After *@
<icon name="spinner" animation="spin" fw="true" />
```

Migrate complete stacks together and check the result visually. Background fill, foreground size and positioning may need adjustment when changing libraries.

### Update dynamic icons

Use the Web Component when JavaScript changes an icon:

```html
<sm-icon id="save-status" name="spinner" animation="spin"></sm-icon>
```

```javascript
const icon = document.getElementById('save-status');

icon.removeAttribute('animation');
icon.setAttribute('name', 'check');
```

Check every state, including loading, success, failure, expanded and collapsed states. Replacing the initial markup alone leaves old class-based updates ineffective.

Also review icon values in navigation items, tabs, grid definitions, module metadata and models. Where an API now accepts an icon address, supply `search` or `fa:magnifying-glass!/s`, not a CSS class string.

### Font Awesome replacements

Use the **Concept** column as the new `name` value. The table lists the reviewed replacements from the migration reference. Style prefixes such as `far`, `fas` and `fal` are omitted to keep it compact.

These are replacements by meaning, not a promise to preserve the original drawing or weight. Use a direct Font Awesome address when that distinction matters.

| Legacy Font Awesome class | Concept |
| --- | --- |
| `fa-address-book` | `address-book` |
| `fa-exclamation-circle`, `fa-circle-exclamation` | `alert-danger` |
| `fa-arrow-down` | `arrow-down` |
| `fa-circle-down` | `arrow-down-circle` |
| `fa-arrow-left` | `arrow-left` |
| `fa-arrow-right` | `arrow-right` |
| `fa-turn-up` | `arrow-turn-up` |
| `fa-arrow-up` | `arrow-up` |
| `fa-circle-up` | `arrow-up-circle` |
| `fa-award` | `award` |
| `fa-ban` | `ban` |
| `fa-chart-bar` | `chart-bar` |
| `fa-barcode` | `barcode` |
| `fa-bars` | `bars` |
| `fa-bars-staggered` | `bars-staggered` |
| `fa-shopping-basket` | `basket` |
| `fa-bell` | `bell` |
| `fa-blog` | `blog` |
| `fa-bold` | `bold` |
| `fa-bolt` | `bolt` |
| `fa-book` | `book` |
| `fa-box-open` | `box-open` |
| `fa-brackets-curly` | `braces` |
| `fa-building` | `building` |
| `fa-calendar`, `fa-calendar-days` | `calendar` |
| `fa-calendar-check` | `calendar-check` |
| `fa-caret-down` | `caret-down` |
| `fa-shopping-cart` | `cart` |
| `fa-cart-arrow-down` | `cart-add` |
| `fa-certificate` | `certificate` |
| `fa-check` | `check` |
| `fa-check-double` | `check-double` |
| `fa-angle-down`, `fa-chevron-down` | `chevron-down` |
| `fa-angle-left`, `fa-chevron-left` | `chevron-left` |
| `fa-angle-right`, `fa-chevron-right` | `chevron-right` |
| `fa-angle-up`, `fa-chevron-up` | `chevron-up` |
| `fa-angle-double-down` | `chevrons-down` |
| `fa-angles-left` | `chevrons-left` |
| `fa-angles-right` | `chevrons-right` |
| `fa-angle-double-up` | `chevrons-up` |
| `fa-circle` | `circle` |
| `fa-clipboard` | `clipboard` |
| `fa-clock` | `clock` |
| `fa-times`, `fa-xmark` | `close` |
| `fa-cloud-arrow-up` | `cloud-upload` |
| `fa-code` | `code-xml` |
| `fa-comment-dots` | `comment` |
| `fa-comments` | `comments` |
| `fa-adjust` | `contrast` |
| `fa-clone`, `fa-copy` | `copy` |
| `fa-credit-card` | `creditcard` |
| `fa-css3` | `css3` |
| `fa-cube` | `cube` |
| `fa-database` | `database` |
| `fa-phone-laptop` | `devices` |
| `fa-save` | `disk` |
| `fa-download` | `download` |
| `fa-droplet-slash` | `droplet-off` |
| `fa-edit` | `edit` |
| `fa-ellipsis`, `fa-ellipsis-h` | `ellipsis` |
| `fa-ellipsis-v` | `ellipsis-v` |
| `fa-envelope` | `mail` |
| `fa-envelope-open-text` | `mail-open-text` |
| `fa-euro-sign` | `euro` |
| `fa-right-left` | `exchange` |
| `fa-exclamation` | `exclamation` |
| `fa-up-right-from-square` | `external-link` |
| `fa-eye` | `eye` |
| `fa-eye-slash` | `eye-off` |
| `fa-facebook` | `facebook` |
| `fa-file` | `file` |
| `fa-file-archive` | `file-archive` |
| `fa-file-audio` | `file-audio` |
| `fa-file-code` | `file-code` |
| `fa-file-csv` | `file-csv` |
| `fa-file-image` | `file-image` |
| `fa-file-pdf` | `file-pdf` |
| `fa-file-powerpoint` | `file-presentation` |
| `fa-file-excel` | `file-spreadsheet` |
| `fa-file-lines`, `fa-file-text` | `file-text` |
| `fa-file-video` | `file-video` |
| `fa-file-word` | `file-word` |
| `fa-film` | `film` |
| `fa-filter` | `filter` |
| `fa-fire` | `fire` |
| `fa-folder` | `folder` |
| `fa-folder-open` | `folder-open` |
| `fa-font` | `font` |
| `fa-forward` | `forward` |
| `fa-cog` | `gear` |
| `fa-gift`, `fa-gifts` | `gift` |
| `fa-github` | `github` |
| `fa-globe` | `globe` |
| `fa-google` | `google` |
| `fa-th` | `grid` |
| `fa-th-large` | `grid-large` |
| `fa-grip` | `grip` |
| `fa-grip-vertical` | `grip-v` |
| `fa-hammer` | `hammer` |
| `fa-dolly` | `folder-transfer` |
| `fa-heart` | `heart` |
| `fa-home` | `home` |
| `fa-hourglass-start` | `hourglass` |
| `fa-html5` | `html5` |
| `fa-image` | `image` |
| `fa-inbox` | `inbox` |
| `fa-info` | `info` |
| `fa-circle-info`, `fa-info-circle` | `info-circle` |
| `fa-instagram` | `instagram` |
| `fa-italic` | `italic` |
| `fa-key` | `key` |
| `fa-language` | `language` |
| `fa-laptop` | `laptop` |
| `fa-layer-group` | `layers` |
| `fa-link` | `link` |
| `fa-linkedin` | `linkedin` |
| `fa-list`, `fa-stream`, `fa-th-list` | `list` |
| `fa-rectangle-list` | `list-square` |
| `fa-location-dot` | `location` |
| `fa-lock` | `lock` |
| `fa-lock-open` | `lock-open` |
| `fa-right-to-bracket` | `login` |
| `fa-right-from-bracket` | `logout` |
| `fa-wand-magic-sparkles` | `sparkles` |
| `fa-mail-forward` | `mail-forward` |
| `fa-expand` | `maximize` |
| `fa-bullhorn` | `megaphone` |
| `fa-compress` | `minimize` |
| `fa-minus` | `minus` |
| `fa-mobile-alt`, `fa-mobile-screen-button` | `mobile` |
| `fa-money-bill-1` | `money` |
| `fa-desktop`, `fa-display` | `monitor` |
| `fa-up-down-left-right` | `move` |
| `fa-newspaper` | `newspaper` |
| `fa-fill-drip` | `paint-bucket` |
| `fa-paint-brush`, `fa-paintbrush` | `paint-brush` |
| `fa-paperclip` | `paperclip` |
| `fa-pause` | `pause` |
| `fa-pause-circle` | `pause-circle` |
| `fa-pencil` | `pen` |
| `fa-phone`, `fa-phone-flip` | `phone` |
| `fa-eye-dropper` | `dropper` |
| `fa-caret-right`, `fa-play` | `play` |
| `fa-play-circle` | `play-circle` |
| `fa-plus` | `plus` |
| `fa-plus-circle` | `plus-circle` |
| `fa-plus-square` | `plus-square` |
| `fa-power-off` | `power` |
| `fa-print` | `printer` |
| `fa-puzzle-piece` | `puzzle` |
| `fa-question` | `question` |
| `fa-circle-question`, `fa-question-circle` | `question-circle` |
| `fa-quote-left` | `quotes` |
| `fa-recycle` | `recycle` |
| `fa-redo` | `redo` |
| `fa-rotate`, `fa-sync` | `refresh` |
| `fa-retweet` | `repeat` |
| `fa-reply` | `reply` |
| `fa-reply-all` | `reply-all` |
| `fa-robot` | `robot` |
| `fa-rotate-left` | `rotate-left` |
| `fa-rotate-right` | `rotate-right` |
| `fa-rss` | `rss` |
| `fa-ruler-combined` | `ruler` |
| `fa-balance-scale` | `balance` |
| `fa-cut` | `cut` |
| `fa-magnifying-glass`, `fa-search` | `search` |
| `fa-paper-plane`, `fa-send` | `send` |
| `fa-cogs` | `gears` |
| `fa-share` | `share` |
| `fa-share-square` | `share-square` |
| `fa-shield-halved` | `shield-half` |
| `fa-signal` | `signal` |
| `fa-sitemap` | `nodes` |
| `fa-sliders-h` | `sliders` |
| `fa-spinner` | `spinner` |
| `fa-square`, `fa-stop` | `square` |
| `fa-star` | `star` |
| `fa-star-half` | `star-half` |
| `fa-tablet-alt`, `fa-tablet-screen-button` | `tablet` |
| `fa-tag` | `tag` |
| `fa-tags` | `tags` |
| `fa-tasks` | `tasks` |
| `fa-thumbs-down` | `thumbs-down` |
| `fa-thumbs-up` | `thumbs-up` |
| `fa-delete`, `fa-trash`, `fa-trash-alt`, `fa-trash-can` | `trash` |
| `fa-trophy` | `trophy` |
| `fa-truck`, `fa-truck-moving` | `truck` |
| `fa-truck-loading` | `truck-loading` |
| `fa-tv` | `tv` |
| `fa-x-twitter` | `twitter-x` |
| `fa-underline` | `underline` |
| `fa-undo` | `undo` |
| `fa-unlink` | `unlink` |
| `fa-unlock-keyhole` | `unlock` |
| `fa-arrow-up-from-bracket`, `fa-upload` | `upload` |
| `fa-user` | `user` |
| `fa-user-plus` | `user-add` |
| `fa-user-check` | `user-check` |
| `fa-user-circle` | `user-circle` |
| `fa-user-edit` | `user-edit` |
| `fa-user-secret` | `privacy` |
| `fa-user-shield` | `user-shield` |
| `fa-group`, `fa-users` | `user-group` |
| `fa-wallet` | `wallet` |
| `fa-exclamation-triangle`, `fa-triangle-exclamation`, `fa-warning` | `warning` |
| `fa-window-restore` | `browser` |
| `fa-wrench` | `wrench` |
| `fa-youtube` | `youtube` |

Some replacements intentionally differ from the old artwork. With HugeIcons, `blog` uses an RSS drawing, `caret-down` uses a chevron, and `file-presentation` uses a generic document.

Where fill carries meaning, choose an explicit shape concept: `circle-fill` for a status dot, `square-fill` for a stop button, and `star-fill` or `star-outline` for ratings. Do not treat grouped legacy names as permission to discard a meaningful fill or style distinction.

Brand concepts can resolve through the brands kit to Font Awesome rather than HugeIcons.

### Bootstrap Icons replacements

Use the old `name` value to find the replacement concept:

| Legacy Bootstrap name | Concept |
| --- | --- |
| `archive` | `archive` |
| `arrow-90deg-up` | `arrow-turn-up` |
| `arrow-up-short` | `arrow-up` |
| `aspect-ratio` | `aspect-ratio` |
| `bar-chart-line` | `chart-bar` |
| `lightning` | `bolt` |
| `box` | `box` |
| `boxes` | `boxes` |
| `braces-asterisk` | `braces-asterisk` |
| `building` | `building` |
| `camera-video` | `camera-video` |
| `cart-check` | `cart-check` |
| `check`, `check-lg` | `check` |
| `chevron-left` | `chevron-left` |
| `chevron-right` | `chevron-right` |
| `chevron-double-left` | `chevrons-left` |
| `chevron-double-right` | `chevrons-right` |
| `circle-fill` | `circle-fill` |
| `clipboard` | `clipboard` |
| `clock` | `clock` |
| `x`, `x-lg` | `close` |
| `x-circle` | `close-circle` |
| `columns-gap` | `dashboard` |
| `chat-dots` | `comment` |
| `chat-square-text`, `chat-square-text-fill` | `comment-text` |
| `copy` | `copy` |
| `credit-card` | `creditcard` |
| `database-add` | `database-add` |
| `database-down` | `database-export` |
| `database-up` | `database-import` |
| `floppy` | `disk` |
| `download` | `download` |
| `pencil-square` | `edit` |
| `three-dots` | `ellipsis` |
| `envelope` | `mail` |
| `envelope-open-heart` | `mail-heart` |
| `envelope-paper` | `mail-open` |
| `eraser` | `eraser` |
| `currency-euro` | `euro` |
| `arrow-left-right` | `exchange` |
| `eye`, `eye-fill` | `eye` |
| `facebook` | `facebook` |
| `file-earmark-pdf` | `file-pdf` |
| `file-earmark-richtext` | `file-pen` |
| `funnel` | `filter` |
| `gear` | `gear` |
| `gift` | `gift` |
| `globe-americas` | `earth` |
| `grid-3x3-gap` | `grid` |
| `hdd` | `hdd` |
| `heart` | `heart` |
| `card-image`, `image` | `image` |
| `images` | `images` |
| `inboxes` | `inbox` |
| `info-circle` | `info-circle` |
| `input-cursor-text` | `input-cursor` |
| `stack` | `layers` |
| `layers-half` | `background` |
| `lightbulb` | `lightbulb` |
| `graph-up` | `chart-up` |
| `graph-up-arrow` | `chart-line-arrow` |
| `link-45deg` | `link` |
| `list-ul`, `view-list` | `list` |
| `geo-alt` | `location` |
| `box-arrow-in-right` | `login` |
| `box-arrow-left` | `logout` |
| `megaphone` | `megaphone` |
| `menu-button-fill` | `menu-button` |
| `palette` | `palette` |
| `vector-pen` | `pen-nib` |
| `pencil`, `pencil-fill` | `pen` |
| `percent` | `discount` |
| `play-fill` | `play` |
| `power` | `power` |
| `printer` | `printer` |
| `puzzle-fill` | `puzzle` |
| `question-circle-fill` | `question-circle` |
| `receipt` | `receipt` |
| `recycle` | `recycle` |
| `arrow-repeat`, `repeat` | `repeat` |
| `arrows-expand-vertical` | `unfold` |
| `arrow-clockwise` | `rotate-right` |
| `scissors` | `cut` |
| `search` | `search` |
| `send` | `send` |
| `shield` | `shield` |
| `shuffle` | `shuffle` |
| `diagram-3` | `nodes` |
| `sliders` | `sliders` |
| `sliders2-vertical` | `sliders-v` |
| `sort-down-alt` | `sort-down` |
| `sort-numeric-down` | `sort-numeric` |
| `sort-up-alt` | `sort-up` |
| `stars-tricolor` | `stars-tricolor` |
| `star-half` | `star-half` |
| `stickies` | `sticky-note` |
| `shop-window` | `store` |
| `tags` | `tags` |
| `card-checklist`, `list-check` | `tasks` |
| `text-wrap` | `text-wrap` |
| `textarea-resize` | `text-resize` |
| `hand-thumbs-down` | `thumbs-down` |
| `hand-thumbs-up` | `thumbs-up` |
| `tools` | `tools` |
| `trash` | `trash` |
| `trophy` | `trophy` |
| `truck` | `truck` |
| `upload` | `upload` |
| `person` | `user` |
| `person-badge` | `user-badge` |
| `person-circle` | `user-circle` |
| `people`, `people-fill` | `user-group` |
| `exclamation-triangle` | `warning` |
| `emoji-wink` | `wink` |

These tables cover reviewed migrations, not every icon available in either library. For an unlisted drawing, use DevTools to find an appropriate concept or retain the Bootstrap source with an address such as `bi:building-check!`.

Bootstrap Icons uses a single `default` variant. Filled drawings retain their original filenames, such as `heart-fill`; there are no synthetic Bootstrap `light` or `solid` variants.

### Check CSS and stored content

Replace font-glyph pseudo-elements with the shared Sass helpers:

```scss
.toggle::after {
    @include icon-mask("chevron-down", .75rem);
}
```

Remove the old font family, Unicode `content` and Font Awesome mixins. Rebuild the relevant CSS bundles after changing embedded icon artwork.

The mapping tables are intended for reviewing application code. Do not apply them blindly to user-authored content. Smartstore's data migration preserves explicit Font Awesome selections and styles as direct addresses rather than substituting the concept table.

Plugin-owned stored content may need its own migration integration through `IconMigrationEvent` or `IIconMigrationProvider`. This is separate from `IIconProvider`, which supplies icon definitions.

Finally, check migrated templates in their actual frontend or backend context, including dynamic states, RTL layouts, stacks and any icons contributed by optional plugins.

## Add Font Awesome Pro

Font Awesome Pro can be installed as an SVG library and configured to replace the bundled Free edition. Existing `fa:` addresses can then use the Pro library without changing every template.

{% hint style="info" %}
Smartstore does not distribute Font Awesome Pro because it contains licensed assets. You are responsible for obtaining the appropriate license and supplying the SVG files for your installation.
{% endhint %}

Icon Kit does not require the Font Awesome webfont, stylesheet or JavaScript renderer.

### Prepare the library files

Create a `fontawesome-pro` directory beneath `App_Data/Icons`. Package each selected style's SVG files into its own `icons.zip`:

```text
App_Data/Icons/fontawesome-pro/
├── library.json
├── mapping.json
├── metadata.json
├── regular/
│   └── icons.zip
├── solid/
│   └── icons.zip
├── light/
│   └── icons.zip
├── brands/
│   └── icons.zip
└── duotone-solid/
    └── icons.zip
```

**Install only the variants you intend to use.** This directory structure and the configuration below are examples, not a requirement to deploy every Pro style. Keep `library.json` consistent with your selection: its default variant and fallback references must point to declared variants. If existing templates use styles you do not install, either provide suitable fallback variants or update those icon selections.

Each archive must contain the SVG files directly at its root, not inside an enclosing style directory:

```text
icons.zip
├── address-book.svg
├── cart-shopping.svg
├── check.svg
└── …
```

Preserve the original filenames and SVG `viewBox` values. Use the package's SVG sources; do not put the complete vendor download into a single archive.

### Declare the variants

Create `library.json`. This example includes Regular, Solid, Light, Brands and Duotone Solid:

```json
{
  "displayName": "Font Awesome Pro",
  "shortName": "fa",
  "replaces": "fontawesome-free",
  "defaultVariant": "regular",
  "variants": {
    "regular": {
      "displayName": "Regular",
      "shortName": "r",
      "fallbacks": ["solid"]
    },
    "solid": {
      "displayName": "Solid",
      "shortName": "s"
    },
    "light": {
      "displayName": "Light",
      "shortName": "l",
      "fallbacks": ["regular", "solid"]
    },
    "brands": {
      "displayName": "Brands",
      "shortName": "b"
    },
    "duotone-solid": {
      "displayName": "Duotone Solid",
      "shortName": "ds",
      "isDuotone": true,
      "primaryPathSelector": "path[2]",
      "secondaryPathSelector": "path[1]"
    }
  }
}
```

Set `version` to the version of your source package if you want to record it.

The duotone selectors above match the Font Awesome Pro 7+ layout described by the Icon Kit integration: the second path is the primary layer and the first path is the secondary layer. Check the SVG structure when importing a different package format.

To preserve existing Free addresses, retain their variant names and short names: `regular`/`r`, `solid`/`s` and `brands`/`b`. Retaining a variant declaration does not require shipping that style's SVGs if you deliberately provide a suitable fallback, but the fallback can change its appearance.

### Supply mappings and search metadata

The replacement library is independent: **Smartstore does not merge Free's files into Pro**.

Use the supplied `fontawesome-pro/mapping.json` as the starting point for conceptual names. Check that its targets exist in the installed package and preserve any local mapping changes.

Supply `metadata.json` for search tags and inventory information. It follows the same format as other libraries:

```json
{
  "icons": {
    "cart-shopping": {
      "tags": ["basket", "checkout"]
    },
    "magnifying-glass": {
      "tags": ["search", "find"]
    }
  }
}
```

Metadata improves searching; actual availability is determined by the SVG sources.

### Choose how Pro is used

The `replaces` setting makes `fontawesome-free` and its `fa` alias resolve to the Pro library. It does not change the global default from HugeIcons to Font Awesome.

To use Pro only where requested, leave the global configuration unchanged:

```cshtml
<icon name="fa:cart-shopping!/l" />
<icon name="fa:cart-shopping!/ds" />
```

To make Pro the global default, update these properties in `config.json`, preserving the file's other settings:

```json
"defaultLibrary": "fontawesome-pro",
"defaultVariant": "light"
```

You can instead select Pro in an individual kit's `defaultLibrary` and `defaultVariant`. Explicit source selections in that kit remain pinned to their configured sources. See [Change the frontend's icon library](#change-the-frontends-icon-library) for a frontend-only change of kit defaults.

Configured fallbacks can substitute another style when a drawing is absent. If a particular style is essential, check the resolved icon in DevTools rather than assuming the requested style was available.

### Style duotone icons

Duotone variants expose separate color and opacity variables:

```cshtml
<icon name="fa:cart-shopping!/ds"
      size="2x"
      style="--icon-primary-color: navy;
             --icon-secondary-color: cornflowerblue;
             --icon-secondary-opacity: 0.4;" />
```

The variables are `--icon-primary-color`, `--icon-secondary-color`, `--icon-primary-opacity` and `--icon-secondary-opacity`. They also work when the drawing is rendered through a shared sprite.

### Verify deployment

Open the DevTools icon browser and check the installed variants, a mapped concept such as `cart`, a direct Pro address and any duotone styles you intend to use.

The standard Smartstore web project excludes `fontawesome-pro` from its normal publish output. Include your licensed library through your own deployment process where permitted, and provide the same sources on every application node. Installing the files on a development machine alone does not make them available in production.
