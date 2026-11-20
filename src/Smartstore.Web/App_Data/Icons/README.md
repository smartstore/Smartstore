# Icon libraries and kits

Smartstore resolves icon concepts to SVG artwork, then renders either an inline SVG
or a symbol from a kit sprite. Razor, HTML helpers, browser components and Sass use
the same resolution rules. Source assets in this directory are not public static files.

This guide covers configuration and integration. For reviewed legacy replacements
and migration rules, see [MIGRATION.md](MIGRATION.md). Keep both documents current
when changing their contracts; do not append implementation history.

## Files and common changes

Paths in this table are relative to `App_Data/Icons`.

| File | Responsibility |
| --- | --- |
| `config.json` | Global library/variant defaults, library fallback and RTL policy |
| `collections.json` | Reusable concept lists and concrete source overrides; no sprites |
| `kits.json` | Kit membership, area selection, defaults and picker settings |
| `<library>/library.json` | Library identity and declared variants |
| `<library>/mapping.json` | Concepts mapped to that library's concrete icon names |
| `<library>/metadata.json` | Inventory and supplemental English search tags |
| `<library>/<variant>/icons.zip` | Optional upstream SVG archive |
| `<library>/<variant>/icons/*.svg` | Smartstore corrections and additions |
| `<library>/<variant>/user/*.svg` | Integrator corrections and additions |

For each icon, precedence is **user > icons > icons.zip**. Use the exact filename
without `.svg` as its name. Put loose SVGs directly in their directory and archived
SVGs at the ZIP root. Removing an override reveals the next layer; an invalid
override raises an error instead of silently falling through. ZIP-free variants
are supported. Only variants declared in `library.json` are loaded.

- **Replace artwork:** put the replacement in the variant's `user` directory,
  for example `hugeicons/stroke-rounded/user/shopping-cart-02.svg`.
- **Change a concept:** edit its library mapping or an intentional kit `sources`
  override. Keep migration mappings synchronized and preserve manual JSON edits.
- **Update a library:** replace upstream archives and update its version. Preserve
  `user`, mappings and custom tags; reconcile metadata and check mapped IDs.
- **Deploy:** include the icon sources on every application node. The web project
  publishes this directory recursively; respect exclusions for licensed local assets.
  Distributed caching does not distribute source packages.

Use two-space JSON indentation, CRLF and single-line tag arrays. Metadata enriches
search but does not determine availability:

```json
{
  "icons": {
    "cabinet-01": {},
    "search-01": { "tags": ["find", "lookup", "magnifier"] }
  }
}
```

Maintain an entry per icon. Omit empty tags, redundant name fragments, `label` and
`aliases`. Preserve exact source spellings. Search matches every query word against
names, mapping concepts or tags, case-insensitively, and materializes only the page.

## Libraries and variants

`config.json` selects the default library by **directory name**, not display name
or short name. Its optional `defaultVariant` affects only that library:

```json
{
  "defaultLibrary": "hugeicons",
  "defaultVariant": "stroke-rounded",
  "fallbackToDefaultLibrary": true
}
```

To install a library, create its directory, declare variants in `library.json`,
add SVG sources, then supply mappings and metadata. For example:

```json
{
  "displayName": "HugeIcons",
  "shortName": "hi",
  "defaultVariant": "stroke-rounded",
  "variants": {
    "stroke-rounded": {
      "displayName": "Stroke Rounded",
      "shortName": "sr",
      "defaultViewBox": "0 0 24 24",
      "stroke": "currentColor",
      "supportsStrokeScaling": true,
      "strokeWidthScale": 1.0666667
    }
  }
}
```

Set `version` to the installed vendor version. Directory names and variant keys
define identity; optional display names affect labels only. Variant order controls
picker order. Short names must be unambiguous and cannot shadow another system
name in their scope. Selectors are case-insensitive; icon names and mapping keys
are exact and case-sensitive. Changing short names can break persisted addresses.

Variant settings are defined in [IconLibrary.cs](../../../Smartstore/IconKit/IconLibrary.cs):

| Setting | Behavior |
| --- | --- |
| `fallbacks` | Ordered variant names/short names in the same library |
| `defaultViewBox` | Used only when the source has no `viewBox`; without either, the icon is skipped |
| `stroke` | Replaces source paint for stroke artwork; explicit `none` stays unpainted |
| `strokeWidthScale` | Finite, non-negative source-width multiplier, default 1; applied once, preserving proportions and numeric units |
| `supportsStrokeScaling` | Enables per-icon stroke multipliers; default false; the resolved source variant decides |
| `isDuotone`, `primaryPathSelector`, `secondaryPathSelector` | Enable two separately styled layers; see below |
| `icon` | Picker icon: variant override, then library icon, then `layers` |

Do not rely on `fill` as an implemented paint override. Picker icon addresses use
normal resolution and system defaults; qualify them for stable source selection.
Keep variant settings out of `metadata.json`.

### Replacing a library

A separately installed edition can declare `"replaces": "fontawesome-free"` in
its `library.json`. Existing system and short selectors, including configured
defaults, then resolve to the replacement. Sources, mappings and metadata are **not
merged**: supply a complete library and preserve the installed predecessor's variant
names and short names. Additional variants are allowed.

The predecessor directory is optional; its system name remains an alias. Replacement
chains are supported, but cycles and competing replacements fail configuration.
Removing the replacement restores an installed predecessor on catalog reload.
Do not commit or redistribute licensed artwork; follow its local packaging rules.

### Duotone variants

Layer selectors are XPath expressions relative to the namespace-normalized SVG
root, with one-based positions. They must select non-overlapping drawing elements;
invalid or missing selections fail preparation. For Font Awesome Pro 7+, primary
is `path[2]` and secondary is `path[1]`.

Prepared SVGs expose `--icon-primary-color`, `--icon-secondary-color`,
`--icon-primary-opacity` and `--icon-secondary-opacity`, preserving original paint
and opacity as fallbacks. These variables also work through external sprites.
The resolved variant determines the `icon-duotone` class.

## Addresses, mappings and presentation

Persist an address as `[library:]name[!][/variant]`, for example `cart`, `hi:cart/sr`
or `hi:shopping-cart-02!/sr`. `!` selects concrete artwork: it bypasses conceptual
kit routing, mapping and library fallback, but **not variant fallback**. It does
not force inline output.

`IconAddress` is an immutable value with `Parse`, `TryParse` and implicit string
conversions. Its default value is empty and cannot resolve. Canonical result addresses
identify actual artwork and prefer configured short names; they omit `!`. Never
persist cache revisions or parse diagnostic `data-icon` text as an address.

Library mappings apply once, across all variants:

```json
{
  "cart": "shopping-cart-02",
  "chevron-left": "chevron-right?flip=x",
  "chevron-up": "chevron-right?rotate=-90"
}
```

Targets are unqualified concrete names, not another alias. Mapping modifiers are
limited to flip, rotation and positive `stroke-scale`. Flips precede clockwise
rotation around the viewBox center; decimal degrees are supported. Rendering-only
values such as color, size and animation are not allowed in library mappings.

An `IconExpression` combines an address and immutable `IconPresentation` values:
`cart?size=lg&fw&anim=spin&color=#1459ad`. Parse it with `IconExpression.Parse` and
persist `ToString()`. Resolution consumes its `Address`; rendering consumes the
expression. `IconOptions` supplies context/HTML attributes, not presentation.

Explicit component/helper attributes override expression values, which override
source/mapping defaults field by field. Omitted values inherit; `false`, zero and
one can be intentional resets. Presentation parsing accepts legacy long tokens;
canonical short tokens include `fw`, `anim`, `anim-dur`, `anim-rev` and `o` (opacity).
Boolean values accept bare true flags, `true`/`false` or `1`/`0`. Unknown keys and
unsafe CSS delimiters fail; invalid known values are ignored and the last valid
value wins. See [IconPresentation.cs](../../../Smartstore/IconKit/IconPresentation.cs)
for the full parser and value contract. Encode `&` as `&amp;` in HTML, and encode the
whole expression when passing it as a URL parameter, including `#` in colors.

## Kits, collections and areas

Collections reuse membership without producing sprites or selecting libraries.
Kits include them with `use`, add their own members and optionally pin sources:

```json
{
  "public": {
    "areas": ["public"],
    "use": ["shared"],
    "icons": ["phone"],
    "sources": { "phone": "bi:telephone" }
  },
  "admin": {
    "areas": ["admin"],
    "use": ["shared"],
    "showInPicker": false,
    "icons": ["layers-plus"]
  }
}
```

This is the top level of `kits.json`, with no enclosing `kits` property.
`collections.json` uses named objects with `icons`, `sources` and optional `use`.
The supplied `shared` collection is included independently by public and admin.

- The **first declared kit** containing a concept in the selected area wins.
  Never alphabetically reorder kit definitions. Within `use`, the first collection
  defining the concept wins; nested collections are supported, cycles are not.
- Local `icons` reset inherited source assignments to normal mapping; local
  `sources` override inherited sources. A source key must already be a member.
  Duplicate concepts, qualified member names and unknown references are errors.
- Source values select concrete artwork without remapping. They accept icon
  expressions; source rotation, flip and stroke scaling become drawing defaults.
  A trailing `!` is redundant. Keep call-site presentation on the rendered expression.
- `defaultLibrary` and `defaultVariant` override global defaults for that kit.
  A variant applies only to its effective library; changing libraries never carries
  an unrelated variant across. Explicit caller selectors win over source and kit defaults.
- Source overrides stay pinned when requesting alternate kit defaults; configured
  variant fallbacks still apply. An individual request selecting another library or
  variant uses that selection's mapping of the original concept instead.
- Omitted `areas` means public and admin; `[]` disables automatic area selection
  but permits explicit kit access. `showInPicker: false` hides only the picker entry.
  `displayName` and `icon` change picker presentation, not membership or identity.

An omitted area resolves from the current request (`admin` or `public`), defaulting
to public outside HTTP. Override it with `<icon area="public">`, `IconOptions.Area`
or the service's `area` argument. Browser components inherit the manifest meta
element's `data-area`, unless their own `area` is set. There is no kit attribute.
Sass bundles use `IconArea` (public by default; admin for the main Admin theme).

### Adding icons: Admin versus Public

- **Admin / Hugeicons:** register the existing library name unchanged. Do not invent
  aliases, add identity mappings or add library-switching mappings unless explicitly
  requested. A mapping request applies only to that icon.
- **Admin / other libraries:** add a qualified source mapping, even when the names
  match, for example `braces-asterisk` to `bi:braces-asterisk`. Preserve the selected library.
- **Public:** use library-independent concepts and provide their corresponding
  library mappings. These mappings are part of adding the icon; preserve visual meaning
  when switching libraries.

Admin kit membership is optional for occasional icons; direct addresses can render
inline. Add shared, reused artwork to kits deliberately. The curated `brands` kit
uses Font Awesome brands; keep its naming conventions and reviewed substitutions in
[MIGRATION.md](MIGRATION.md), rather than maintaining a second list here.

## Resolve missing icons

After selecting the area, kit and effective library/variant:

1. Apply the library mapping once, or use the original name when unmapped.
2. Search that name in the requested variant, then its ordered fallbacks depth-first.
   Visit each variant once; mutual fallbacks are safe. There is no implicit fallback
   to the library's default variant. A missing mapped target does not retry the concept locally.
3. If enabled and the initial library differs from the system default,
   `fallbackToDefaultLibrary` retries the **original concept** in the system default,
   using its mapping and effective variant. The first library's modifiers and kit
   defaults do not carry across. The option defaults to false and is enabled in the
   supplied configuration.
4. Return null if unavailable. Unknown requested selectors return null; invalid
   syntax/configuration and broken SVGs raise errors rather than invoking fallback.

Direct `!` addresses and concrete kit sources keep their library fixed. Native
library browsing/counts/sprites never include fallback inventory; kits show resolved
concepts and may mix libraries and variants. Resolved identities and CSS classes
always describe the actual source, while kit context remains separate.

Rendering a missing icon uses `system:not-found!/outline`, with `icon-not-found`
and danger color. Lookup/search still report absence. If the placeholder is also
unavailable, rendering returns no output. `data-icon` retains the requested identity.

## Render icons

```cshtml
<icon name="cart" size="lg" aria-label="Cart" />
<icon name="shopping-cart-02!" lib="hi" variant="sr" />
<icon expression="Model.Icon" rotate="0" />
@await Html.IconAsync("cart?size=lg")
```

Do not combine `expression` with `name`, `lib` or `variant`. The Razor tag is replaced
by SVG; `Html.IconAsync` uses the same renderer and accepts optional `IconOptions`.
The old synchronous `Html.Icon()` and `Html.BootstrapIcon()` helpers are removed.

For dynamic HTML, use explicit closing tags:

```html
<sm-icon name="cart" size="lg" aria-label="Cart"></sm-icon>
<sm-icon-stack size="2x" aria-label="Confirmed">
    <sm-icon name="circle-outline" class="icon-stack-2x"></sm-icon>
    <sm-icon name="check" class="icon-stack-1x"></sm-icon>
</sm-icon-stack>
```

Razor provides the equivalent `icon-stack` with direct `icon` children. Stack layers
keep separate viewBoxes and paint in DOM order; other children/nested stacks are
invalid. Label the host for a combined symbol.

All renderers emit `.icon`, `icon-[library]` and `icon-[library]-[variant]`, using short
selectors where available. Kits normally render through external `use`; non-kit
icons render inline. Overriding baked mapping transforms/stroke widths can force
server-side inline rendering. `force-inline="true"` / `IconOptions.ForceInline`
always embeds artwork, including during installation when sprite routes may redirect.

Icons are decorative by default. Label icon-only buttons on the button; use
`aria-label`/`aria-labelledby` for meaningful standalone icons. HTML attributes pass
through; explicit `style` declarations follow generated styles and take precedence.

For C#, inject [IIconService](../../../Smartstore/IconKit/IIconService.cs) for lookup,
search and prepared SVGs, and [IIconRenderer](../../../Smartstore/IconKit/IIconRenderer.cs)
for rendering. `GetSvgAsync(IconInfo)` consumes an already resolved icon without
remapping. Payloads are immutable and presentation-free. Custom renderers must encode
root attributes and namespace SVG IDs/references per repeated instance.

### CSS and presentation

Import `shared/_utils.scss` before [shared/_icons.scss](../../wwwroot/shared/_icons.scss)
in custom themes; Admin and Flex already do this. Put `.icon` on the SVG **or** its
host with one direct SVG child, not both. Paint uses source attributes/`currentColor`;
CSS does not universally force monochrome artwork. External sprite paths cannot be
styled with descendant selectors: use variables on the outer icon.

| Purpose | Attributes / classes |
| --- | --- |
| Layout size | `size`: `2xs`, `xs`, `sm`, `lg`, `xl`, `2xl`, `1x`–`10x`; corresponding `icon-*` classes; `font-scale` overrides preset size |
| Fixed width | `fw` / `icon-fw` (1.25em) |
| Paint | `color`, `opacity` (0–1), `inverse` / `icon-inverse` |
| Transform | `rotate`, `flip-h`, `flip-v`, `scale`, `shift-x`, `shift-y`; shifts are sixteenths of an em |
| Stroke | Positive `stroke-scale`, only for supported source variants |
| Animation | `animation`, `animation-duration`, `animation-reverse`; `icon-spin`, `icon-pulse`, `icon-beat`, `icon-fade`, `icon-throb`, `icon-bounce`, `icon-flip`, `icon-shake`, `icon-spin-snap`, `icon-swing`, `icon-wag` |
| Lists / stacks | `icon-ul`, separate `icon-li` marker; `icon-stack`, `icon-stack-sm`, `icon-stack-1x`, `icon-stack-2x` |

The base is a square following font size, with a 16px minimum **before** size scaling.
Set `--icon-min-size: 0px` to disable it. `font-scale` changes layout size; `scale`
changes only the drawing. The stack defaults to 2em square; `icon-stack-sm` makes it
1em. `position="end-end"` generates `icon-pos-end-end`: block axis first, inline axis
second, each `start`, `center` or `end`. Omission centers; unknown strings have no rule.

Use `--icon-size`, `--icon-color`, `--icon-rotate`, `--icon-scale`,
`--icon-shift-x/y` and `--icon-flip-x/y` for CSS composition. Lists and stacks expose
their dimensions, alignment and layer order; animation variables control timing and
effect amplitudes. The stylesheet is the complete variable reference. One animation
per host composes with static transforms; reduced-motion preferences disable animation
and transitions while retaining positioning. `icon-pulse` is stepped rotation.

`--icon-stroke` changes stroke paint and `--icon-stroke-width` is an absolute width,
not another multiplier. Both preserve source fallbacks and explicit `none` strokes.
Unsupported variants ignore per-icon stroke multipliers. Duotone uses the layer
variables described above.

### Direction-sensitive concepts

`config.json`'s exact, case-sensitive `mirrorInRtl` list controls automatic mirroring
of **unqualified concepts**. Explicit library/variant requests and `!` are excluded.
Choose direction-sensitive concepts deliberately; logos/charts need not follow text.

`mirror-rtl` (or `IconPresentation.MirrorRtl`) overrides the policy: omitted inherits,
true enables it even for explicit addresses, false suppresses it. CSS evaluates local
`:dir(rtl)` and composes mirroring with ordinary flips and animation. Artwork and
sprite revisions stay direction-independent; policy changes revise the manifest.

### Browser integration

Custom layouts must include the shared jQuery bundle (which loads `smartstore.icons.js`)
and `sm:root` / `sm:icons` meta elements with the manifest URL and `data-area`.
Relative URLs preserve PathBase. Prefer unqualified kit concepts: manifest hits use
the kit sprite directly. Unknown names, explicit selectors, `!` and unavailable/old
manifests fall back to `/icons/render`.

Presentation updates stay local without replacing the drawing. Requests are shared
and bounded; stale responses cannot replace newer selections. Components emit bubbling
`icon-load` and `icon-error` (`event.detail.error`). SVGs are normal children, not
Shadow DOM. Components own generated classes/styles and accessibility. Persist
`element.getSourceNode()`, an inert copy of authored content, instead of live
`outerHTML`. Stacks include their authored layers. In Vue, call
`app.use(Smartstore.Icons)` before mounting; DataGrid already does so.

## IconBrowser and editor integrations

Configure the admin editor on GET and again after failed POSTs:

```csharp
model.Icon = new IconBrowserModel
{
    Icon = IconExpression.Parse("cart?rotate=90"),
    VisibleOptions = IconOptionFields.Size | IconOptionFields.Rotate
};
```

```cshtml
@Html.EditorFor(x => x.Icon, "IconBrowser")
```

The types live in `Smartstore.Web.Models.Media`. Persist `model.Icon.Icon?.ToString()`.
The nullable `Icon` holds the complete expression; empty input binds to null.
ModelState attempted values win on failed POSTs: restore editor configuration without
overwriting the posted selection. `VisibleOptions` defaults to None; it and `Area`,
`ShowHiddenKits`, `ShowProviderIndicators` are trusted, non-bound configuration.
Hidden presentation options survive edits. Selecting a different icon or clearing
resets options; changing only the browsing source retains the selection.

Kit selections return concepts; library selections return qualified direct addresses
such as `hi:shopping-cart-02!/sr`. Previews do not persist preview size. The source
dropdown excludes the system library; explicit unknown saved selectors are retained.
Provider indicators identify membership that depends on an installed provider, not
the library supplying the drawing. A local JSON membership removes this dependency;
a source override alone does not.

`IIconBrowser` adapts kit/library searches for the picker; the corresponding services
own resolution and sprite generation. `/icons/browser/search` returns 50-item pages
with `no-store`. Selecting a library prepares its **complete variant sprite** on
demand; kits use normal kit sprites. Opening a picker can therefore prepare more
artwork than the visible page. Preview generation does not populate the individual
SVG cache. Search and selection previews retain the requested identity despite fallbacks.

Summernote's Smartstore plugin inserts/edits `sm-icon` using public-area kits and
libraries. Its serializer uses `getSourceNode()` for save, source view and undo;
CodeMirror completion likewise defaults to public unless the tag specifies an area.
Keep editor icon overrides in `wwwroot/lib/editors/summernote/globalinit.js` and
styles in `wwwroot/skinning/_summernote.scss`, not the vendor fork. The admin-only
`htmledit` kit holds editor-specific concepts. Shared color/animation tools live in
`smartstore.icontools.js`; hosts own persistence and dispose their bindings.

Modules may supply an `Icon` expression in `module.json`; `<module-icon>` renders
`ModuleManager.GetIcon()` results. Provider images win; without a manifest expression,
`icon.svg`, `icon.png`, then `hi:plug-02` are tried. Image files keep their own colors.
Select2 accepts `data-icon` on a select as a default, overridden by option `data-icon`
or AJAX `icon`; empty suppresses it. Images/swatches win; placeholders, headings,
loading and add-entity actions receive no default icon.

## Module contributions

Implement [IIconProvider](../../../Smartstore/IconKit/IIconProvider.cs) in an installed
module; discovery creates it in a temporary scope once per catalog generation:

```csharp
internal sealed class Icons : IIconProvider
{
    public void Configure(IconKitBuilder builder)
    {
        builder.Kit("admin").Add("myplugin-action", "bi:braces-asterisk");
        builder.Collection("shared").Add("myplugin-shared", "hi:plug-02");
    }
}
```

`Kit(name)` extends an existing kit (unknown names fail); `Collection(name)` creates
or extends a collection. `AddKit` declares a standalone kit. A supplied source is
concrete, not a global mapping; omission uses ordinary mapping. Providers cannot add
public libraries or variants. Use `AddSvg(concept, svg)` or
`AddSvgFile(concept, "~/Modules/MyCompany.MyPlugin/wwwroot/icons/logo.svg")` for private
artwork. Files must be beneath the application's content root, deployed with the
module; URLs, physical paths and traversal are rejected. Include a `viewBox`.
Private drawings have no variant defaults or per-icon stroke scaling.

Providers run deterministically in ordinal full-type-name order. Identical additions
are idempotent; conflicting sources/kit definitions fail. JSON kit settings and local
membership/source declarations take precedence; provider members are additive and
provider-only kits follow JSON kits. Inherited collection entries outrank additions
to the including set. See [IconKitBuilder](../../../Smartstore/IconKit/IconKitBuilder.cs)
and [IconSetBuilder](../../../Smartstore/IconKit/IconSetBuilder.cs) for the API contracts.

Do not use request/scoped dependencies, resolve `IIconService` inside `Configure`,
or retain the builder. Providers rerun on catalog invalidation; code/module changes
require restart. Private SVG files are read lazily and **not watched**: restart or
rebuild the catalog after changing them. Provider discovery is skipped before installation.

## SVG preparation, caching and hosting

Sources may contain static geometry, groups, local references, gradients, clips and
masks. Scripts, handlers, external references, embedded HTML and authored style
declarations are rejected. Use SVG presentation attributes. Limits are 1 MiB per
SVG and 256 MiB per archive before/after decompression. Preparation removes root
dimensions/classes, preserves viewBox/geometry and scopes IDs during rendering or
sprite generation. Stroke normalization exposes CSS fallbacks without mutating files.

Catalog startup reads configuration/manifests; mappings, metadata, name indexes and
artwork load on demand. Do not introduce eager archive scans or SVG preparation in
constructors. Kit membership uses name indexes, not drawing reads. Source changes
invalidate catalog state; a source changing during preparation fails rather than
publishing under an outdated revision. Retry after watcher invalidation.

| Cache | Contents / behavior |
| --- | --- |
| `IIconCache` | Immutable SVG payloads under canonical address + revision; uses configured cache manager/Redis, bounded lifetime |
| `App_Data/.cache/icons/kits` | Kit sprites and revisioned browser manifests |
| `App_Data/.cache/icons/browser` | Complete variant sprites for library browsing |

Sprites/manifests are global, tenant-independent files, published atomically and
served with revision URLs, ETags and GET/HEAD support. Concurrent requests share
generation; each participating archive is processed sequentially, without retaining
all artwork or populating the individual SVG cache. Current files regenerate on
demand. Historical URLs never serve current bytes; unavailable old revisions return
404. Open pages retain their manifest until reload.

[IIconCacheLifecycle](../../../Smartstore/IconKit/Cache/IIconCacheLifecycle.cs) watches
sources and tracks generated files in the local cache index. Obsolete files have a
10-minute grace period before activity-triggered cleanup; this is not permanent
historical retention. `IIconCache.InvalidateLibraryAsync` removes payload entries
only, not generated file artifacts.

DevTools uses `IIconService.GetCacheSummaryAsync()` to show existing current kit
sprites and cached individual icons, excluding kit members/library logos, without
generating artwork. `IIconKitService.GetCachedSpriteFiles()` and
`IIconCache.GetEntriesAsync()` expose lower-level inventories. Entries can expire;
cache contents are not a usage history and do not identify the requesting plugin.

Smartstore's `IconKitStarter` registers and maps endpoints automatically. Other
hosts use `services.AddIconKit(options => ...)` and `app.MapIconKit()` once. Options
are `SourcePath` (default `App_Data/Icons`), `CachePath` (`App_Data/.cache/icons`) and
`RoutePrefix` (`/icons`); relative paths resolve against the host content root.
A later `AddIconKit` can override options without duplicate service registration.
Contracts and hosting code live in [Smartstore/IconKit](../../../Smartstore/IconKit).

### Sass masks

```scss
.toggle::after {
    @include icon-mask("chevron-down", .75rem);
}
```

`icon-url($expression)` embeds a prepared SVG data URL through normal resolution,
including mapping transforms/stroke scaling. Use an explicit color for background
images; masks use host `currentColor` and preserve source alpha. They cannot inherit
page variables inside the SVG or expose independent duotone colors. Apply RTL
mirroring in the consuming stylesheet. Missing artwork uses the system placeholder,
then transparency if that too is absent. **Regenerate CSS bundles after icon changes**:
source watching does not invalidate embedded SVGs in compiled CSS automatically.

## Migration reminders

- Use `Html.IconAsync` and SVG helpers/components for new integrations; legacy font
  glyph classes are not converted by adding `.icon`. Follow [MIGRATION.md](MIGRATION.md)
  for replacements and provider-specific exceptions.
- System artwork now uses `outline` and `fill`, with mutual fallbacks. Replace explicit
  `sys:.../default` addresses; existing concepts remain stable (`circle-outline` and
  `circle-fill` select the respective `circle` artwork).
- Persist expressions and authored component markup, not resolved/cache identities
  or generated SVG HTML. Keep configuration, source packages and customizations
  synchronized across nodes and preserve manual mappings during imports.
