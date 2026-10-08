# Customizing icon libraries

Smartstore's icon configuration separates application concepts, such as `cart`
or `search`, from the SVG files used to display them. You can replace individual
icons, select another style, or provide your own library while keeping the same
conceptual names in your integration.

All paths below are relative to `App_Data/Icons`. These source assets live outside
`wwwroot` and are not normal publicly served static files.

> Integration status: local discovery, icon resolution, search and SVG caching
> are available through `IIconService`. The existing FontAwesome explorer remains
> available alongside it. A minimal `icon` TagHelper renders inline SVG. Bundling,
> the HtmlHelper and browser component will follow separately.

<!-- Update this guide and its examples whenever the concept, configuration format,
     or resolution rules change. Keep variant-specific documentation consistent. -->

## File structure

```text
Icons/
    config.json
    hugeicons/
        library.json
        mapping.json
        metadata.json
        stroke-rounded/
            icons.zip
            icons/
                shopping-cart-02.svg
            user/
                shopping-cart-02.svg
```

The example shows a system replacement in `icons` and a user customization
with the same name. Resolution uses **user > icons > icons.zip**, per icon.

| File | Purpose |
| --- | --- |
| `config.json` | Application-wide defaults and kits |
| `<library>/library.json` | Library identity and variant settings |
| `<library>/mapping.json` | Conceptual names mapped to concrete icon IDs |
| `<library>/metadata.json` | Icon inventory and additional English search terms |
| `<library>/<variant>/icons.zip` | Optional archive of original SVG files for one variant |
| `<library>/<variant>/icons/*.svg` | Smartstore-supplied replacements or additional icons |
| `<library>/<variant>/user/*.svg` | Integrator replacements or additional icons; highest priority |

## Select a library and variant

Configure the defaults in `config.json`:

```json
{
  "defaultLibrary": "hugeicons",
  "defaultVariant": "stroke-rounded",
  "kits": {
    "shared": { "icons": ["cart", "heart", "search"] },
    "frontend": { "icons": ["handshake"] },
    "backend": {
      "defaultLibrary": "hugeicons",
      "defaultVariant": "stroke-rounded",
      "icons": ["barcode"]
    }
  }
}
```

`defaultLibrary` identifies a library by its **system name: its directory name**.
Here, `hugeicons` refers to the `hugeicons/` directory. Use the directory name,
not the display name or short name. The system name is derived from the
folder; there is no separate `systemName` field to maintain.

`defaultVariant` is an optional override for that default library. If omitted,
the library's own `defaultVariant` applies. The global override does not apply
to other libraries. An unavailable configured variant is a configuration error;
it does not silently select a different style.

## Configure a library

Each library contains a `library.json`:

```json
{
  "displayName": "HugeIcons",
  "version": "4.3.5",
  "shortName": "hi",
  "defaultVariant": "stroke-rounded",
  "variants": {
    "stroke-rounded": {
      "shortName": "sr",
      "gridSize": 24,
      "stroke": "currentColor",
      "strokeWidthScale": 1.0666667
    }
  }
}
```

- `displayName` is the human-readable library name.
- `version` identifies the installed library version.
- `shortName` is optional, for example `hi`, `bi`, or `fa`. It must be unique
  across libraries and must not shadow another library's system name.
- `defaultVariant` selects the library's default style, unless overridden by
  the global configuration for the default library.
- `variants` holds technical settings keyed by variant directory name.
- Each variant may also have an optional `shortName`, such as `sr`. It must be
  unique within that library and must not shadow another variant's name.
- `gridSize` describes the nominal design grid. The SVG's actual `viewBox`
  remains authoritative when rendering.
- `fill` is reserved for future paint configuration.
  Stroke width is measured in SVG coordinates; caps accept `butt`, `round` or `square`.
  These settings are currently not applied.
- `stroke` optionally replaces explicit source stroke colors (for example, `currentColor`).
  Omission preserves original colors. Explicit `none` remains unpainted.
- `strokeWidthScale` multiplies original stroke widths once, including inherited widths.
  It defaults to 1 and must be finite and non-negative. For example, 1.0666667 turns
  1.5 into approximately 1.6 while preserving relative differences between widths.
  Numeric SVG lengths retain their units; unsupported expressions fail explicitly when scaling.

`icons.zip` is optional. A variant can consist entirely of loose SVGs in `icons`
or `user`; a variant without any sources is empty. A missing archive does not cause
an error, and unavailable icons return null. An existing invalid archive still
raises an error when accessed.
Only declared variants are loaded. Configuration describes variants; their
archives and loose SVG files establish the available icons.
Keep variant settings in `library.json`, not in `metadata.json`.

## Map application concepts to icons

Edit `mapping.json` to choose which icon represents each concept:

```json
{
  "cart": "shopping-cart-02",
  "search": "search-01"
}
```

An icon ID is the exact SVG filename without `.svg`. For example,
`shopping-cart-02` refers to `shopping-cart-02.svg`.

When switching libraries, keep the conceptual keys and change their values to
IDs provided by the new library. Mappings are shared across all variants of a
library. Check that mapped IDs exist in each variant you intend to use. Variant
selection is independent of mapping; there is no additional per-icon alias layer.

Mapping values may append `flip` and `rotate` modifiers:

```json
{
  "chevron-left": "chevron-right?flip=x",
  "chevron-up": "chevron-right?rotate=-90",
  "example": "some-icon?flip=xy&rotate=22.5"
}
```

`flip` accepts `x`, `y`, or `xy`. `rotate` accepts finite decimal degrees using a
period as the decimal separator; positive angles rotate clockwise. Transformations
always flip first, then rotate around the source viewBox center, regardless of
parameter order. Unknown, repeated or malformed parameters are configuration errors.
These modifiers belong to mapping values, not the public icon-address syntax.

The renderer applies transformations inside the icon, independently of outer CSS
sizing, animation and transforms. Kit symbols apply the same transformation once,
sharing original artwork between concepts. Source SVG cache entries remain unchanged.
A direct `!` address skips mapping and therefore its modifiers. Kit selection matches
both the source icon and its transformation; a direct icon cannot select a transformed
symbol accidentally. Modifier changes also change the affected kit revision.

An empty mapping file (`{}`) contains no assignments. Populate mappings for all
concepts used by your kits before relying on icon resolution.

## Use the .NET API

Inject `IIconService` from `Smartstore.Core.Content.Media.Icons`. Its implementation
currently lives under `Media/Icons/Svg`, using the parent namespace, so it can run
alongside the existing FontAwesome `IIconExplorer`.

```csharp
IconLibrary library = icons.DefaultLibrary;
IconLibrary hugeicons = icons.GetLibrary("hi"); // Also accepts "hugeicons".
IconInfo icon = await icons.GetIconAsync("cart");
IconSvg svg = await icons.GetSvgAsync("hi:cart@stroke-rounded");
IconSearchResult results = await icons.SearchAsync(new IconSearchQuery
{
    Term = "basket",
    Library = "hugeicons",
    Take = 50
});
```

`mapping.json` must contain the example `cart` assignment for those calls to find
the cart icon. Each address lookup applies exactly one mapping first, then finds the
actual icon. Without a mapping, the supplied name is used directly. A mapping
whose target is missing returns null; it does not fall back to the conceptual name.
Unknown requested libraries, variants and icons also return null. Invalid address
syntax and invalid configuration raise exceptions.

`IIconService.GetSvgAsync(IconInfo)` consumes an already resolved icon from
`GetIconAsync` or search, without mapping its name again. The string overload is
an extension method that resolves the address and forwards the resulting `IconInfo`.
Preparation uses the current library, variant and actual name; it derives the
canonical address again rather than trusting a caller-modified `Address` property.

Persist strings as `[library:]name[!][@variant]`: `cart`, `hi:cart`,
`cart@stroke-rounded` or `hi:cart@stroke-rounded`. `IconAddress.Parse` parses this
syntax without accessing configuration. Library and variant selectors accept
their full names or optional short names, case-insensitively. Icon names and
mapping keys are exact and case-sensitive; even spaces in source names are retained.
Names of libraries, variants and their short names use letters, digits, `-` and `_`.

Append `!` to the name to skip mapping: `cart!` or `hi:cart!@sr` addresses the
actual icon directly. A missing direct icon returns null without a mapping fallback.
`IconAddress.SkipMapping` retains this flag, and `ToString()` preserves it for
persistence. Component-based construction uses `new IconAddress("cart", "hi", "sr",
skipMapping: true)`. The marker is reserved syntax and cannot occur within an icon
name. Resolved addresses, `data-icon` and cache keys omit the marker because they
already identify the actual icon.

`IconAddress` is an immutable `readonly record struct` with value equality and
implicit conversions in both directions: `IconAddress address = "hi:cart";`
and `string persisted = address;`. String conversion into an address parses the
syntax and throws `FormatException` for invalid input; use `TryParse` for unchecked
input. `default(IconAddress)` is empty (`IsEmpty`) and cannot resolve an icon.

The service fills missing selectors from the configured defaults. Optional
`library` and `variant` method arguments can fill missing selectors too. Explicit
conflicting selectors are rejected; a full name and its equivalent short name
do not conflict. Canonical result addresses contain the actual mapped icon name
and both selectors, preferring each short name when present. For example, with
the manifest above: `hi:shopping-cart-02@sr`. Without short names:
`hugeicons:shopping-cart-02@stroke-rounded`. Changing short names requires updating
persisted strings that use them; persist full names when that stability is needed.

`IconLibrary.SystemName` and `IconVariant.Name` come from directory names and
manifest keys. Library and variant manifests are immutable and shared; their variants dictionary
is frozen. SVG payloads are also immutable, with frozen root attributes, and are shared without cache-boundary copies. Search
matches every query word against icon names, supplemental tags or conceptual
mapping names, and returns a page plus the total count.

`IconSvg` is an immutable serializable payload: canonical `Address`, source `Revision`,
`Library`, `Variant`, `Name`, the original `ViewBox`, `RootAttributes` and child
`Content`. Root width and height attributes are removed; CSS controls the rendered size. It contains no
HTML helper, XML DOM or presentation state such as size, transforms or animation.
Consumers must encode root attribute values when rendering them. Source IDs and
local references are preserved; a renderer that repeats icons with IDs must
scope those IDs per rendered instance.

SVG sources support static geometry, groups, local references, gradients, clips
and masks. Scripts, event handlers, external references, embedded HTML and source
style declarations are rejected. Each SVG is limited to 1 MiB, and each archive
to 256 MiB both before and after decompression. Use presentation attributes in
custom SVGs. The renderer removes root width/height attributes and moves root stroke attributes to children, preserving inheritance and leaving the root free of stroke attributes. The optional stroke setting supplies the stroke color fallback. Stroke widths are multiplied by the variant strokeWidthScale; inherited values are not multiplied again. Changes to library.json invalidate the SVG cache revision. Geometry, descendant line caps and all source fill attributes remain unchanged. Generated inline styles expose `--icon-stroke` and `--icon-stroke-width`. Corresponding presentation attributes are removed only after their fallback is stored in the generated style; `none` and inherited declarations that are not converted remain intact. A CSS variable overrides the configured/source fallback; `--icon-stroke-width` is an absolute width, not an additional multiplier. Explicit `none` strokes remain untouched. For example, `<icon name="hi:search-01" style="--icon-stroke: red; --icon-stroke-width: 2" />` overrides both for one render without changing the cached payload.

The first catalog access reads only root configuration and library manifests. All
manifests are needed to resolve short names and reject ambiguous selectors. Mappings
load on the first lookup in a library; metadata loads only when `IconInfo.Tags` is accessed or during
search, never for SVG rendering. Each variant's ZIP central directory loads on
its first archive lookup or search. Unused libraries and variants incur no archive
or metadata reads. Invalid deferred files are reported when first used.

User files are resolved directly by name, then system icons, then the archive.
Only the winning layer is read and hashed. Search
only enumerates their filenames. Only requested sources retain a small descriptor
(path and fingerprint); no complete override index, file objects, SVG contents or
open handles are retained. Loose-file revisions include the layer and a SHA-256 fingerprint; archived icons use
the ZIP entry CRC. Archive bodies are not scanned for hashing. Each miss reopens
and checks the requested source, then streams it into the XML parser. Warm cache
hits need no file access. Local providers must supply seekable file streams.

File changes under `Icons` invalidate the generation when the provider reports the
change. Deferred indexes and requested-source descriptors are then rebuilt on demand.
Source replacement detected during preparation fails the lookup rather than caching
under an outdated revision; retry after the watcher invalidates the generation.
`IIconCache` uses Smartstore's configured cache manager, including Redis when
enabled. Its key is the canonical address plus an internal content revision,
such as `hi:shopping-cart-02@sr:<revision>`. The revision covers the library
manifest, the requested source fingerprint and SVG preparation version. It is never
part of persisted icon addresses. Unused revisions expire after seven days.
`InvalidateLibraryAsync` accepts the canonical library selector (short name if
configured, otherwise system name). Every application node needs the same source
files; a distributed cache does not distribute library packages.

## Render an icon in Razor

The shared `IconTagHelper` renders an inline SVG with the `icon`, `icon-[library]`
and `icon-[library]-[variant]` CSS classes. Each selector uses its short name when configured,
otherwise its library system name or variant name. For HugeIcons stroke-rounded,
the configured short names produce `icon icon-hi icon-hi-sr`; without short names,
the classes are `icon icon-hugeicons icon-hugeicons-stroke-rounded`. The helper uses
`IconInfo.LibraryKey` and `IconInfo.VariantKey`, computed from the short
names with `LibraryName` and `VariantName` as fallbacks, without parsing the address again:

```cshtml
<icon name="hi:search-01" />
<icon name="shopping-cart-02" library="hugeicons" variant="stroke-rounded"
      class="icon-2x" aria-label="Cart" />
```

`name` accepts the same addresses and conceptual mappings as `IIconService`.
`library` and `variant` fill missing address selectors. An unavailable icon emits
no markup. Ordinary HTML attributes pass through to the SVG. The data-icon`r
attribute exposes the resolved canonical address. Size, animation and transforms use
existing CSS utilities and variables. Icons are decorative by default; supply
`aria-label` or `aria-labelledby` for an accessible standalone icon.

The Razor tag is replaced on the server, so the browser receives no `icon` host.
A future native custom element needs a hyphenated name, such as `sm-icon`, because
`icon` is not a valid name for registration with `customElements.define`.

## Planned admin icon picker

This design is agreed for later implementation; the current picker has not yet
been migrated. It should support all registered libraries and variants through
library/variant selection and paginated Select2 AJAX search and scrolling.

- Retain only names and search data in the demand-loaded variant index, not SVG
  contents. Filter and paginate before creating result objects; the current
  `SearchAsync` still creates objects for all matches before pagination.
- Return a small page (for example, 40 icons) with prepared SVG previews and a
  `hasMore` indicator. Use the same validation and preparation rules as normal
  rendering, but do not populate `IIconCache` for picker previews.
- Open a variant ZIP once per preview batch and process one SVG at a time.
  Preserve `user > icons > icons.zip` resolution, including ZIP-free libraries.
- Return previews in the page response rather than issuing one request per icon.
  Debounce search and cancel or discard obsolete requests when selections change.
  Bound retained browser results; infinite scrolling must not accumulate the
  entire library as inline SVGs indefinitely.
- Start without a preview cache. If measurements justify one, add a separate,
  size-bounded, short-lived page cache keyed by the relevant source revision and
  query. Do not create an unbounded cache of search combinations or eagerly
  generate a complete library sprite.
- Persist concrete picker selections with the mapping bypass, for example
  `hi:cart-01!@sr`, so conceptual mappings cannot redirect the selected icon.

The initial approach is paginated search with batched, uncached previews. Only
requested icons are prepared; opening the picker must not process all icons.

## Define kits

The `kits` object in `config.json` groups conceptual names by area of use,
such as `shared`, `frontend`, `backend`, or a custom `media` kit. Add names to the relevant
`icons` arrays and provide their assignments in each library's `mapping.json`.
Kit objects may specify `defaultLibrary` (a library system name) and `defaultVariant`.
Both are optional. Array-only definitions remain supported as shorthand.

For conceptual lookups, the preferred kit is selected before library mapping:
`shared` first, then kit names in ordinal order. Explicit address components or
method arguments take precedence over kit defaults, followed by global defaults.
A kit's variant applies only to its effective default library; selecting another
library never carries that variant across. A kit that changes the library without
specifying a variant uses that library's default (or the global variant override
when it selects the global default library).

Direct `!` addresses skip conceptual kit defaults as well as mapping. Search also
uses its explicitly selected library or the system default, independently of kits.
`IIconKitService.GetUrl` follows the named kit's defaults unless overridden.
Changes to kit defaults are picked up by the configuration watcher.

The supplied kit memberships and HugeIcons mappings are a work in progress.
Edit `config.json` to move concepts between kits and `hugeicons/mapping.json` to
change their icons. Keep both files alphabetically ordered within each object or
kit array. The initial assignments were imported from the cheatsheet migration
review; subsequent edits to these JSON files must be preserved when importing
further review changes.

`shared` contains concepts used by both areas. Combine it with `frontend` or
`backend`; shared concepts do not need to be repeated in those kits.

`IconRenderer` automatically selects an external symbol for resolved icons that
belong to a kit. Callers still provide only an icon name; the TagHelper delegates
rendering to the Core renderer. Icons outside kits retain inline SVG output.
Membership uses the actual mapped icon identity. When several kits contain the
same icon, `shared` wins, followed by ordinal kit-name order.

Sprites are served by `/icons/{kit}-{revision}.svg`.
The revision covers kit membership, mapping targets, source fingerprints and SVG
preparation rules. Request path bases are preserved. The browser caches each
revision independently. The endpoint returns a physical file; ASP.NET Core handles
ETag revalidation and HEAD without transferring a response body. A first request,
including HEAD, generates a missing current revision before serving it.

Generated sprites are global, tenant-independent files in `App_Data/.cache/IconKits`, with flat
names such as `shared-<revision>.svg`. Library and variant selectors are not exposed
in URLs or filenames; the revision includes library, variant and kit identity. It uses the
first 96 bits of SHA-256, represented as 24 lowercase hexadecimal characters. Existing files
are served without loading their contents into the multilevel or Redis cache.

A cache miss opens the ZIP once for the kit and processes SVGs individually; no
archive handle or drawing collection survives generation. It streams symbols into
a temporary file beside the destination and
publishes the complete file atomically. Failed writes remove the temporary file.
Concurrent requests share generation; multiple processes may safely publish the
same revision. Generation does not populate the individual SVG cache. Source IDs and their
references are namespaced per drawing; concepts sharing artwork reuse a symbol.
Configuration and source changes invalidate the catalog and produce new URLs.
The source watcher explicitly excludes generated cache files. Historical files
remain available for existing pages, even after their source definitions are removed; unavailable historical revisions return 404, never current bytes.
There is no automatic cleanup yet. The directory can be inspected or cleared;
URL resolution registers the generation plan internally. After a restart, existing
files are addressed directly without a directory scan. For a missing file whose
plan is not registered, current configurations are inspected once per kit and
catalog generation to recover revision lookups. Incomplete variants are skipped.
Current revisions regenerate on demand, but deleted historical revisions cannot
be reconstructed from changed sources.
Missing mapped sources are reported with their kit and concept names.

Use classes and CSS variables on the outer `.icon` element for presentation.
Descendant selectors cannot style paths inside an external sprite. Presentation
attributes belonging to source SVG roots remain inside the sprite.

Kits remain the same when changing libraries. Their conceptual names resolve
through the selected library's mapping. Overlap is allowed; resolved icons are
deduplicated when kits are combined.

## Replace or add an SVG

To replace the HugeIcons cart icon, place your SVG at:

```text
hugeicons/stroke-rounded/user/shopping-cart-02.svg
```

A matching filename in `user` replaces both a system override and the archived
icon for this variant. Smartstore supplies corrections and additions in `icons`.
Both directories can add new icons. Search lists each name only once across all
three layers. Place SVGs directly in these directories, without subdirectories.
Non-SVG files, including README files, are not icon assets.

Keep customizations in `user`; `icons` and `icons.zip` are maintained by
Smartstore and may be replaced during updates. Removing a user file reveals the
system override, or the archived icon if no system override exists. An invalid
higher-priority SVG raises an error rather than silently using a lower layer.
All loose SVGs are variant-specific;
provide a separate SVG for each style you want to customize.

## Add search terms

`metadata.json` is shared across a library's variants:

```json
{
  "icons": {
    "cabinet-01": {},
    "cactus": {},
    "search-01": {
      "tags": ["find", "lookup", "magnifier"]
    }
  }
}
```

Keep an entry for every icon, including custom additions. Use an empty object
when no extra terms are needed. Names are already searchable, so tags add terms
that the name does not cover. For example, `cabinet` is redundant for `cabinet-01`.

Omit `tags` when empty. Do not add `label` or `aliases` fields. Search terms are
English and are not localized at this layer. Preserve exact icon IDs, including
unusual spelling in source filenames.

Metadata enriches search; it does not determine availability. Icons missing from
metadata can still be discovered from archives or loose SVG files and found by name.
You can generate metadata locally and maintain additional terms manually. This
workflow does not require a HugeIcons API connection.

## Install another library

1. Create a library directory. Its name becomes the library's system name.
2. Add `library.json` with a default variant, variant settings and optional short names.
3. Optionally package each variant's SVGs at the root of its `icons.zip`, inside
   the matching variant directory. For small custom libraries, omit the archive
   and place Smartstore-supplied SVGs directly in the variant's `icons` folder.
4. Populate `mapping.json` for your concepts and `metadata.json` for the icon
   inventory and additional search terms.
5. Place custom SVGs in the relevant variant's `user` directory.
6. To select this library by default, set `config.json`'s `defaultLibrary` to its
   directory name. Update or remove the global `defaultVariant` override to match.

## Update and deploy customizations

For a library update, replace the upstream archives and update the version in
`library.json`. Preserve `user`, mappings, and custom search terms. Reconcile
metadata with the updated inventory and check that mapped IDs still exist.

Include the entire `App_Data/Icons` directory in your deployment. Smartstore's
web project includes it recursively in publish output.

Use two spaces per JSON indentation level and CRLF line endings, as specified in
`.editorconfig`. Keep tag arrays
on one line.

## Shared icon CSS

`wwwroot/shared/_icons.scss` provides library-independent SVG layout and utility
classes. It is included in the Admin and Flex themes after `shared/_utils.scss`,
whose animation keyframes it reuses. Custom themes should import both in that
order. This CSS is available independently of the future icon loader and helpers.

### Markup and color

Apply `.icon` either to the SVG itself or to a host containing one direct child
SVG. The latter also supports a future web component using light DOM:

```html
<svg class="icon icon-fw" viewBox="0 0 24 24"
     fill="none" stroke="currentColor" aria-hidden="true" focusable="false">
    <path d="M4 12h16M12 4v16" />
</svg>

<span class="icon icon-2x" aria-hidden="true">
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" focusable="false">
        <path d="M4 12h16M12 4v16" />
    </svg>
</span>
```

Place utility classes on the host; do not also add `.icon` to its child SVG.
Each SVG must retain its own `viewBox` and appropriate fill/stroke attributes.
Use `currentColor` for paint that should follow text or button color. The CSS
does not force a fill or stroke, rewrite hardcoded source colors, or assume a
16-unit grid. A shadow-DOM component would need these styles inside its shadow
root; global styles do not cross that boundary.

Decorative icons should be hidden from assistive technology. For an icon-only
button, label the button itself. Meaningful standalone icons need an accessible
name, for example `role="img"` and `aria-label` on their host. CSS cannot infer
these semantics. Existing `.sr-only` utilities remain available.

### Utility classes

| Purpose | Classes |
| --- | --- |
| Relative size | `icon-1x` through `icon-10x` |
| Text-aligned sizes | `icon-2xs`, `icon-xs`, `icon-sm`, `icon-lg`, `icon-xl`, `icon-2xl` |
| Width | `icon-fw` (1.25em) |
| Lists | `icon-ul`, `icon-li` |
| Stacks | `icon-stack`, `icon-stack-1x`, `icon-stack-2x` |
| Appearance | `icon-inverse` |
| Rotation | `icon-rotate-90`, `icon-rotate-180`, `icon-rotate-270`, `icon-rotate-by` |
| Mirroring | `icon-flip-h`, `icon-flip-v`, `icon-flip-hv` |
| Animation | `icon-spin`, `icon-pulse`, `icon-spin-pulse`, `icon-beat`, `icon-fade`, `icon-bounce`, `icon-beat-fade`, `icon-flip`, `icon-shake` |
| Additional shared effects | `icon-throb`, `icon-cylon`, `icon-cylon-vertical` |

The base size follows the surrounding font size, with a minimum of 16px. Size
utilities scale this base after the minimum is applied: at 14px or 15px text,
the normal icon is 16px, `icon-sm` is 14px, `icon-xs` is 12px, and `icon-2xs` is
10px. `icon-2x` is 32px. Set `--icon-min-size: 0px` to disable the base minimum.
Stack layers scale the already-sized stack without applying the minimum again.
Fixed width is explicit; add
`icon-fw` when replacing layouts that relied on FA7's default fixed width.
The base icon occupies a square (1em by 1em); the SVG viewBox preserves the artwork's proportions within it.

For migration, use the corresponding icon utilities listed above and provide SVG markup
with the base `.icon` class. Glyph classes such as `fa-cart-shopping` still need
to be resolved through the icon integration; CSS alone does not replace them.
Existing FA and BI markup and helpers continue to use their existing styles.

### Lists and stacks

Keep the list marker separate from the icon so sizing does not change its indent:

```html
<ul class="icon-ul">
    <li>
        <span class="icon-li" aria-hidden="true">
            <svg class="icon" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" focusable="false">
                <path d="m5 12 4 4 10-10" />
            </svg>
        </span>
        Item text
    </li>
</ul>
```

List indents adapt to RTL. Stack independent SVGs inside an
HTML host; each layer keeps its own coordinate system:

```html
<span class="icon-stack icon-2x" aria-hidden="true">
    <svg class="icon icon-stack-2x" viewBox="0 0 24 24"
         fill="currentColor" focusable="false">
        <circle cx="12" cy="12" r="11" />
    </svg>
    <svg class="icon icon-stack-1x icon-inverse" viewBox="0 0 24 24"
         fill="none" stroke="currentColor" focusable="false">
        <path d="M4 12h16M12 4v16" />
    </svg>
</span>
```

The stack defaults to 2.5em by 2em. Layers are centered and paint in DOM order
unless given `--icon-stack-z-index`. Scale or animate the host for the entire
stack, or individual layers for independent effects. The existing BI helper's
single-SVG stack output is not automatically converted to this HTML structure.

### CSS variables and composition

| Variable | Meaning / default |
| --- | --- |
| `--icon-size` | Base size as a CSS length, `1em` (surrounding font size) |
| `--icon-min-size` | Minimum base size before scaling, `16px`; set to `0px` to disable |
| `--icon-size-factor` | Multiplier applied after the base minimum, `1`; set by size utilities |
| `--icon-align` | Inline vertical alignment, `-0.125em` |
| `--icon-color` | Text color; inherits by default |
| `--icon-rotate` | Static angle, `0deg` |
| `--icon-rotate-angle` | Angle used by `icon-rotate-by` |
| `--icon-scale` | Visual scale without changing layout, `1` |
| `--icon-flip-x`, `--icon-flip-y` | Axis multipliers, `1`; use `-1` to mirror |
| `--icon-shift-x`, `--icon-shift-y` | Visual displacement as CSS lengths, `0` |
| `--icon-li-margin`, `--icon-li-width` | List indent and marker width, `2.5em` and `2em` |
| `--icon-stack-width`, `--icon-stack-height` | Stack dimensions, `2.5em` and `2em` |
| `--icon-stack-align`, `--icon-stack-z-index` | Stack alignment and layer order, `middle` and `auto` |
| `--icon-inverse` | Inverse color, defaults to the theme's `$yiq-text-light` |

For example, `style="--icon-size: calc(1rem + 4px); --icon-rotate: 30deg"`
sets a custom size and angle. Use existing Bootstrap border, spacing, and float
utilities for decoration and surrounding layout; there are no icon-specific
border or pull classes.

Static transforms use the individual CSS `translate`, `rotate`, and `scale`
properties. Animations use `transform`, so spinning an icon does not discard
its static rotation or flip. Existing `rotate-90/180/270`, `flip-h/v/hv`, and
`animate-*` classes also work on `.icon` and `.icon-stack` hosts. Existing offset
variables `--offset-x/y` remain supported with those generic transform classes.

Icon animation utilities accept `--icon-animation-duration`, `--icon-animation-delay`,
`--icon-animation-direction`, `--icon-animation-timing`,
`--icon-animation-iteration-count`, and `--icon-animation-play-state`.
Duration also falls back to the existing `--animation-duration` variable.
Use `icon-spin icon-spin-reverse` for reverse spin; `icon-spin-reverse` is a
modifier, not an animation by itself. `icon-pulse` means stepped rotation, as in FA.

Existing spin, beat, fade, throb, and cylon keyframes are reused. Bounce,
beat-fade, animated flip, and shake have icon-specific keyframes. These effects
provide equivalent purposes, not frame-for-frame copies of every FA animation.
Their adjustable parameters include `--icon-beat-scale`, `--icon-bounce-height`,
`--icon-bounce-rebound`, `--icon-beat-fade-opacity`, `--icon-beat-fade-scale`,
`--icon-flip-axis-x/y/z`, `--icon-flip-angle`, and `--icon-shake-angle`.
Use one animation effect per host; separate stack layers can animate independently.

With `prefers-reduced-motion: reduce`, animations and transitions on icon and
stack hosts are disabled, including existing `animate-*` effects on those hosts.
Static rotation, mirroring, and positioning remain in place.

### Icon TagHelper presentation and modifiers

Use `lib` to select a library by system name or short name. `variant` selects its
variant. `name` accepts an address with optional `flip`, `rotate`, and `stroke-scale`
query modifiers. Append `!` to the icon name to bypass conceptual mapping.

```html
<icon name="hi:cart@sr?rotate=90&amp;stroke-scale=1.1" rotate="0" size="lg" />
<icon name="refresh" animation="spin" animation-duration="2s" />
<icon name="arrow-right!" flip-h="true" class="text-muted" />
```

Explicit helper properties override address modifiers, which override mapping
modifiers, individually. `rotate="0"`, `flip-h="false"` and `flip-v="false"`
explicitly reset inherited values; omission retains them. The `flip` query accepts
`x`, `y`, `xy`, or `none`. Mapping and query rotations retain decimal support;
the helper's `rotate` property accepts integer degrees.

`stroke-scale` is a finite positive multiplier, applied after the library's
`strokeWidthScale`. A value of `1` preserves the library appearance. It can also
be supplied in mapping.json, for example `"arrow": "arrow-right?stroke-scale=1.1"`.
Different source widths retain their proportions.

Existing kits are not modified for rendering overrides. Replacing a baked-in
transformation, or requesting an effective stroke multiplier other than `1`,
renders the cached original source inline. No modifier-specific source cache
entries or kit variants are created. The stroke multiplier is applied by the
renderer, not baked into kit files; consumers of raw kit URLs do not receive it.

Presentation attributes include `size`, `font-scale`, `fw`, `color`, `inverse`,
`animation`, `animation-duration`, `animation-reverse`, `scale`, `shift-x`, and
`shift-y`. Shifts use sixteenths of an em. `scale` changes drawing size only;
`font-scale` changes layout size. Size presets are `2xs`, `xs`, `sm`, `lg`, `xl`,
`2xl`, and `1x` through `10x`. Animations are `spin`, `pulse`, `spin-pulse`, `beat`,
`fade`, `throb`, `cylon`, `cylon-vertical`, `bounce`, `beat-fade`, `flip`, and `shake`.
These presentation options do not themselves require inline rendering.

`class`, `style`, `aria-*` and `data-*` are preserved. Explicit CSS declarations
in `style` follow generated declarations and take precedence. Use `aria-label`
or `aria-labelledby` for a meaningful standalone icon; unlabeled icons are
decorative by default. Lists and stacks continue to use the existing CSS hosts.

### Stack TagHelper

`icon-stack` renders a `span.icon-stack` containing direct `icon` children only.
Razor rejects other child elements, including nested stacks. Layers remain
independent SVGs; their viewBoxes are never merged. Shared presentation attributes
(size, animation, color, scale, shift, rotation and mirroring) apply to the host.
The stroke multiplier belongs to individual icons.

```html
<icon-stack size="2x" aria-label="Confirmed">
    <icon name="circle!" class="icon-stack-2x" />
    <icon name="check" class="icon-stack-1x" />
</icon-stack>
```

Use `icon-stack-1x` and `icon-stack-2x` for layer sizes. DOM order determines
painting order unless `--icon-stack-z-index` is set. Supply the accessible label
on the host when the layers represent one combined symbol.

### Browser components

`sm-icon` and `sm-icon-stack` are native custom elements with light DOM. They
use the same presentation attributes as the Razor helpers, including `lib`,
`variant`, nullable `rotate` / `flip-h` / `flip-v`, and `stroke-scale`. Use explicit
closing tags in browser HTML; `<sm-icon ... />` is not a self-closing HTML element.

```html
<sm-icon name="cart" size="lg" aria-label="Cart"></sm-icon>
<sm-icon-stack size="2x" aria-label="Confirmed">
    <sm-icon name="circle!" class="icon-stack-2x"></sm-icon>
    <sm-icon name="check" class="icon-stack-1x"></sm-icon>
</sm-icon-stack>
```

The storefront and admin layouts expose the immutable manifest URL through
`meta[property="sm:icons"]`. Relative URLs are resolved against
`meta[property="sm:root"]`, including the application's PathBase. Both layouts preload the manifest as a same-origin fetch.
`smartstore.icons.js` is included in the shared `/bundle/js/jquery.js` bundle,
loaded in the head, so it uses the existing bundle minification and versioning.
Other layouts must provide these two meta elements and include the jQuery bundle;
no separate icon script is needed.

`IconKitService` creates the small resolution manifest once per catalog generation.
The browser builds the reverse kit index once, omitting redundant identity mappings
and any search metadata or SVG drawings. Only effective kit-default library/variant
combinations are advertised. Other selections are resolved through `/icons/render`.
Generated manifests live in `App_Data/.cache/IconKits/manifest-{revision}.json` and are
served by `IconController` at `/icons/manifest/{revision}.json` with immutable caching.
Historical files remain available; a missing old revision returns 404, never newer data.
The manifest revision is path-base independent; source/configuration watchers invalidate
its owning catalog. Already open pages retain their current manifest until reloaded.

Kit hits create `use` elements without an individual resolution request. Inline
fallbacks use the Core renderer through `/icons/render`, preserving mapping modifier
precedence and the existing source cache. Matching in-flight requests share one promise.
Up to six fetches run concurrently; up to 256 settled SVG templates are retained per page.
Each instance clones its template and rewrites local SVG IDs to avoid gradient/mask
collisions. Presentation-only changes require no request. Failed requests are evicted;
a later attribute change or reconnection can retry. An older response never replaces a
newer icon selection. Components emit bubbling `icon-load` and `icon-error` events;
the latter exposes the error in `event.detail.error`.

The custom element owns sizing, animation and accessibility; its child SVG is
decorative. Author classes and styles remain on the host and explicit styles follow
generated styles. Set nullable booleans to the strings `true` or `false`; removing the
attribute restores mapping defaults. `sm-icon-stack` accepts direct `sm-icon` children
and reports invalid children through `icon-error`.

Vue applications can call `app.use(Smartstore.Icons)` before mounting. The plugin
recognizes only these two custom-element names and preserves existing compiler rules.
The DataGrid already installs it. No Vue dependency is required by the components.
