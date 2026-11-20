# Migrating legacy icons to Smartstore icons

Use this guide to migrate views, dynamic icon producers, plugin APIs and stored
content from FA/BI renderers to IconKit. The tables preserve reviewed replacements;
they are not evidence that every external caller has been migrated. Configuration,
resolution and rendering contracts are documented in [README.md](README.md).

## Migration workflow

1. Find both the renderer and the producer of its icon value: Razor, C# models,
   JavaScript state changes, CSS pseudo-elements, module metadata and persisted content.
2. Choose a reviewed concept below. Use a qualified direct address only when specific
   artwork is intentional or no suitable concept exists. Preserve style distinctions
   and the approximations noted in the tables.
3. Update the rendering API and presentation together. Preserve IDs, titles,
   accessibility labels, state colors and application selectors. Replace font-specific
   sizing/animation classes; changing only the tag does not convert old class strings.
4. Check the concept in its actual public/admin area and with its module installed.
   Inspect the resolved `data-icon`, missing-icon indicator, dynamic states and RTL
   orientation. Visually check stacks, filled shapes and brand logos.
5. For persisted data, use the conversion pipeline described below; do not blindly
   apply the curated HI substitutions to user-authored FA selections.

```cshtml
@* Before *@
<i class="far fa-trash-can"></i>
<bootstrap-icon name="trash" />

@* After *@
<icon name="trash" size="lg" class="mr-2" title="Trash" />
@await Html.IconAsync("trash?size=lg")
```

Maintain this guide with agreed mapping/API changes. `hugeicons/mapping.json`,
`collections.json`, `kits.json` and module providers define current artwork;
this document retains the legacy-to-concept relationships. Preserve reviewed
exceptions during imports rather than regenerating them from a comparison page.

## Removed APIs and replacement contracts

| Legacy input/API | Replacement and migration requirement |
| --- | --- |
| `bootstrap-icon`, `bootstrap-iconstack` | `icon`, `icon-stack`; use `bi:trash!` to retain an exact BI drawing |
| `Html.Icon()`, `Html.BootstrapIcon()`, `BootstrapIconInternal()`, `BootstrapIconUrl()` | Await `Html.IconAsync(expression, options)`; do not block async rendering |
| `GetIconAsync(name, library, variant, ...)` | Core contract takes `IconAddress`; convenience extensions still accept separate selectors |
| Presentation mixed into resolution/options | Parse `IconExpression`; pass `.Address` to lookup, the expression to rendering; `IconOptions` holds context and HTML attributes |
| Old `@variant` address separator | `/variant`, e.g. `hi:cart!/sr`; no compatibility parser for `@` |
| Vue `bootstrap-icon`, `iconSpriteUrl` | `sm-icon` / `sm-icon-stack`; register `Smartstore.Icons` with Vue (DataGrid already does) |
| DataGrid header `Icon` containing FA classes | A concept/address, e.g. `paperclip`; update external cell/display templates too |
| `EntityPickerTagHelper.IconCssClass` / `icon-css-class` | `Icon` / `icon`; default `search`, empty suppresses the icon |
| `ActionItemModel.IconCssClass` | Icon addresses for product actions; no legacy Razor fallback |
| `ConfirmTagHelper` / `ConfirmModel.IconClass` | `Icon` address; client `confirm2`, `alert2`, `prompt2` also take concepts/addresses |
| `TreeItem.IconClass`, `data-icon-class` | `Icon`, `data-icon`; state options are `defaultCollapsedIcon`, `defaultExpandedIcon`, `collapsedIcon`, `expandedIcon` addresses |
| `MediaEditorCommand.IconClass` / `IconLibrary` | `Icon` concept/address; `IconUrl` still wins for image providers |
| Media preview font names / `iconCssClasses` | `getIconHint().address` and `getPreview(..., { iconSize })`; previews/Dropzone use SVG components |
| `BlockAttribute.Icon` containing FA classes | Concept/address; palette/dialog rendering is server-side, dynamic block lists use `sm-icon` |
| LinkBuilder source metadata containing FA classes | Icon addresses; reuse rendered source icons instead of a second JS mapping |
| Module `IconColor` / `ModuleIcon.Color` | `?color=...` in the manifest `Icon` expression; explicit presentation can override it |
| `IconBrowserModel.Expression` | Nullable `Icon`; persist `model.Icon.Icon?.ToString()` for an editor property named `Icon` |

The foundation namespace is `Smartstore.IconKit`, in `src/Smartstore/IconKit`.
Sources stay in `App_Data/Icons`. `IconKitStarter` handles normal Smartstore
registration; custom hosts use `AddIconKit` / `MapIconKit`, not the removed MVC
icon controller. See [hosting](README.md#svg-preparation-caching-and-hosting).

Media TagHelper extensions now override `ProcessMediaAsync`; pagination extensions
use `AppendItemAsync` / `GetItemInnerContentAsync`; cover-image extensions use
`CreateDropdownAsync` / `CreateDropdownItemAsync`. Await `ThemeVarChainInfoAsync`.
External authentication components use `InvokeAsync`; asynchronous result filters
use `IAsyncResultFilter`. AI menu/translation generators are asynchronous.
SmartLabel, hint helpers and `ThemeVarLabel` remain synchronous; `Html.LocalizedEditor`
retains its API through its existing translation-tool bridge. Do not copy that bridge
into new callers.

### Menus and tabs

`NavigationItem.Icon` accepts a concept/address. `IconLibrary` is obsolete but still
qualifies an unqualified value; an explicit address wins. The two-argument fluent
`Icon(value, library)` overload remains compatible. Keep presentation in `IconClass`
using `icon-fw`, `icon-lg` and shared transforms. Menu templates preserve it,
including the return-case `flip-h`, stacked settings icons and empty slots.

`TabTagHelper.Icon` (`sm-icon`) uses the same address contract; `IconClass`
(`sm-icon-class`) is forwarded, including through `TabFactory`. Image tabs retain
image rendering. For unmatched drawings, keep direct addresses such as
`bi:building-check!`, `bi:calendar-event!`, `bi:car-front!`, `bi:currency-dollar!`,
`bi:menu-down!`, `bi:phone-vibrate!`, `bi:shield-check!`, `fa:calendar-days!/r`,
`fa:ruler-horizontal!/s` and `fa:section!/r`. Source availability depends on the
installed library edition and fallback configuration.

## Client-side state changes

Use `sm-icon` for dynamic artwork and change `name`, not FA glyph classes. Set
`animation="spin"` or `pulse` while busy and remove it on completion. Remove state
colors when leaving the state. Static icons normally render server-side. JavaScript
cannot execute Razor `<icon>` markup.

Common state replacements: `circle-check` → `check`, `exclamation-circle` /
`circle-exclamation` → `alert-danger`, `eye-slash` → `eye-off`, `angle-*` → `chevron-*`.
The DataGrid search reset uses `filter-remove`, not `eraser`; null boolean cells
retain empty slots.

Components render in the light DOM: inspect their child SVG, not Shadow DOM or CSS
custom states. Persist `getSourceNode()` rather than generated `outerHTML`; this
keeps authored attributes/layers and excludes rendering details. Summernote uses
this serialization for save, code view and undo/redo. Follow the
[browser integration contract](README.md#browser-integration).

### Select option icons

The default `selectWrapper` formatter accepts option `data-icon="mail"` or an
address array such as `data-icon='["mail", "lock"]'`. AJAX items use `icon`,
including `init.icon`. A select-level `data-icon` supplies the default for dynamic
results/tags; option values win, empty suppresses it, and images/swatches retain
precedence. Custom formatters retain their own contracts. Prefer kit concepts.

Module/provider choices use the complete expression in `data-icon` or an image URL
in `data-imageurl`. Provider images precede manifest expressions, then module
`icon.svg`, `icon.png` and `hi:plug-02`. `GetIconUrl()` remains an image-only API;
address colors do not recolor image files.

## Presentation and special cases

- Replace `fa-fw` / `bi-fw` with `fw="true"` or `icon-fw`, and FA size/animation
  classes with IconKit attributes/utilities. Do not retain `fa`, `fas`, `far`, `fal`,
  `fab` or `fad`: these now expose unmigrated markup as a red missing-icon mask.
  The webfont assets are removed; this is not a compatibility renderer.
- Preserve accessible labels, explicit flips/rotations and empty-slot widths.
  Migrate the entire stack to `icon-stack`, checking fill, layer size and offsets.
  Source/mapping modifiers are defaults: do not repeat them on the caller.
- `!` bypasses mappings and kit routing, not variant fallback and not inline/sprite
  selection. Use `force-inline="true"` during installation to avoid redirected
  sprite requests. Ensure restricted Razor imports enable the icon TagHelper.
- `mirrorInRtl` applies automatically to listed unqualified concepts. Explicit
  sources are excluded unless `mirror-rtl="true"` is set. Keep deliberate fixed
  orientation; see [RTL policy](README.md#direction-sensitive-concepts).
- The shared collection owns `-outline` / `-fill` shapes: bubble-round,
  bubble-square, circle, diamond, hexagon, polygon, rect, seal, square, star and
  triangle. Use explicit shapes when fill matters: `circle-fill` for status dots,
  `square-fill` for stop controls, `star-fill` / `star-outline` for ratings.
  Unsuffixed concepts remain available; `star-half` is the system half-filled star.
- Keep `stars-tricolor` multicolor; monochrome `stars` or `sparkles` is not an
  automatic replacement. Shared `alert-info`, `alert-danger`, `alert-success` and
  `alert-warning` are pinned system drawings and retain their appearance across libraries.
- Use `compare` for product comparison, `repeat` for repetition, `return` for
  returns/withdrawal, `currency` for the currency selector (not the old typo `curreny`).
  `customer-roles` differs from navigation `customers`; `seo` serves SEO tabs.
  MegaMenu tabs reuse `menu-button`; the former module-only `mega-menu` is removed.

See [presentation](README.md#css-and-presentation) for sizes, transforms, stroke
capabilities and animation behavior. Bare concepts retain the manifest fast path
even with presentation modifiers; explicit source selection requires resolution.

### Sass pseudo-element migration

Replace font glyphs and removed `fontawesome` / `fontawesome-font` mixins with
`icon-mask` / `icon-url`. Change state masks instead of Unicode `content`; reuse
existing icon-mask variables where available. Category paths use `arrow-turn-up`,
star ratings use `star-outline` / `star-fill`, and alerts use `--alert-icon-mask`.
Apply RTL mirroring in CSS and regenerate bundles after changing embedded artwork.
See [Sass masks](README.md#sass-masks). Existing `.bi` icon selectors become `.icon`;
the unrelated menu class `bi-circle` remains.

## Persisted content and story templates

Automatic conversion preserves the selected **FA artwork/style**, rather than
substituting the curated HI concepts below. The core FluentMigrator icon migration
expands `MenuItemRecord.Icon` to 512 characters and runs `IIconMigrator` in its late
data-seeding stage. Successful menu conversion writes the expression and clears
legacy `Style` / `IconColor`; already qualified values are skipped.

`LegacyIconConverter` creates direct `fa:name!/variant` expressions. It preserves
regular, solid, light, duotone and brand selectors (`r`, `s`, `l`, `ds`, `b`) and
moves valid theme color tokens to `color=var(--token)`. Known brand names select
brands. It does not resolve artwork: licensed-only styles/names need installed
sources or configured fallbacks; conversion success does not prove renderability.
Unsupported values produce warnings and remain unconverted; invalid theme colors
can be omitted with a warning. Existing qualified expressions are preserved.

The migrator also handles registered HTML fields (core category/manufacturer
descriptions, product agreement/full description and topic body). HTML conversion
targets empty FA `<i>` elements, preserving unrelated attributes/content. Installed
modules extend the run through `IconMigrationEvent.RegisterHtml<TEntity>(...)` or
`IIconMigrationProvider` for structured data; providers run before HTML fields.
Do not confuse migration providers with `IIconProvider` artwork contributions.

For manual runs, call [IIconMigrator.MigrateAsync](../../../Smartstore.Core/Data/Migrations/Icons/IIconMigrator.cs)
with a caller-owned context and optional report callback. Cancellation/failure can
leave completed batches migrated; review warnings and rerun remaining work. Logs
are written under `App_Data/Logs/icon-migration-*.log`. See the
[converter](../../../Smartstore.Core/Data/Migrations/Icons/LegacyIconConverter.cs)
and [migration event](../../../Smartstore.Core/Data/Migrations/Icons/IconMigrationEvent.cs)
for extension contracts. A source-code audit cannot replace checking stored content.

### Bundled Page Builder story templates

Shipped `.story` archives use current `Icon` / `ButtonIcon` models and curated
Public concepts. Active size/color/display settings and pictures are preserved;
inactive legacy selections and empty FA models were removed. Intentional semantic
changes include a globe for Internet and WLAN artwork for Wi-Fi.

| Legacy FA name | Template concept | Hugeicons source |
| --- | --- | --- |
| percent | discount | badge-percent |
| headphones | headphones | headphones |
| signal | wifi | wifi-02 |
| envelope-open | mail-open | mail-open-01 |
| camera | camera | camera-01 |
| internet-explorer | globe | globe-02 |
| hdd | hard-drive | hard-drive |
| clock | clock | clock-04 |
| headset | headset | headset |
| bolt | bolt | zap |
| question-circle | question-circle | circle-question-mark |
| certificate | certificate | certificate-01 |
| star | star | star |
| credit-card | creditcard | credit-card |

User templates/uploaded archives migrate legacy HTML-block models on temporary
extracted copies before writing records, including nested stories. Bundled templates
skip that import conversion. User conversion retains explicit FA sources/styles
rather than applying the table; originals are unchanged, and errors identify the
story/block. Installed PageBuilder also contributes structured database migration.

## Kits and plugin integration

The old shared **kit** is now a collection included separately by public and admin;
it has no sprite. The system library has no separate kit. Use system `outline` or
`fill`, not removed `/default`; BI uses one `default` variant with exact upstream
filenames, including `-fill` / `-fill-`, not synthetic `light` / `solid` variants.
Old `wwwroot/lib/bi` sprites and `_bootstrap-icons.scss` are removed; BI SVG sources
remain available. See the [BI import recipe](bootstrap/README.md).

Preserve kit declaration order and area boundaries. Public concepts support library
switching; Admin Hugeicons additions use existing names without invented aliases or
redundant mappings. Non-Hugeicons Admin additions require qualified sources. Follow
[Admin/Public rules](README.md#adding-icons-admin-versus-public) and
[fallback rules](README.md#resolve-missing-icons), including when testing a partially
mapped library. An explicit source can intentionally prevent library fallback.

Module-only membership belongs in the module's `Icons.cs` provider, not unconditional
core JSON. Extend existing kits/collections through `IIconProvider`; keep genuinely
generic motifs in core even if currently used by one module. Local JSON wins over
provider defaults. Identical contributions are idempotent; missing modules can mean
missing concepts. Do not hard-code provider-dependent concepts into core consumers.
See [module contributions](README.md#module-contributions) for private SVGs, path
constraints, restart requirements and provider lifetime.

Examples: MediaManager owns its filled media folders and product/mail/user overlays;
core owns file-type concepts and the shared folder-stack drawings. Media trees use
stack bases with deliberate overlays, not the former fixed FA overlay set. PageBuilder
owns editor motifs, AddToAny adds service-picker concepts to admin (fallback `share`),
and AI contributes its specialized commands to admin while generic motifs remain core.
EventFlow uses shared for concepts serving both areas; Homepage/HP contribute public
feature concepts. Do not restore obsolete fixed memberships from old migration notes.

Summernote integration belongs in Smartstore's `globalinit.js` and
`skinning/_summernote.scss`, not vendor code. The adapter supplies SVG icons; its save
slot deliberately retains the original disk SVG. The fork's standalone font is not
imported by Smartstore. The admin-only `htmledit` kit reuses shared formatting concepts;
the forum BBEditor uses shared bold/italic/underline/link/code/quotes. Table-insertion
icons intentionally distinguish the solid new block from the dashed existing block.

### Brand logos use Font Awesome

The curated `brands` kit uses FA brands, with specific BI sources such as `alexa`,
`anthropic`, `apple-music`, `bing` and `microsoft-teams`. Prefer the concept, not an
explicit address: the same sprite may contain multiple libraries. HI targets in
the tables below document HI alternatives, not an override of brand-kit selection.

- `twitter-x` maps to FA `x-twitter`.
- `windows-logo` means the brand; `windows` means overlapping application windows.
- Browser concepts retain `browser-`: chrome → `browser-chrome`, edge →
  `browser-edge`, firefox-browser → `browser-firefox`, firefox → `browser-firefox-alt`,
  safari → `browser-safari`.
- Square concepts use `-square`, e.g. `facebook-square` → FA `square-facebook` and
  `twitter-x-square` → `square-x-twitter`.

Naming/mapping availability does not imply core kit membership. Browser logos,
many square/social/developer logos and competing shop/CMS platforms are intentionally
outside the curated kit; their library sources remain accessible. Module-specific
brands such as PageBuilder's SoundCloud belong to the module provider.

## Use concepts in application code

The following tables retain the reviewed FA/BI relationships. **Use the Concept
column in code.** Target is the current HI mapping or a deliberate qualified source;
area-specific kit overrides and brand defaults can select other artwork. Examples:
admin `box` retains BI artwork; admin `discount` pins HI percent while the public
mapping remains badge-percent. Do not copy a target's baked modifiers to the caller.

The tables are not interchangeable with automatic user-data conversion. Review
fill/outline and style distinctions, especially grouped legacy aliases. Preserve
explicit shape concepts when those distinctions carry meaning.

## Font Awesome to HugeIcons

| Source FA icon classes | Target | Concept |
| --- | --- | --- |
| `fal fa-address-book` | `address-book` | `address-book` |
| `fa fa-exclamation-circle`, `fa-circle-exclamation` | `sys:alert-danger!/outline` | `alert-danger` |
| `fa fa-arrow-down` | `arrow-down-02` | `arrow-down` |
| `far fa-circle-down` | `circle-arrow-down-02` | `arrow-down-circle` |
| `fa fa-arrow-left` | `arrow-left-02` | `arrow-left` |
| `fas fa-arrow-right` | `arrow-right-02` | `arrow-right` |
| `fa fa-turn-up` | `corner-right-up` | `arrow-turn-up` |
| `fa fa-arrow-up` | `arrow-up-02` | `arrow-up` |
| `far fa-circle-up` | `circle-arrow-up-02` | `arrow-up-circle` |
| `fa fa-award` | `star-award-01` | `award` |
| `fa fa-ban`<br>`far fa-ban`<br>`fas fa-ban` | `ban` | `ban` |
| `far fa-chart-bar` | `chart-bar-increasing` | `chart-bar` |
| `fa fa-barcode` | `bar-code-01` | `barcode` |
| `fa fa-bars`<br>`fas fa-bars` | `menu-01` | `bars` |
| `far fa-bars-staggered` | `menu-03` | `bars-staggered` |
| `fa fa-shopping-basket` | `shopping-basket-01` | `basket` |
| `fal fa-bell`<br>`far fa-bell`<br>`fas fa-bell` | `bell` | `bell` |
| `fa fa-blog` | `rss`<br>Approximation: HI RSS omits the pen. | `blog` |
| `fa fa-bold` | `bold?stroke-scale=1.5` | `bold` |
| `fa fa-bolt`<br>`far fa-bolt` | `zap` | `bolt` |
| `fa fa-book` | `book-02` | `book` |
| `fas fa-box-open` | `package-open` | `box-open` |
| `fal fa-brackets-curly` | `braces` | `braces` |
| `far fa-building` | `building-03` | `building` |
| `fa fa-calendar`<br>`far fa-calendar`<br>`far fa-calendar-days` | `calendar-03` | `calendar` |
| `far fa-calendar-check` | `calendar-check` | `calendar-check` |
| `fa fa-caret-down` | `chevron-down?stroke-scale=1.25`<br>Approximation: HI uses a chevron instead of a filled triangle. | `caret-down` |
| `fa fa-shopping-cart`<br>`fal fa-shopping-cart`<br>`far fa-shopping-cart` | `shopping-cart-02` | `cart` |
| `fa fa-cart-arrow-down` | `shopping-cart-add-02` | `cart-add` |
| `fal fa-certificate`<br>`far fa-certificate` | `certificate-01` | `certificate` |
| `fa fa-check`<br>`far fa-check`<br>`fas fa-check` | `check?stroke-scale=1.5` | `check` |
| `fa fa-check-double` | `check-check` | `check-double` |
| `fa fa-angle-down`<br>`fal fa-angle-down`<br>`fas fa-angle-down`<br>`fa fa-chevron-down`<br>`fas fa-chevron-down` | `chevron-down?stroke-scale=1.25` | `chevron-down` |
| `fa fa-angle-left`<br>`fa fa-chevron-left`<br>`far fa-chevron-left` | `chevron-left?stroke-scale=1.25` | `chevron-left` |
| `fa fa-angle-right`<br>`fa fa-chevron-right`<br>`far fa-chevron-right`<br>`fas fa-chevron-right` | `chevron-right?stroke-scale=1.25` | `chevron-right` |
| `fa fa-angle-up`<br>`fas fa-angle-up`<br>`fa fa-chevron-up` | `chevron-up?stroke-scale=1.25` | `chevron-up` |
| `fa fa-angle-double-down` | `arrow-down-double?stroke-scale=1.25` | `chevrons-down` |
| `fa fa-angles-left` | `arrow-left-double?stroke-scale=1.25` | `chevrons-left` |
| `fa fa-angles-right` | `arrow-right-double?stroke-scale=1.25` | `chevrons-right` |
| `fa fa-angle-double-up` | `arrow-up-double?stroke-scale=1.25` | `chevrons-up` |
| `fa fa-circle`<br>`fas fa-circle` | `circle` | `circle` |
| `fa fa-clipboard`<br>`far fa-clipboard` | `clipboard-check` | `clipboard` |
| `fa fa-clock`<br>`far fa-clock` | `clock-04` | `clock` |
| `fa fa-times`<br>`fal fa-times`<br>`fas fa-times`<br>`fa fa-xmark` | `cancel-01?stroke-scale=1` | `close` |
| `fas fa-cloud-arrow-up` | `cloud-upload` | `cloud-upload` |
| `fa fa-code` | `code-xml` | `code-xml` |
| `far fa-comment-dots` | `message-02` | `comment` |
| `fa fa-comments` | `message-multiple-01` | `comments` |
| `fas fa-adjust` | `contrast` | `contrast` |
| `fa fa-clone`<br>`fa fa-copy`<br>`far fa-copy` | `copy-02` | `copy` |
| `far fa-credit-card` | `credit-card` | `creditcard` |
| `fab fa-css3` | `css-3` | `css3` |
| `fa fa-cube` | `cube` | `cube` |
| `fa fa-database` | `database` | `database` |
| `fa fa-phone-laptop` | `computer-phone-sync` | `devices` |
| `fa fa-save` | `floppy-disk` | `disk` |
| `fa fa-download`<br>`fal fa-download` | `download-01` | `download` |
| `far fa-droplet-slash` | `droplet-off` | `droplet-off` |
| `fa fa-edit`<br>`far fa-edit` | `pencil-edit-02` | `edit` |
| `fa fa-ellipsis`<br>`fa fa-ellipsis-h` | `ellipsis` | `ellipsis` |
| `fa fa-ellipsis-v` | `ellipsis-vertical` | `ellipsis-v` |
| `fa fa-envelope`<br>`fal fa-envelope`<br>`far fa-envelope`<br>`fas fa-envelope` | `mail-01` | `mail` |
| `fal fa-envelope-open-text` | `mail-open` | `mail-open-text` |
| `fas fa-euro-sign` | `euro` | `euro` |
| `fa fa-right-left` | `arrow-left-right` | `exchange` |
| `fa fa-exclamation` | `bi:exclamation-lg` | `exclamation` |
| `fa fa-up-right-from-square` | `link-square-01` | `external-link` |
| `fa fa-eye`<br>`fal fa-eye`<br>`far fa-eye` | `view` | `eye` |
| `fa fa-eye-slash`<br>`far fa-eye-slash` | `view-off-slash` | `eye-off` |
| `fab fa-facebook` | `facebook-01` | `facebook` |
| `far fa-file` | `file-empty-01` | `file` |
| `far fa-file-archive` | `file-archive` | `file-archive` |
| `far fa-file-audio` | `file-audio` | `file-audio` |
| `far fa-file-code` | `file-code` | `file-code` |
| `fas fa-file-csv` | `csv-01` | `file-csv` |
| `far fa-file-image` | `file-image` | `file-image` |
| `far fa-file-pdf` | `pdf-01` | `file-pdf` |
| `far fa-file-powerpoint` | `file-02`<br>Approximation: generic document; presentation detail is missing. | `file-presentation` |
| `far fa-file-excel` | `file-spreadsheet` | `file-spreadsheet` |
| `fal fa-file-lines`<br>`far fa-file-lines`<br>`fas fa-file-lines`<br>`fa fa-file-text` | `file-02` | `file-text` |
| `far fa-file-video` | `file-video` | `file-video` |
| `far fa-file-word` | `doc-01` | `file-word` |
| `fas fa-film` | `film-01` | `film` |
| `fa fa-filter` | `filter` | `filter` |
| `fa fa-fire` | `fire` | `fire` |
| `far fa-folder`<br>`fas fa-folder` | `folder-01` | `folder` |
| `fa fa-folder-open`<br>`fas fa-folder-open` | `folder-02` | `folder-open` |
| `fa fa-font` | `text-font` | `font` |
| `fa fa-forward` | `forward-01` | `forward` |
| `fa fa-cog`<br>`fal fa-cog`<br>`far fa-cog` | `settings-01` | `gear` |
| `fa fa-gift`<br>`fa fa-gifts` | `gift` | `gift` |
| `fab fa-github` | `github` | `github` |
| `fa fa-globe`<br>`far fa-globe` | `globe-02` | `globe` |
| `fab fa-google` | `google` | `google` |
| `fa fa-th` | `dialpad-square-01` | `grid` |
| `fa fa-th-large` | `grid-view` | `grid-large` |
| `fa fa-grip` | `grip` | `grip` |
| `fas fa-grip-vertical` | `grip-vertical` | `grip-v` |
| `far fa-hammer` | `hammer` | `hammer` |
| `fa fa-dolly` | `folder-transfer` | `folder-transfer` |
| `fal fa-heart` | `favourite` | `heart` |
| `fa fa-home` | `home-05` | `home` |
| `fa fa-hourglass-start` | `loading-01` | `hourglass` |
| `fab fa-html5` | `html-5` | `html5` |
| `fa fa-image`<br>`far fa-image` | `image-02` | `image` |
| `fa fa-inbox` | `inbox` | `inbox` |
| `fa fa-info` | `info` | `info` |
| `fa fa-circle-info`<br>`fa fa-info-circle` | `information-circle` | `info-circle` |
| `fab fa-instagram` | `instagram` | `instagram` |
| `fa fa-italic` | `text-italic` | `italic` |
| `fa fa-key` | `key-01` | `key` |
| `fa fa-language` | `languages` | `language` |
| `far fa-laptop`<br>`fas fa-laptop` | `laptop` | `laptop` |
| `far fa-layer-group` | `layers-01` | `layers` |
| `fa fa-link`<br>`fal fa-link` | `link-01` | `link` |
| `fab fa-linkedin` | `linkedin-01` | `linkedin` |
| `fa fa-list`<br>`far fa-stream`<br>`fa fa-th-list` | `left-to-right-list-dash` | `list` |
| `fa fa-rectangle-list`<br>`far fa-rectangle-list` | `clipboard-list` | `list-square` |
| `fa fa-location-dot` | `location-01` | `location` |
| `fa fa-lock` | `lock-keyhole` | `lock` |
| `fa fa-lock-open` | `lock-open` | `lock-open` |
| `fa fa-right-to-bracket` | `login-02` | `login` |
| `fal fa-right-from-bracket`<br>`fas fa-right-from-bracket` | `logout-02` | `logout` |
| `fa fa-wand-magic-sparkles` | `magic-wand-04` | `sparkles` |
| `fa fa-mail-forward` | `mail-send-01` | `mail-forward` |
| `fa fa-expand` | `full-screen` | `maximize` |
| `fa fa-bullhorn` | `megaphone-02` | `megaphone` |
| `fa fa-compress` | `minimize` | `minimize` |
| `fa fa-minus` | `minus?stroke-scale=1.5` | `minus` |
| `far fa-mobile-alt`<br>`fa fa-mobile-screen-button` | `smart-phone-01` | `mobile` |
| `far fa-money-bill-1` | `hand-coins` | `money` |
| `fa fa-desktop`<br>`far fa-desktop`<br>`fa fa-display` | `computer` | `monitor` |
| `fa fa-up-down-left-right` | `arrow-all-direction` | `move` |
| `far fa-newspaper` | `newspaper` | `newspaper` |
| `fa fa-fill-drip`<br>`fas fa-fill-drip` | `paint-bucket` | `paint-bucket` |
| `fa fa-paint-brush`<br>`fa fa-paintbrush` | `paintbrush` | `paint-brush` |
| `fa fa-paperclip` | `paperclip` | `paperclip` |
| `fa fa-pause` | `pause` | `pause` |
| `far fa-pause-circle` | `pause-circle` | `pause-circle` |
| `fa fa-pencil`<br>`fas fa-pencil` | `edit-03` | `pen` |
| `fal fa-phone`<br>`fas fa-phone-flip` | `call` | `phone` |
| `fa fa-eye-dropper` | `dropper` | `dropper` |
| `fa fa-caret-right`<br>`fa fa-play` | `play` | `play` |
| `far fa-play-circle` | `play-circle-02` | `play-circle` |
| `fa fa-plus` | `plus?stroke-scale=1.5` | `plus` |
| `fa fa-plus-circle` | `add-circle` | `plus-circle` |
| `fa fa-plus-square`<br>`fal fa-plus-square` | `add-square` | `plus-square` |
| `fa fa-power-off` | `power` | `power` |
| `fa fa-print` | `printer` | `printer` |
| `fa fa-puzzle-piece` | `puzzle` | `puzzle` |
| `fa fa-question`<br>`fas fa-question` | `chat-question` | `question` |
| `fa fa-circle-question`<br>`far fa-circle-question`<br>`fa fa-question-circle`<br>`far fa-question-circle`<br>`fas fa-question-circle` | `circle-question-mark` | `question-circle` |
| `fa fa-quote-left` | `quote` | `quotes` |
| `fa fa-recycle` | `recycle-01` | `recycle` |
| `fa fa-redo` | `redo-02` | `redo` |
| `fa fa-rotate`<br>`fa fa-sync` | `refresh-01` | `refresh` |
| `fa fa-retweet` | `repeat` | `repeat` |
| `fa fa-reply` | `reply` | `reply` |
| `fa fa-reply-all`<br>`fal fa-reply-all` | `reply-all` | `reply-all` |
| `fa fa-robot` | `robotic` | `robot` |
| `fas fa-rotate-left` | `rotate-ccw-square` | `rotate-left` |
| `fa fa-rotate-right` | `rotate-cw-square` | `rotate-right` |
| `fa fa-rss` | `rss` | `rss` |
| `fas fa-ruler-combined` | `ruler` | `ruler` |
| `fas fa-balance-scale` | `balance-scale` | `balance` |
| `fa fa-cut` | `scissors` | `cut` |
| `fa fa-magnifying-glass`<br>`fa fa-search`<br>`far fa-search` | `search-01` | `search` |
| `fa fa-paper-plane`<br>`far fa-paper-plane`<br>`fa fa-send` | `sent` | `send` |
| `fa fa-cogs` | `gears` | `gears` |
| `fa fa-share`<br>`far fa-share` | `share-08` | `share` |
| `far fa-share-square` | `share-01` | `share-square` |
| `fas fa-shield-halved` | `shield-half` | `shield-half` |
| `fa fa-signal` | `signal-full-02` | `signal` |
| `fa fa-sitemap` | `hierarchy-square-02` | `nodes` |
| `fa fa-sliders-h` | `sliders-horizontal` | `sliders` |
| `fa fa-spinner`<br>`fas fa-spinner` | `loader-circle` | `spinner` |
| `far fa-square`<br>`fa fa-stop`<br>`fas fa-stop` | `square` | `square` |
| `fa fa-star` | `star` | `star` |
| `fa fa-star-half` | `sys:star-half!/fill` | `star-half` |
| `far fa-tablet-alt`<br>`fa fa-tablet-screen-button`<br>`far fa-tablet-screen-button`<br>`fas fa-tablet-screen-button` | `tablet-02` | `tablet` |
| `fas fa-tag` | `tag-01` | `tag` |
| `fa fa-tags`<br>`fad fa-tags` | `tags` | `tags` |
| `fa fa-tasks` | `clipboard-list` | `tasks` |
| `fa fa-thumbs-down` | `thumbs-down` | `thumbs-down` |
| `fa fa-thumbs-up` | `thumbs-up` | `thumbs-up` |
| `fa fa-delete`<br>`far fa-trash`<br>`far fa-trash-alt`<br>`fa fa-trash-can`<br>`fal fa-trash-can`<br>`far fa-trash-can` | `delete-02` | `trash` |
| `fa fa-trophy` | `trophy` | `trophy` |
| `fa fa-truck`<br>`fal fa-truck`<br>`fa fa-truck-moving` | `delivery-truck-01` | `truck` |
| `fal fa-truck-loading` | `shipping-loading` | `truck-loading` |
| `far fa-tv`<br>`fas fa-tv` | `tv-01` | `tv` |
| `fab fa-x-twitter` | `new-twitter` | `twitter-x` |
| `fa fa-underline` | `text-underline` | `underline` |
| `fa fa-undo` | `undo-02` | `undo` |
| `fa fa-unlink`<br>`fas fa-unlink` | `unlink-01` | `unlink` |
| `fa fa-unlock-keyhole`<br>`fal fa-unlock-keyhole` | `lock-keyhole-open` | `unlock` |
| `fa fa-arrow-up-from-bracket`<br>`fa fa-upload`<br>`far fa-upload` | `upload-01` | `upload` |
| `fa fa-user`<br>`fal fa-user` | `user-02` | `user` |
| `fa fa-user-plus` | `user-add-01` | `user-add` |
| `fa fa-user-check` | `user-check-01` | `user-check` |
| `fal fa-user-circle`<br>`fas fa-user-circle` | `user-circle` | `user-circle` |
| `far fa-user-edit` | `user-edit-01` | `user-edit` |
| `fa fa-user-secret` | `incognito` | `privacy` |
| `fa fa-user-shield` | `user-shield-01` | `user-shield` |
| `fa fa-group`<br>`fa fa-users` | `user-group-02` | `user-group` |
| `fa fa-wallet` | `wallet-01` | `wallet` |
| `fa fa-exclamation-triangle`<br>`fa fa-triangle-exclamation`<br>`fa fa-warning` | `alert-02` | `warning` |
| `far fa-window-restore` | `browser` | `browser` |
| `fa fa-wrench` | `wrench-01` | `wrench` |
| `fab fa-youtube` | `youtube` | `youtube` |

## Bootstrap Icons to HugeIcons

| Source BI icon name | Target | Concept |
| --- | --- | --- |
| `archive` | `archive-02` | `archive` |
| `arrow-90deg-up` | `corner-right-up` | `arrow-turn-up` |
| `arrow-up-short` | `arrow-up-02` | `arrow-up` |
| `aspect-ratio` | `aspect-ratio` | `aspect-ratio` |
| `bar-chart-line` | `chart-bar-increasing` | `chart-bar` |
| `lightning` | `zap` | `bolt` |
| `box` | `bi:box!` | `box` |
| `boxes` | `boxes` | `boxes` |
| `braces-asterisk` | `bi:braces-asterisk` | `braces-asterisk` |
| `building` | `building-03` | `building` |
| `camera-video` | `video-02` | `camera-video` |
| `cart-check` | `shopping-cart-check-02` | `cart-check` |
| `check`<br>`check-lg` | `check?stroke-scale=1.5` | `check` |
| `chevron-left` | `chevron-left?stroke-scale=1.25` | `chevron-left` |
| `chevron-right` | `chevron-right?stroke-scale=1.25` | `chevron-right` |
| `chevron-double-left` | `arrow-left-double?stroke-scale=1.25` | `chevrons-left` |
| `chevron-double-right` | `arrow-right-double?stroke-scale=1.25` | `chevrons-right` |
| `circle-fill` | `sys:circle!/fill` | `circle-fill` |
| `clipboard` | `clipboard-check` | `clipboard` |
| `clock` | `clock-04` | `clock` |
| `x`<br>`x-lg` | `cancel-01?stroke-scale=1` | `close` |
| `x-circle` | `cancel-circle` | `close-circle` |
| `columns-gap` | `layout-dashboard` | `dashboard` |
| `chat-dots` | `message-02` | `comment` |
| `chat-square-text`<br>`chat-square-text-fill` | `message-square-text` | `comment-text` |
| `copy` | `copy-02` | `copy` |
| `credit-card` | `credit-card` | `creditcard` |
| `database-add` | `database-add` | `database-add` |
| `database-down` | `database-export` | `database-export` |
| `database-up` | `database-import` | `database-import` |
| `floppy` | `floppy-disk` | `disk` |
| `download` | `download-01` | `download` |
| `pencil-square` | `pencil-edit-02` | `edit` |
| `three-dots` | `ellipsis` | `ellipsis` |
| `envelope` | `mail-01` | `mail` |
| `envelope-open-heart` | `mail-open-love` | `mail-heart` |
| `envelope-paper` | `mail-open-01` | `mail-open` |
| `eraser` | `remove-formatting` | `eraser` |
| `currency-euro` | `euro` | `euro` |
| `arrow-left-right` | `arrow-left-right` | `exchange` |
| `eye`<br>`eye-fill` | `view` | `eye` |
| `facebook` | `facebook-01` | `facebook` |
| `file-earmark-pdf` | `pdf-01` | `file-pdf` |
| `file-earmark-richtext` | `file-pen` | `file-pen` |
| `funnel` | `filter` | `filter` |
| `gear` | `settings-01` | `gear` |
| `gift` | `gift` | `gift` |
| `globe-americas` | `earth` | `earth` |
| `grid-3x3-gap` | `dialpad-square-01` | `grid` |
| `hdd` | `hard-drive` | `hdd` |
| `heart` | `favourite` | `heart` |
| `card-image`<br>`image` | `image-02` | `image` |
| `images` | `images` | `images` |
| `inboxes` | `inbox` | `inbox` |
| `info-circle` | `information-circle` | `info-circle` |
| `input-cursor-text` | `input-cursor-text` | `input-cursor` |
| `stack` | `layers-01` | `layers` |
| `layers-half` | `background` | `background` |
| `lightbulb` | `lightbulb` | `lightbulb` |
| `graph-up` | `chart-analysis` | `chart-up` |
| `graph-up-arrow` | `chart-analysis` | `chart-line-arrow` |
| `link-45deg` | `link-01` | `link` |
| `list-ul`<br>`view-list` | `left-to-right-list-dash` | `list` |
| `geo-alt` | `location-01` | `location` |
| `box-arrow-in-right` | `login-02` | `login` |
| `box-arrow-left` | `logout-02` | `logout` |
| `megaphone` | `megaphone-02` | `megaphone` |
| `menu-button-fill` | `dropdown-field-type` | `menu-button` |
| `palette` | `paint-board` | `palette` |
| `vector-pen` | `pen-tool-03` | `pen-nib` |
| `pencil`<br>`pencil-fill` | `edit-03` | `pen` |
| `percent` | `badge-percent` | `discount` |
| `play-fill` | `play` | `play` |
| `power` | `power` | `power` |
| `printer` | `printer` | `printer` |
| `puzzle-fill` | `puzzle` | `puzzle` |
| `question-circle-fill` | `circle-question-mark` | `question-circle` |
| `receipt` | `invoice-01` | `receipt` |
| `recycle` | `recycle-01` | `recycle` |
| `arrow-repeat`<br>`repeat` | `repeat` | `repeat` |
| `arrows-expand-vertical` | `unfold-horizontal` | `unfold` |
| `arrow-clockwise` | `rotate-cw-square` | `rotate-right` |
| `scissors` | `scissors` | `cut` |
| `search` | `search-01` | `search` |
| `send` | `sent` | `send` |
| `shield` | `shield-01` | `shield` |
| `shuffle` | `shuffle` | `shuffle` |
| `diagram-3` | `hierarchy-square-02` | `nodes` |
| `sliders` | `sliders-horizontal` | `sliders` |
| `sliders2-vertical` | `sliders-vertical` | `sliders-v` |
| `sort-down-alt` | `sorting-02` | `sort-down` |
| `sort-numeric-down` | `sorting-1-9` | `sort-numeric` |
| `sort-up-alt` | `arrow-up-narrow-wide` | `sort-up` |
| `stars-tricolor` | `sys:stars-tricolor!/fill` | `stars-tricolor` |
| `star-half` | `sys:star-half!/fill` | `star-half` |
| `stickies` | `sticky-note-02` | `sticky-note` |
| `shop-window` | `store-01` | `store` |
| `tags` | `tags` | `tags` |
| `card-checklist`<br>`list-check` | `clipboard-list` | `tasks` |
| `text-wrap` | `text-wrap` | `text-wrap` |
| `textarea-resize` | `resize-field-rectangle` | `text-resize` |
| `hand-thumbs-down` | `thumbs-down` | `thumbs-down` |
| `hand-thumbs-up` | `thumbs-up` | `thumbs-up` |
| `tools` | `repair` | `tools` |
| `trash` | `delete-02` | `trash` |
| `trophy` | `trophy` | `trophy` |
| `truck` | `delivery-truck-01` | `truck` |
| `upload` | `upload-01` | `upload` |
| `person` | `user-02` | `user` |
| `person-badge` | `user-account` | `user-badge` |
| `person-circle` | `user-circle` | `user-circle` |
| `people`<br>`people-fill` | `user-group-02` | `user-group` |
| `exclamation-triangle` | `alert-02` | `warning` |
| `emoji-wink` | `winking` | `wink` |
