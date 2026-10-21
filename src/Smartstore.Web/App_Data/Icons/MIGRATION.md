# Migrating legacy icons to Smartstore icons

This is the maintained migration reference for developers, coding agents and plugin
vendors. Plugins maintained outside this workspace can use the same replacements.
The current system default is HugeIcons (`hi`), variant `stroke-rounded` (`sr`).
These tables contain names and mappings only; they do not depend on a diagnostic
Razor view, generated SVG output or temporary import reports.

## Typed resolution and presentation

- `Html.IconAsync` accepts an `IconExpression` or its string representation, plus optional
  `IconOptions` for render context, inline output and HTML attributes. Put selectors
  and presentation in the expression; the helper encapsulates the renderer.

- Replace separate `GetIconAsync(name, library, variant, area, token)` arguments with
  `GetIconAsync(new IconAddress(name, library, variant), area, token)` for unqualified names.
- Parse qualified names with `IconAddress.Parse`; parse strings containing modifiers with
  `IconExpression.Parse` and pass only `.Address` to the icon service.
- Pass `IconExpression` directly to `RenderAsync`, or pass an already resolved icon
  and its separate `IconPresentation`. Stack hosts accept presentation without an address.
  `IconOptions` contains render context and HTML attributes only. `IconInfo` retains
  mapping defaults and source metadata, but no caller color, size, animation or expression.
- Convenience extensions accept name, library and variant and forward an `IconAddress`
  to the service. `GetSvgAsync` returns source artwork; presentation belongs to the renderer
  or standalone SVG formatting.


## Keep this reference current

Update this file as part of every agreed migration change: new source icons,
corrected mappings, presentation rules, exceptions and helper replacements.
Preserve reviewed decisions when importing new library versions. Do not regenerate
this document blindly or treat a temporary comparison page as its source of truth.

- `hugeicons/mapping.json` defines concept-to-HI targets; `kits.json` defines concrete cross-library overrides.
- This file preserves the legacy FA/BI-to-concept relationships needed by integrators.
- When a concept or HI assignment changes, update every affected table row here.
- Record uncertain visual approximations and exceptions instead of implying exact equivalence.
- Keep this guide in English. Display names and search tags are not localized here.

## Client-side state changes

Dynamic icons use `sm-icon` and change `name` instead of swapping Font Awesome
classes. Busy indicators also set `animation="spin"` (or `pulse`) and remove the
attribute when returning to the idle state. Keep state colors in CSS classes
(`text-muted`, `text-danger`, etc.) and remove them when leaving that state.
Unchanging icons remain server-rendered.

Migrated callers include checkout/topic submission, password rules, username
availability, download panels/changelogs, tax locks, import mappings, maintenance
panels, payment/search settings, PageBuilder fullscreen/block visibility and busy
indicators, GeoBlocker tests, HP contact submission, CustomDashboard edit mode,
Shopware import progress and SupportAgent collapse/copy controls.

Use `check` for legacy `circle-check`, `alert-danger` for `exclamation-circle` or
`circle-exclamation`, `eye-off` for `eye-slash`, and `chevron-*` for `angle-*`.
These state icons are available in the applicable public/admin kits. The cheatsheet and the independent `confirm2` CSS-class
contract are excluded from this migration.

## Select option icons

The default `selectWrapper` formatter accepts an icon concept/address in an
option's `data-icon`, or an array of addresses. AJAX items use `icon` with the
same contract, including initial loading and lazy selections (`init.icon`).
For example, use `data-icon="mail"` or `data-icon='["mail", "lock"]'`.
Icons render through `sm-icon` in both results and selections; prefer kit concepts
to avoid individual SVG requests. Icons count as rich content on touch devices.
Custom formatters retain their own contracts.

Export deployment icons use `folder-open`, `mail`, `hi:cloud-server!/sr` (HTTP POST),
`database-export` (FTP), and `hi:folder-upload!/sr` (public folder).
AddToAny reuses existing kit concepts where available, including `twitter-x`,
and uses `share` as its fallback. AddToAny's `Icons` provider contributes 19 additional
brand concepts and SMS exclusively to the admin kit for the service picker.
Callers use these concepts instead of explicit FA addresses.

## Collections and areas

The former shared kit is now the `shared` collection in `collections.json`.
Both `public` and `admin` include it through `use` and resolve its concepts
independently. `brands` applies to both areas. First kit declaration wins within
an area; first collection in `use` wins, followed by local kit overrides.
Collections never produce sprites. Existing `shared-*.svg` cache files are only
historical artifacts. Server helpers derive the MVC area, while `sm-icon` uses
the area declared on the manifest meta element or its explicit `area` attribute.

IconBrowser kit visibility is independent of area membership: only `showInPicker`
and `ShowHiddenKits` govern its source list. Selected previews retain the chosen
kit sprite, or its concrete source with modifiers, even across areas.

## System library icons

The system library has no separate kit. `not-found`, `smartstore` and
`stars-tricolor` belong to collection `shared`, pinned to their concrete `outline` or `fill` variant.
Missing icons continue to use an external area-kit sprite symbol in danger color.

## Legacy Bootstrap assets removed

The old `wwwroot/lib/bi` sprites and `_bootstrap-icons.scss` have been removed.
Bootstrap drawings remain available through the IconKit `bi` library. DevTools
widget-zone controls now use server-rendered concepts (`power`, `eye`, `eye-off`,
`arrow-right`), preserving the existing visibility selectors. Empty fixed-width
slots use `icon icon-fw`; legacy `.bi` selectors must target `.icon` instead.
SupportAgent and TopM icon markup also uses IconKit; unmapped drawings use explicit
BI addresses. The menu-specific `bi-circle` class is independent and remains.

## Explicit shape concepts

The shared collection provides `-outline` and `-fill` concepts for `bubble-round`,
`bubble-square`, `circle`, `diamond`, `hexagon`, `polygon`, `rect`, `seal`,
`square`, `star` and `triangle`. Each concept pins its source to
`sys:<shape>!/outline` or `sys:<shape>!/fill` (without the concept suffix), independently of the configured default library.
The SVG files live in the system library, but these 22 concepts belong only to
the shared collection. Existing unsuffixed concepts remain unchanged.
Use, for example, `<icon name="star-outline" />` or `<icon name="star-fill" />`.
Delivery-time status indicators use `circle-fill`, including cart and StorageDisplay views.
AI and Shopware import stop buttons use `square-fill`. Admin review headings use
`star-fill`; ProductSpecificationTable uses `star-fill` for earned stars and
`star-outline` for remaining stars. Legacy PageBuilder icon choices remain unchanged.

The shared collection's `star-half` concept pins `sys:star-half!/fill`: the same star geometry
as `star-outline` and `star-fill`, with a complete outline and the fill clipped to
the left half of the viewBox. It replaces the former admin-kit membership.

## Bootstrap TagHelper removal

The `bootstrap-icon` and `bootstrap-iconstack` Razor TagHelpers have been removed.
Use `<icon name="bi:trash!" />` for an original Bootstrap drawing, and `<icon-stack>`
for stacks. Replace `bi-fw` with `fw="true"`. The cheatsheet uses explicit BI
addresses to preserve its library comparisons; its old reduced-sprite mirroring
workarounds are no longer needed. The synchronous `Html.Icon()`, `Html.BootstrapIcon()`,
`BootstrapIconInternal()` and `BootstrapIconUrl()` helpers have also been removed.
Use `await Html.IconAsync(...)`. AI dialog openers render the shared `stars-tricolor`
concept server-side. Their generators are asynchronous. `Html.LocalizedEditor()`
retains its synchronous API through one deliberate, documented `.Await()` bridge
to `GenerateTranslationToolAsync()`; its Razor callers remain unchanged.

## Use concepts in application code

The first column lists existing FA CSS classes or BI names. The second column gives
the current target (HI name or qualified source address), and the third gives the concept to use with the new API. Multiple source
styles or aliases in a cell share the same destination. Repeated concept names
across the FA and BI tables are intentional.

Prefer the **concept** in views and plugins so a configured library can supply its
own mapping. The target documents the current default artwork; do not hard-code
it unless that specific library and drawing are intentional.

```cshtml
@* Before *@
<i class="far fa-trash-can"></i>
<bootstrap-icon name="trash" />

@* After: both become *@
<icon name="trash" />

@await Html.IconAsync("trash?size=2x")
@* With additional HTML attributes: *@
<icon name="trash" size="2x" class="mr-2" title="Trash" />
```

`name="trash"` currently maps to HI `delete-02`. Query modifiers shown in an HI
target are already part of its mapping; do not repeat them on the caller.
For example, use `name="check"`, not the target's `stroke-scale` modifier again.
Explicit library selection uses `lib="hi"`; `variant="sr"` selects Stroke Rounded.

Do not transfer FA style classes (`fa`, `fas`, `far`, `fal`, `fab`) onto the new SVG.
They describe the old font renderer. A filled/outline or brand distinction may need
visual review because the default HI variant is stroke-based.

## My Account menu and menu extensions

All built-in menu templates render `NavigationItem.Icon` through the `icon`
TagHelper. Use a concept name such as `cart` or a qualified address such as
`bi:terminal!`; no separate library is needed. `NavigationItem.IconLibrary` is obsolete. The two-argument fluent
`Icon(value, library)` overload remains compatible and produces an address. The property
still qualifies an unqualified name for older extensions; an explicit address wins.

`IconClass` supplies presentation classes independently of the address. Use
`icon-fw`, `icon-lg` and the shared transform utilities instead of FA utilities.
The admin sitemap stores concepts/addresses directly; its `iconClass` attribute is
preserved. The return-case menu retains `flip-h`.

The settings menu retains its circular backgrounds through `icon-stack`. Empty
icon slots retain their width. Standard menu templates, the HP menu templates and
the menu editor list all preserve `IconClass`.

Existing database menu selections remain stored as legacy FA names. Their provider
emits concrete FA SVG addresses at runtime and moves the selected color into
`IconClass`; no database migration is performed. Legacy light/duotone selections
use the available solid SVG variant. FA Pro-only artwork may still require a
custom source. Edit-mode link metadata is adapted at the menu provider boundary.

Script-swapped font icons and other non-menu consumers remain outside the menu
migration. See [MENU-MIGRATION.md](MENU-MIGRATION.md) for the audit.

All workspace AdminMenuProvider implementations and MenuBuiltEvent consumers
have also been audited. Remaining source-only HI names use explicit addresses:
`hi:alarm-clock!/sr`, `hi:bell-plus!/sr` and `hi:chat!/sr`.
CartApproval uses `cart-check`; GDPR uses `privacy`.
See the provider/consumer audit in [MENU-MIGRATION.md](MENU-MIGRATION.md).

## Store dashboard report

StoreDashboardReport uses the icon TagHelper and reviewed concepts for all 13
statistics icons, replacing its Bootstrap-specific names and renderer.

## MediaManager Vue icons

MediaManager's built-in Vue templates use `sm-icon`; album overlays use
`sm-icon-stack`. Folder and sort states bind concept names. The upload status
chevron follows Vue state, while pause/resume artwork is pre-rendered and switched
by CSS. Special folders and album overlay metadata are normalized at the
MediaManager controller boundary; other album consumers keep their current contract.

`Smartstore.media.getIconHint` now supplies `address` alongside the legacy `name`.
SVG consumers use address, while existing font-based previews continue using name.
The shared Dropzone wrapper supports both SVG custom elements and legacy font
elements for preview and completion status. Existing confirmation-dialog icon
options and the server-rendered MediaEditorCommand extension API remain legacy;
their migration is separate from the Vue component.

Validation: seven Vue templates compiled, JavaScript syntax checks passed, and all
48 referenced concepts/addresses resolved to installed SVG sources. The targeted
MediaManager Debug build passed with zero warnings and errors. Browser rendering
and real upload interactions have not been verified.

## DataGrid Vue icons

Built-in grid, pager and tools icons use `sm-icon` with concept names. Column
header Icon values now accept concepts or full addresses; FA class strings must
be migrated (for example, `fa fa-paperclip` becomes `paperclip`).
Boolean cells use check/minus SVGs with the existing state colors and no font
pseudo-glyphs; null values keep an empty slot. The detail toggle retains its CSS
rotation, and refresh uses `icon-spin` while busy.

The legacy `bootstrap-icon` Vue component, its bundle inclusion, DataGrid
registration and `iconSpriteUrl` option have been removed. External display
templates must use `sm-icon` with a concept or explicit address instead.

## TabStrip icons

`TabTagHelper.Icon` (`sm-icon`) accepts concepts and qualified addresses.
The parent `TabStripTagHelper` renders them asynchronously through `Html.IconAsync`.
`IconClass` (`sm-icon-class`) is preserved, including values supplied by
`TabFactory`. Stacked tabs use `icon-fw`; empty slots retain their width without
FA markup. Image-based tabs retain their existing rendering.
Line-tab icons follow the link text color through currentColor. Stacked line-tab
icons retain the shared icon sizing (including its 16px minimum) and hover scaling
without muted inactive icon colors. The legacy nav font-size inheritance excludes
.icon so it cannot override the shared minimum or size modifiers.

Template inputs and TabStrip extensions in the public, enterprise, internal and
customer repositories were audited. Known FA/BI names now use reviewed concepts.
The following unmatched drawings retain explicit addresses with mapping bypassed:

- `bi:building-check!`
- `bi:calendar-event!`
- `bi:car-front!`
- `bi:currency-dollar!`
- `bi:menu-down!`
- `bi:phone-vibrate!`
- `bi:shield-check!`
- `fa:calendar-days!/r`
- `fa:ruler-horizontal!/s`
- `fa:section!/r`

All ten sources exist in the installed libraries. DimensionPricing's embedded
`fa-lg fa-fw` classes became `IconClass("icon-lg icon-fw")`.
PageBuilder block metadata still supplies FA classes to other editor surfaces;
its tab input alone converts those dynamic values to explicit FA addresses.

Validation: the Debug platform build passed with zero warnings and errors.
Three rendering regression cases passed for a concept, a concrete address and
an unavailable icon, including class forwarding and empty stacked slots.
Private module builds and browser appearance were not verified in this step.

## Testing a partially mapped library

A non-default library can be tried before its mapping is complete. Resolution tries
its mapped target (or the concept name when unmapped), then its configured variant
fallbacks. With `fallbackToDefaultLibrary` enabled in `config.json`, unresolved
concepts are mapped again in the system default library. The shipped configuration
enables this and links FA regular and solid as mutual variant fallbacks.

Keep conceptual names in migrated code so this works across libraries. A `!` address
skips mapping and library fallback, but still permits configured variant fallbacks.
`data-icon` and library/variant CSS classes identify the actual resolved artwork,
which can differ from the requested variant. Kit sprites and client identity patches
follow the same server-side resolution. Library browsing shows only native icons.
See [README.md](README.md#resolve-missing-icons) for the configuration and precedence.

## Font Awesome to HugeIcons

| Source FA icon classes | Target | Concept |
| --- | --- | --- |
| `fal fa-address-book` | `address-book` | `address-book` |
| `fa fa-exclamation-circle`, `fa-circle-exclamation` | `alert-danger` | `sys:alert-danger!/outline` |
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
| `fa fa-bold` | `bold` | `bold` |
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
| `fa fa-times`<br>`fal fa-times`<br>`fas fa-times`<br>`fa fa-xmark` | `cancel-01` | `close` |
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
| `far fa-file-word` | `file-02`<br>Approximation: generic document; W detail is missing. | `file-word` |
| `fas fa-film` | `film-01` | `film` |
| `fa fa-filter` | `filter` | `filter` |
| `fa fa-fire` | `fire` | `fire` |
| `far fa-folder`<br>`fas fa-folder` | `folder-01` | `folder` |
| `fa fa-folder-open`<br>`fas fa-folder-open` | `folder-open` | `folder-open` |
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
| `fa fa-link`<br>`fal fa-link` | `link` | `link` |
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
| `fa fa-expand` | `maximize-screen` | `maximize` |
| `fa fa-bullhorn` | `megaphone-02` | `megaphone` |
| `fa fa-compress` | `minimize-screen` | `minimize` |
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
| `fa fa-pencil`<br>`fas fa-pencil` | `pen-01` | `pen` |
| `fal fa-phone`<br>`fas fa-phone-flip` | `bi:telephone` | `phone` |
| `fa fa-eye-dropper` | `dropper` | `dropper` |
| `fa fa-caret-right`<br>`fa fa-play` | `play` | `play` |
| `far fa-play-circle` | `play-circle-02` | `play-circle` |
| `fa fa-plus` | `plus?stroke-scale=1.5` | `plus` |
| `fa fa-plus-circle` | `bi:plus-circle` | `plus-circle` |
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
| `fa fa-sitemap` | `bi:diagram-3` | `nodes` |
| `fa fa-sliders-h` | `sliders-horizontal` | `sliders` |
| `fa fa-spinner`<br>`fas fa-spinner` | `loader-circle` | `spinner` |
| `far fa-square`<br>`fa fa-stop`<br>`fas fa-stop` | `square` | `square` |
| `fa fa-star` | `star` | `star` |
| `fa fa-star-half` | `star-half` | `star-half` |
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
| `fa fa-unlink`<br>`fas fa-unlink` | `link-off` | `unlink` |
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

Source names refer to the legacy Bootstrap TagHelper or sprite, including original
`fill` tokens. The imported BI library preserves all upstream names unchanged in its
single `default` variant, including both terminal `-fill` and interior `-fill-` names.
Use `bi:alarm-fill/default`, for example. The former synthetic `light` and `solid`
variants no longer exist. See the [BI import recipe](bootstrap/README.md).

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
| `chevron-left?stroke-scale=1.25` | `chevron-left` | `chevron-left` |
| `chevron-right?stroke-scale=1.25` | `chevron-right` | `chevron-right` |
| `chevron-double-left` | `arrow-left-double?stroke-scale=1.25` | `chevrons-left` |
| `chevron-double-right` | `arrow-right-double?stroke-scale=1.25` | `chevrons-right` |
| `circle-fill` | `circle` | `circle` |
| `clipboard` | `clipboard-check` | `clipboard` |
| `clock` | `clock-04` | `clock` |
| `x`<br>`x-lg` | `cancel-01` | `close` |
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
| `eraser` | `eraser` | `eraser` |
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
| `link-45deg` | `link` | `link` |
| `list-ul`<br>`view-list` | `left-to-right-list-dash` | `list` |
| `geo-alt` | `location-01` | `location` |
| `box-arrow-in-right` | `login-02` | `login` |
| `box-arrow-left` | `logout-02` | `logout` |
| `megaphone` | `megaphone-02` | `megaphone` |
| `menu-button-fill` | `dropdown-field-type` | `menu-button` |
| `palette` | `paint-board` | `palette` |
| `vector-pen` | `pen-tool-03` | `pen-nib` |
| `pencil`<br>`pencil-fill` | `pen-01` | `pen` |
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
| `diagram-3` | `bi:diagram-3` | `nodes` |
| `sliders` | `sliders-horizontal` | `sliders` |
| `sliders2-vertical` | `sliders-vertical` | `sliders-v` |
| `sort-down-alt` | `sorting-02` | `sort-down` |
| `sort-numeric-down` | `sorting-1-9` | `sort-numeric` |
| `sort-up-alt` | `arrow-up-narrow-wide` | `sort-up` |
| `stars-tricolor` | `stars` | `stars` |
| `star-half` | `star-half` | `star-half` |
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

## Presentation and special cases

- Preserve IDs, accessible labels, titles and application-specific classes. Pass
  attributes through `IconOptions.Attributes` in the HTML helper.
- Replace `fa-fw` or `bi-fw` with `fw="true"` or `icon-fw`. Use `icon-*` sizing
  utilities and the shared animation/transform options documented in [README.md](README.md).
- Preserve explicit flips and rotations; they may convey direction independently
  of the selected drawing. Mapping modifiers and explicit presentation overrides
  have different roles. Review the resulting orientation.
- Do not mechanically replace a child inside a legacy Bootstrap stack. Migrate the
  entire composition deliberately to `icon-stack`, checking fill, scale and offsets.
- Dynamic names from menus, plugin models and client templates need migration at
  their producers. Replacing only the rendering tag is insufficient if it still
  receives old FA class strings or BI-specific names.
- Client code is a separate migration step. Use the supported `sm-icon` and
  `sm-icon-stack` web components where appropriate; do not emit server TagHelper
  markup from JavaScript and expect it to execute in the browser.
- `stars-tricolor` uses the shared concept pinned to `sys:stars-tricolor`. Keep its
  multicolor rendering; the table's monochrome HI `sparkles` alternative is not an
  approved automatic replacement.
- The legacy synchronous icon helpers have been removed. Use `await Html.IconAsync(...)`.

## Kits and plugin integration

### Brand logos use Font Awesome

The `brands` kit selects `fontawesome-free` / `brands`, overriding the global HI
default for its concepts. The HI columns above still document HI equivalents;
brand concepts in this kit now resolve to the original FA logos unless callers
explicitly select another library.

The kit declares `alexa`, `anthropic`, `apple-music`, `bing` and `microsoft-teams`
in `icons`, with concrete BI/light addresses in its `sources` object.
These explicit BI selections share the same generated sprite as the FA members.
Use the bare concepts (`alexa`, `anthropic`, `apple-music`, `bing`, `microsoft-teams`)
in views, or their full addresses when BI must be selected explicitly. Source
qualifiers override kit defaults; explicit caller selectors remain authoritative.

When customizing a kit, keep every concept in `icons`. Add only exceptional concrete
addresses to `sources`; source keys outside the member list are errors. Source target
names are not passed through library mapping. Renaming a target never changes the
public concept or symbol ID. The browser references concepts directly:
`alexa` uses `brands-<revision>.svg#alexa`. Compact per-kit identity defaults and
`sources` patches also supply canonical `data-icon` and library classes, matching
server-rendered output. These patches only describe the resolved identity; they
never change the symbol reference. Explicit addresses, modifiers and concepts absent
from the manifest go to the render endpoint. Plugins do not need library mappings
or source resolution in JavaScript.

Existing concepts moved into this kit: `css3`, `facebook`, `github`, `google`,
`html5`, `instagram`, `linkedin`, `twitter-x` and `youtube`.
Use `twitter-x` for the existing concept mapped to FA `x-twitter`.
Use `windows-logo` for the Microsoft Windows logo (FA `windows`). The existing
`windows` concept continues to mean overlapping application windows and keeps its
FA `window-restore` mapping; do not use it for the brand logo.

Browser logo concepts use the `browser-` prefix while retaining their FA sources:

| FA source | Concept |
| --- | --- |
| `chrome` | `browser-chrome` |
| `edge` | `browser-edge` |
| `firefox-browser` | `browser-firefox` |
| `firefox` | `browser-firefox-alt` |
| `safari` | `browser-safari` |

Square logo concepts use a `-square` suffix, for example `facebook-square`
maps to FA `square-facebook` and `twitter-x-square` maps to
`square-x-twitter`. Keep this suffix convention when adding further square logos.

The supplied kit deliberately excludes competing shop/CMS platforms and keeps
a curated social selection. Tumblr remains included. Removing a logo from the
kit does not remove it from the FA library; it remains available through an
explicit library and variant selection.

Other kit members use their FA brand names directly, for example `paypal`,
`cc-visa`, `amazon` or `dhl`. Explicitly selecting a library or variant still
takes precedence over these kit defaults.


Rendering automatically selects kit references or inline SVG. Plugin callers do not
need to decide which form to output. Kit definitions live in `kits.json`; mappings
live per library. Plugin icons rendered through the shared foundation participate
in the same SVG cache and can later be inspected through backend maintenance.

The tables describe migration choices, not a promise that every listed call site
has already been changed. External plugins must migrate their own views, producers
of dynamic icon names and client rendering, then visually verify the result.

See [README.md](README.md) for addressing, mapping modifiers, styling, kits,
customization layers and the planned backend maintenance tools.

## Icon editor toolbar concepts

The Backend kit includes these HI mappings for the icon editor toolbar:

| Concept | HI source |
| --- | --- |
| `flip-h` | `flip-horizontal-01` |
| `flip-v` | `flip-vertical-01` |
| `rotate` | `rotate-top-right?rotate=15` |
| `upscale` | `image-upscale` |
| `animation` | `ease-out-control-point` |

## Reviewing icon addresses

The migration table preview field accepts full addresses such as `bi:telephone`,
`fa:phone!/solid` or `hi:rotate-top-right!/sr?rotate=15`. Unqualified names
remain literal HugeIcons stroke-rounded proposals. Qualified addresses use normal
icon resolution. The optional HI font comparison is hidden for other libraries,
query modifiers or unavailable font glyphs; it never blocks a valid SVG preview.

Admin sitemap root nodes now use SVG concepts; the legacy menu rendering branch has been removed.


## RTL concept policy

`config.json` now owns a kit-independent `mirrorInRtl` array. Server rendering and
manifest schema 8 apply `icon-mirror-rtl` automatically to listed, unqualified
concepts. The initial policy covers left/right arrows and single/double chevrons.
Explicit source selections remain unchanged. CSS follows the element's writing
direction, so sprite artwork and cache references stay shared between LTR and RTL.
See [Direction-sensitive concepts](README.md#direction-sensitive-concepts).


## Mapping stroke scales in kit sprites

Mapping-level `stroke-scale` is now baked into kit artwork, included in manifest
memberships and available in IconBrowser sprite previews. Per-call values differing
from the mapping still render inline. Sprite revisions include the stroke scale;
existing immutable files remain untouched.


## Product comparison concept

The Shared kit includes `compare`, mapped to HI `repost`. Product-detail
`.action-compare` uses this concept; `repeat` remains available for repetition.


## Slash variant separator

Icon addresses now use `[library:]name[!][/variant]`, for example `hi:cart!/sr`.
The former at-sign separator is rejected; there is no compatibility parser.
Manifest schema 8 publishes slash-qualified identities. Razor attributes can use
plain literal addresses without an explicit string expression or doubled at-sign.

## Category path arrow-turn-up mask

The expanded category path now uses `icon-mask("arrow-turn-up", 1rem)` instead of
the FA webfont glyph. This uses the existing Shared kit concept and its library mappings.

## Sass pseudo-element migration

FA mixin consumers now use SVG masks across shared/admin styles, Flex, ContentSlider,
MediaManager and the HP theme. Frequent chevrons, plus and check reuse curated CSS
variables. Angle directions use chevron concepts; state and RTL overrides change
masks instead of Unicode glyphs. The offcanvas loading state uses `spinner`.

`maximize` and `minimize` moved from Backend to Shared for PhotoSwipe fullscreen
controls. PhotoSwipe's close button uses the `close` icon TagHelper.

Star-rating uses `star-outline` and `star-fill` masks with the existing size,
hover and checked-state settings. Both legacy `fontawesome` and
`fontawesome-font` mixins have been removed. Alert motifs use
`--alert-icon-mask`: the pinned shared concepts `alert-info`, `alert-danger`,
`alert-success` and `alert-warning`, with `award` for review rewards.
The four alert SVGs live in the system library and keep their appearance across
library changes. Info is unframed; success is a heavier HI check;
warning uses an HI-derived outline triangle and danger an outline circle, both with the same filled exclamation mark.
All use a 24-unit viewBox and the existing 28px alert viewport. The shared icon-box mixin renders masks only when
alert icons are enabled.

NumberInput steppers render kit SVGs server-side through `IIconRenderer`: default
controls use `chevron-down`/`chevron-up` at 1rem, centered controls use `minus`/`plus`.

## Entity picker icon addresses

`EntityPickerTagHelper.Icon` (`icon`) replaces `IconCssClass` (`icon-css-class`).
The default is `search`; an empty address suppresses the icon. Rendering uses
`HtmlHelper.IconAsync`, including kit sprites and missing-icon fallback. Explicit
callers use `plus`, `link`, or `search` concepts instead of FA classes.


## Server-rendered icons in C# helpers

Media thumbnails, cover-image controls, file icons, pagination, theme inheritance
badges, external login buttons, forum profile actions and StorageDisplay indicators
now render SVGs on the server instead of emitting `sm-icon` hosts. Back links also
render server-side and use size `lg`.

SmartLabel and hint tooltip helpers intentionally remain synchronous and emit
`sm-icon` for the tooltip icon, avoiding async changes throughout label callers.
`ThemeVarLabel` also remains synchronous; callers await `ThemeVarChainInfoAsync`.
Media delegation uses `ProcessAsync`; async media renderers override
`ProcessMediaAsync`. Pagination overrides use `AppendItemAsync` and
`GetItemInnerContentAsync`; cover-image overrides use `CreateDropdownAsync`
and `CreateDropdownItemAsync`. External authentication components use `InvokeAsync`,
and the forum profile filter implements `IAsyncResultFilter`.

PNotify uses the custom smartstore styling preset: status icons reuse alert masks;
close uses the close mask, and pin/unpin retain the pause/play motifs as SVG masks.
The HP theme inherits this preset. Vendor scripts remain unchanged.

Legacy Font Awesome styles now always use the bundled Free webfonts. The Pro theme
switch, hosted CSS kit loading, unused thin shim and group/overlay styles were removed.
Light/regular fallbacks and the legacy chooser's duotone shim remain until their
callers are migrated. Pro SVG libraries use library replacement independently.

## MediaManager kit icons

The hidden admin kit now owns file-archive, file-code, file-csv, file-presentation,
file-spreadsheet and file-word (moved from shared). Existing callers keep their
concept names. Shared file, file-text, file-pdf, file-image, file-audio and file-video
remain shared for frontend and common thumbnail consumers. General folder concepts
and folder-transfer retain their previous memberships. No separate media kit exists.

MediaManager uses media-folder/media-folder-open (explicit HI outlines),
media-folder-fill/media-folder-open-fill (System artwork) and media-overlay-*
(explicit FA Solid) for product, category, download, mail and user overlays.
The media library node uses images. Folder stacks retain outline backgrounds.
These concepts all belong to admin; normal file hints reuse their existing kits.
The admin view-tiles concept maps to HI gallery-vertical, BI view-list and FA
table-list. MediaManager no longer registers the legacy Bootstrap Vue icon plugin
or emits its sprite URL.

MediaEditorCommand now exposes Icon (concept name or address) instead of IconClass
and IconLibrary. Its Razor template renders icons server-side with the icon
TagHelper. The AI commands use stars-tricolor from the shared collection. IconUrl retains
precedence for image-based providers such as Pixlr. External providers must migrate
their command initializers to Icon; legacy CSS-class properties have been removed.

## Backend navigation

Admin sitemap root nodes now use home, catalog, chart-up, customers, marketing,
cms, sliders-v, database and puzzle. The temporary template mapping
has been removed. Main navigation and right-hand toolbar icons render at 24px
with stroke-scale 0.85; toolbar concepts are gear, earth, question-circle and user-circle.

The admin kit pins catalog to hi:delivery-box-01!/sr and customers to
hi:user-group-03!/sr. The cms concept pins hi:layout-dashboard!/sr, retaining the dashboard motif.
The marketing concept pins hi:megaphone-02!/sr, retaining the megaphone motif.
The sitemap uses these concepts instead of explicit sources.

## Block metadata icons

BlockAttribute.Icon now contains a concept or explicit icon address instead of FA CSS
classes. All block handlers, including private modules and the module template, use
this contract. PageBuilder renders palette and dialog icons on the server; the
dynamic block list assigns addresses to sm-icon. Icons without a concept use default-library motif names and do not require
additional kit entries. No BlockAttribute retains an explicit FA address.

The audio block uses the shared `audio` concept (HI `audio-lines`, FA `music`,
BI `soundwave`). The text block uses `text-firstline-right`; the default block
icon is `block-game`.

The story block uses `cms`. The video block uses the shared `video` concept, mapped
to HI `film-01` and FA/BI `film`.

## Icon provider foundation

Installed modules can implement `IIconProvider.Configure(IconKitBuilder)` to
extend kits or collections and declare new kits using existing library sources.
Local JSON keeps priority. Provider concepts use the existing sprite and manifest
pipeline. Providers are auto-discovered and instantiated through `ResolveUnregistered`
in a temporary scope for each catalog generation, without singleton registrations.
AddToAny contributes all its additional service-picker icons exclusively to the admin kit.


## Module-owned icon contributions

Module providers live in each module root as `Icons.cs` (`internal sealed class Icons`).
They extend the existing area kits; module-only concepts are no longer unconditional
members of the core admin kit or shared collection. Global library mappings remain
available so source-less contributions can follow the selected kit library and variant.

Removed the nine MediaManager-specific folder and overlay concepts from the core memberships.
Generic motifs remain in core kits and shared even if only one module currently uses them;
a single caller is not enough to classify an icon as module-owned. The obsolete `caret-down`
entry was also removed; DimensionPricing now uses `chevron-down`. File-type concepts
remain in the core because FileIconTagHelper, ThumbnailTagHelper and other platform
callers use them independently of MediaManager.

| Module | Provider target | Contributed concepts |
|---|---|---|
| Smartstore.MediaManager | admin kit | `media-folder`, `media-folder-open`, `media-folder-fill`, `media-folder-open-fill`, `media-overlay-product`, `media-overlay-category`, `media-overlay-download`, `media-overlay-mail`, `media-overlay-user` |
| Smartstore.PageBuilder | admin kit | `gallery-thumbnails`, `text-firstline-right`, `bug-01`, `block-game` |
| Smartstore.Forums | public kit | `forum-subscriptions`, `forum-messages` |
| Smartstore.Polls | admin kit | `list-checks` |
| Smartstore.AdditionalBlocks | admin kit | `advertisement` |
| Smartstore.ContentSlider | admin kit | `content-slider` |
| Smartstore.EmailReminder | public kit | `email-reminder` |
| Smartstore.SearchLog | admin kit | `search-log` |
| Smartstore.UrlRewriter | admin kit | `url-rewriter` |
| Smartstore.MegaMenu | admin kit | `mega-menu` |
| Smartstore.DependingPrices | admin kit | `depending-prices` |
| Smartstore.Warranty | admin kit | `warranty` |
| Smartstore.GPSR | admin kit | `gpsr` |
| Smartstore.Timeline | admin kit | `timeline` |
| Smartstore.GeoConfigurator | admin kit | `geo-configurator` |
| Smartstore.LinkMobility | admin kit | `sms-notifications` |
| Smartstore.StorageDisplay | admin kit | `storage-display` |
| Smartstore.Tyre24 | admin kit | `tyre24` |
| Smartstore.UmamiAnalytics | admin kit | `traffic-chart` |
| Smartstore.TopM.R6Connector | admin kit | `r6-connector` |
| Smartstore.SupportAgent | admin kit | `support-agent`, `support-import`, `support-history` |
| Smartstore.ShopwareImport | admin kit | `import-history` |
| Smartstore.DevTools | admin kit | `devtools-server`, `terminal` |
| Smartstore.CartApproval | public kit | `cart-approved` |
| Smartstore.CartApprovalWithBudget | public kit | `cart-approved` |
| Smartstore.EventFlow | shared collection | `event-flow`, `ticket-scan`, `ticket-holder`, `ticket-valid` |
| Smartstore.Homepage | public kit | `feature-catalog`, `feature-payment`, `feature-shipping`, `feature-security`, `feature-performance`, `chart-bar`, `discount`, `user-group`, `megaphone`, `nodes`, `globe`, `images`, `chart-up`, `palette` |
| Smartstore.Themes.HP | public kit | `feature-catalog`, `feature-payment`, `feature-shipping`, `feature-security`, `feature-performance`, `chart-bar`, `discount`, `user-group`, `megaphone`, `nodes`, `globe`, `images`, `chart-up`, `palette` |
| Smartstore.Themes.HP | admin kit | `hp-home`, `hp-features` |
| Smartstore.TyreCommerce | admin kit | `tire` |
| Smartstore.TyreCommerce | public kit | `tyre-summer`, `tyre-winter`, `tyre-all-season`, `tyre-offroad`, `tyre-season-unknown`, `tyre-efficiency`, `tyre-wet-grip`, `tyre-noise` |

New motif selections prefer HugeIcons. UrlRewriter, MegaMenu, StorageDisplay and the
HP administration menu retain their original BI sources pending visual review.
MediaManager retains its deliberately filled FA overlays and system folder drawings.
Homepage and HP contribute identical feature concepts; duplicate identical contributions
are idempotent. Public feature lists also contribute the general admin concepts they use
to the public kit. EventFlow contributes to shared because its concepts serve both areas.

Callers use concept names. TyreCommerce's legacy Html.Icon calls now use the SVG icon
TagHelper. Remaining Bootstrap icon tags for filter, trash, check and stars-tricolor in
the inspected modules were replaced with the corresponding existing concepts.

The DataGrid search reset uses the admin kit concept `filter-remove`, mapped to
the HugeIcons motif of the same name, instead of `eraser`.

### Brands kit curation

Keep major platform and AI brands, payment/shipping logos, and the curated technical
logos bootstrap, css, css3, css3-alt, git and markdown in core. Removed memberships
retain their library sources and mappings. PageBuilder contributes soundcloud to
the admin kit.

Removed fixed brands memberships: `airbnb`, `browser-chrome`, `browser-edge`, `browser-firefox`, `browser-firefox-alt`, `browser-safari`, `dart-lang`, `docker`, `facebook-square`, `font-awesome`, `github-square`, `gitlab-square`, `imdb`, `instagram-square`, `java`, `js`, `kickstarter`, `ko-fi`, `kubernetes`, `linkedin-square`, `node-js`, `npm`, `php`, `pinterest-square`, `playstation`, `postgresql`, `python`, `react`, `reddit-square`, `sass`, `soundcloud`, `steam`, `swift`, `threads-square`, `twitter-square`, `twitter-x-square`, `typescript`, `uber`, `vimeo-square`, `vuejs`, `whatsapp-square`, `xing-square`, `youtube-square`, `zoom`.

### AI command menu

The platform AI menu generator renders admin kit concepts server-side through
HtmlHelper.IconAsync. Text menu entry points and their callers are asynchronous. Generate: ai-auto-rotate; style: ai-editing;
tone: wink; summarize: reduce-paragraph; improve: ai-magic; simplify: menu-08;
extend: expand-paragraph; organize: layout-list; continue: arrow-big-right-dash.
The AI module provider contributes the menu concepts to admin; the generic wink
concept remains in core. Source declarations live in the module provider rather
than global mappings. Kit sprites avoid individual icon rendering requests.

AI dialogs reuse ai-editing for style/prompt editing and ai-auto-rotate for
regeneration. The optimization menu button retains sliders. The module additionally contributes ai-image for
image generation and ai-idea for reasoning effort. General actions retain core
concepts. The mock provider renders its file-image placeholder server-side once
per rich-text answer and reuses that markup for each paragraph.

### Summernote adapter

Override all built-in icon slots and the AI extension in Smartstore's Summernote
configuration with kit-backed sm-icon markup, except the save slot, whose original
disk SVG is preserved directly in globalinit.js. Restore the original note-icon font defaults in the fork. Move bold, italic and underline from admin to shared and add strikethrough,
subscript, superscript, text alignment, indentation, ordered/unordered lists and code
there. Add the admin-only htmledit kit for 18 editor-specific concepts. Shared and
existing admin concepts are reused rather than duplicated in htmledit.

Smartstore-specific icon styling lives in wwwroot/skinning/_summernote.scss,
imported directly after the vendor stylesheet. Dropdown checks support sm-icon and
SVG icons; chevrons use 13px and stroke-scale 1.4. The vendor Sass stays unchanged. The fork now routes the color picker through colorReset/colorPicker and reuses
css/menuCheck in CssClass/TableStyles. Smartstore overrides all four slots. The
InlineEmoji fixed-width wrapper no longer depends on FA. Embedded SVG defaults have been removed; additional fork slots use font-based fallbacks. The rebuilt editor JS bundle and source map are deployed in
Smartstore; vendor Sass was not copied.

The HI row-insert and column-insert drawings now have local overrides with one
solid and one dashed block, preserving the middle arrow. Existing htmledit mappings
use the solid new bottom/right block directly and mirror it for above/before.
The existing top/left block is dashed.

The forum BBEditor toolbar now uses shared kit concepts through sm-icon for bold,
italic, underline, link, code and quotes. Move link from admin to shared so every
toolbar icon is available in the public sprite. BBCode insertion remains unchanged.

The rebuilt Summernote JS bundle uses note-icon font defaults instead of embedded
SVG drawings. Smartstore still overrides every slot, preserving its original save
disk SVG exclusively in globalinit.js. The delivered JS shrank by 30,775 bytes
(11,183 bytes with gzip).

The standalone Summernote demo font now uses outlined copies of the current editor
icon sources, including transforms and dashed table insertion blocks. Existing
glyph names/codepoints are preserved; extra fork slots have dedicated glyphs.
The fork records source addresses and stable codepoints and versions font URLs by
content hash. Its font CSS explicitly uses weight 400 and disables font synthesis.
Smartstore still renders SVG icons and does not import this font.

### Module and provider representations

- Added optional manifest `Icon`/`IconColor`, the `ModuleIcon` value and server-side
  `module-icon` helper. Module/provider lists, configuration headers, payment,
  export, CAPTCHA and AI provider choices use the resolved representation.
- Image compatibility remains: provider-specific SVG/PNG first, then manifest
  address, module SVG/PNG and `hi:plug-02`. `GetIconUrl()` retains its legacy group/default
  image fallback and remains an image
  URL API and now prefers SVG to PNG. Image files are not recolored by `IconColor`.

- Migrated 23 module manifests to explicit HI/system icon addresses and saturated
  colors (official brands use their brand colors; system stars retain their own colors).
  Removed 22 unused module icon.png files; AI also uses sys:stars-tricolor as its controller fallback. The four
  rule/filter entries in the admin sitemap now use hi:filter-edit.

- Added shared return concept (HI delivery-return-01, BI box-arrow-left, FA rotate-left).
  Withdrawal and return-case menus, headings and actions use this concept.

- Migrated 15 further module manifests (Gemini, search/media/security, authentication,
  tax/shipping and payment modules); removed 11 unused icon.png files. Provider-specific
  payment images remain. TinyImage admin navigation uses hi:image-down. Added admin
  tax concept: HI taxes, BI receipt, FA file-invoice-dollar.

- DevTools configuration now includes an Icons group with the existing IconBrowser
  editor, all kits and provider indicators. The migration
  cheatsheet remains unchanged.

- DevTools shows a read-only grid of cached inline icon drawings, deduplicated by
  address. The grid reads existing payloads without populating or refreshing the cache.

- Cache diagnostics now exclude source addresses covered by configured kit variants
  across all areas, including collections and providers. Added behance/ello to brands,
  migrated their footer references, and moved privacy to the shared collection.

- Added system file-stack and folder-stack drawings from HI file-edit and
  folder-add, retaining their lower-right gaps without the overlay paths.

- MediaManager tree uses shared folder-stack/folder-open-stack for folders with
  overlays, with HI stroke-rounded overlays positioned inside the lower-right gap.
  Existing base/overlay colors and plain filled folders remain unchanged.

## Module icon colors as address modifiers

- Replaced module `IconColor` and `ModuleIcon.Color` with `?color=...` in all module manifests.
- SVG rendering honors address colors; explicit presentation colors take precedence.
- Colors do not change sprite selection or source cache identity. The client strips
  color before manifest lookup and drawing requests, then applies it to the host.
- Library mapping colors are rejected because they are rendering-only values.

## Optional kit display names

- Kits accept `displayName` in JSON and provider definitions.
- Icon Browser and DevTools use the label, falling back to the unchanged kit name.
- Removed kit-name titleizing; core kits declare explicit display labels.

## Select2 choice icons

- Added select-level icon defaults for tags, stores, customer roles, discounts,
  rule sets, widget zones, delivery times and email recipients. Shared templates
  and direct selections use existing kit concepts, including dynamic choices.
- Option-specific icons and images retain precedence; default icons preserve
  existing item actions. No changes to vendor Select2 code.

- Added admin concept `customer-roles` mapped to HI `user-multiple-02` for all
  customer-role selections, including grid editors. The main-menu `customers`
  concept retains its existing artwork.

- Added admin concept `seo` (HI `seo`) and applied it to all SEO tabs
  in platform and module Razor views, including tabs without a previous icon.

- MegaMenu tabs in category and menu-item editing now reuse `menu-button`, matching
  the module menu. Removed the unused `mega-menu` concept and its icon provider.

- LinkBuilder metadata now carries icon addresses instead of FA classes. Core, Blog
  and News sources use kit concepts; the source selector renders SVGs server-side
  and clones them into its button. Updated query/target state selectors for SVGs.

- PageBuilder target rows reuse server-rendered icons from the Add Target dropdown.
  Source concepts match LinkBuilder; removed the duplicated FA-class mapping in JS.

- Moved the SVG foundation to Smartstore/IconKit (namespace Smartstore.IconKit). Added AddIconKit options for source/cache paths and route prefix, a dedicated IconKitStarter, and MapIconKit endpoints replacing IconController. Source data remains in App_Data/Icons; module providers, Razor imports and tests use the new namespace.

- Providers can contribute private drawings with AddSvg and AddSvgFile on kits or collections. File arguments use ~/ application paths and are read lazily without file watching. Existing SVG parsing, cache revisions, sprites and provider indicators are reused; no public library or stroke-scaling option is added.

## Checkout progress icons

- Replaced font-based pseudo-elements with server-rendered kit icons: cart, home,
  truck, creditcard, thumbs-up and check. Icons are 24px and hidden below sm;
  existing progress circles and state colors are preserved.
- Moved creditcard from admin into shared for both public and admin sprites.

- Password recovery now renders mail-open through the icon TagHelper instead of
  the legacy icon font. Moved mail-open from admin into shared for public sprite availability.

- Installation uses explicit HI stroke-rounded addresses with mapping bypass in
  server-rendered icon TagHelpers. Removed its legacy icon stylesheet; the existing
  FA password visibility toggle and its stylesheet remain unchanged.

- Installation explicitly enables IconTagHelper and AttributesTagHelper in its
  restricted Razor imports. All install icons force inline rendering to avoid
  sprite requests being redirected by the installation middleware.

- Removed the unused legacy icon font assets, bundle/report imports, Sass utilities,
  mixin and project exclusion after migrating all remaining callers to IconKit.

- Product actions now use icon addresses exclusively. Sample downloads use download;
  LinkMobility uses mobile with the shared product-action sizing. Removed the legacy
  ActionItemModel.IconCssClass property and Razor fallback.

- Media preview hints expose IconKit addresses only. Preview rendering, Dropzone
  placeholders and FileManager consumers use sm-icon; getPreview accepts iconSize
  instead of FA-specific iconCssClasses. Existing type colors and size factors remain.

- ConfirmTagHelper and ConfirmModel use Icon addresses instead of IconClass. The
  dialog renders server-side SVGs with existing theme colors and size utilities.
  Defaults are alert-circle and trash; Campaign and PersonalPromo use question-circle
  and rotate-right. The independent client-side confirm2 API remains unchanged.

- TreeItem.Icon and data-icon replace IconClass/data-icon-class. The tree renders
  sm-icon and accepts defaultCollapsedIcon/defaultExpandedIcon and
  collapsedIcon/expandedIcon addresses. Default chevrons retain their rotation
  animation; custom state icons switch names. Category folders use the folder/folder-open kit
  concepts and retain the success color. Image URL support is unchanged.

- sm-icon renders in the light DOM again and shares generated icon classes with
  server-rendered icons. Removed CSS custom-state selectors and Shadow CSS.
  Accessibility defaults still use ElementInternals without generating ARIA attributes.
- Components retain inert authored source nodes and expose getSourceNode(). External
  attribute/class/style changes are synchronized; generated rendering is excluded.
  Summernote uses a configurable element-name list and serialization callback for
  code view, saving and undo/redo. Inspect drawings through the host's children.

- Summernote uses `editor-color` from the `htmledit` kit (`bi:pencil-fill`) for its
  colorable toolbar icon. The generic `paint-brush` concept uses the library mapping
  (`hi:paintbrush`) without an admin-kit source override.

The storefront toolbar language selector uses `globe` (`globe-02` in Hugeicons). The concept now belongs to the shared collection instead of the admin kit, making it available in both areas.

The storefront toolbar currency selector uses `curreny` (`coins-02` in Hugeicons). The concept belongs to the shared collection and is available in both areas.

The admin kit includes `sync` mapped to `hi:reload`, used by the theme list reload button.

## Address presentation and DevTools code examples

- Expressions now accept `size` and `animation`, with the same presets as markup attributes.
- Explicit presentation options override expression values without changing source cache or sprite identity.
- DevTools provides copyable TagHelper, Web Component and expression output for the current Icon Browser selection.
