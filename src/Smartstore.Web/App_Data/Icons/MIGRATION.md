# Migrating legacy icons to Smartstore icons

This is the maintained migration reference for developers, coding agents and plugin
vendors. Plugins maintained outside this workspace can use the same replacements.
The current system default is HugeIcons (`hi`), variant `stroke-rounded` (`sr`).
These tables contain names and mappings only; they do not depend on a diagnostic
Razor view, generated SVG output or temporary import reports.

## Keep this reference current

Update this file as part of every agreed migration change: new source icons,
corrected mappings, presentation rules, exceptions and helper replacements.
Preserve reviewed decisions when importing new library versions. Do not regenerate
this document blindly or treat a temporary comparison page as its source of truth.

- `hugeicons/mapping.json` is authoritative for current concept-to-HI targets.
- This file preserves the legacy FA/BI-to-concept relationships needed by integrators.
- When a concept or HI assignment changes, update every affected table row here.
- Record uncertain visual approximations and exceptions instead of implying exact equivalence.
- Keep this guide in English. Display names and search tags are not localized here.

## Use concepts in application code

The first column lists existing FA CSS classes or BI names. The second column gives
the actual HI target, and the third gives the concept to use with the new API. Multiple source
styles or aliases in a cell share the same destination. Repeated concept names
across the FA and BI tables are intentional.

Prefer the **concept** in views and plugins so a configured library can supply its
own mapping. The HI target documents the current default artwork; do not hard-code
it unless that specific library and drawing are intentional.

```cshtml
@* Before *@
<i class="far fa-trash-can"></i>
<bootstrap-icon name="trash" />

@* After: both become *@
<icon name="trash" />

@await Html.IconAsync("trash", options: new()
{
    Size = "2x",
    Attributes = { ["class"] = "mr-2", ["title"] = "Trash" }
})
```

`name="trash"` currently maps to HI `delete-02`. Query modifiers shown in an HI
target are already part of its mapping; do not repeat them on the caller.
For example, use `name="check"`, not the target's `stroke-scale` modifier again.
Explicit library selection uses `lib="hi"`; `variant="sr"` selects Stroke Rounded.

Do not transfer FA style classes (`fa`, `fas`, `far`, `fal`, `fab`) onto the new SVG.
They describe the old font renderer. A filled/outline or brand distinction may need
visual review because the default HI variant is stroke-based.

## My Account menu and menu extensions

The platform My Account menu and the extensions migrated in this workspace now
store concept names in `MenuItem.Icon`. Plugin integrations should likewise use,
for example, `Icon = "cart"` or `.Icon("cart")`, without FA style or utility classes.
Leave `IconLibrary` unset to follow configured kit/system defaults; set it only
when an explicit library is intended. The TagHelper treats empty `lib` and `variant`
attributes as omitted, including null model values bound by Razor.

The shared `ListGroup` menu template renders concept names through the `icon`
TagHelper inside a fixed-width `list-group-item-icon` wrapper. The wrapper is one
text line high (`1lh`) and centers the artwork with flex layout beside the first
line of multiline labels. It also preserves empty icon slots and wraps legacy FA
icons for configured menus and plugins that have not migrated. Other menu templates are not migrated by this
change; check the consumer before replacing their icon strings.

`address-book`, `truck-loading`, `user-secret` and `wallet` belong to the Frontend
kit. `address-book` resolves directly by name; the other three use the mappings
listed below. `unlock` moved from Backend to Shared because it is also used for
changing the customer password. The Wallet balance badge uses the `check` concept
through the Core icon service and renderer, including its configured stroke scale.
Badges containing HTML need explicit migration too; `MenuItem.Icon` does not cover them.

The Avatar menu entry uses the `avatar` concept from the Shared kit, following
the configured library and variant defaults without a pinned source. Library mappings:

| Library | Icon | Concept |
| --- | --- | --- |
| HugeIcons | `user-square` | `avatar` |
| Bootstrap Icons | `person-circle` | `avatar` |
| Font Awesome | `circle-user` | `avatar` |

The existing `user-circle` concept remains available for other uses.

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

| Source FA icon classes | HI target | Concept |
| --- | --- | --- |
| `fal fa-address-book` | `address-book` | `address-book` |
| `fa fa-exclamation-circle` | `alert-circle` | `alert-circle` |
| `fa fa-arrow-down` | `arrow-down-02` | `arrow-down` |
| `far fa-circle-down` | `circle-arrow-down-01` | `arrow-down-circle` |
| `fa fa-arrow-left` | `arrow-left-02` | `arrow-left` |
| `fas fa-arrow-right` | `arrow-right-02` | `arrow-right` |
| `fa fa-turn-up` | `arrow-turn-up` | `arrow-turn-up` |
| `fa fa-arrow-up` | `arrow-up-02` | `arrow-up` |
| `far fa-circle-up` | `circle-arrow-up-01` | `arrow-up-circle` |
| `fa fa-award` | `star-award-01` | `award` |
| `fa fa-ban`<br>`far fa-ban`<br>`fas fa-ban` | `ban` | `ban` |
| `far fa-chart-bar` | `chart-column` | `bar-chart` |
| `fa fa-barcode` | `barcode` | `barcode` |
| `fa fa-bars`<br>`fas fa-bars` | `menu-09` | `bars` |
| `far fa-bars-staggered` | `menu-02` | `bars-staggered` |
| `fa fa-shopping-basket` | `shopping-basket-01` | `basket` |
| `fal fa-bell`<br>`far fa-bell`<br>`fas fa-bell` | `bell` | `bell` |
| `fa fa-blog` | `rss`<br>Approximation: HI RSS omits the pen. | `blog` |
| `fa fa-bold` | `text-bold` | `bold` |
| `fa fa-bolt`<br>`far fa-bolt` | `zap` | `bolt` |
| `fa fa-book` | `book-01` | `book` |
| `fas fa-box-open` | `package-open` | `box-open` |
| `fal fa-brackets-curly` | `braces` | `braces` |
| `far fa-building` | `building-03` | `building` |
| `fa fa-calendar`<br>`far fa-calendar`<br>`far fa-calendar-days` | `calendar-03` | `calendar` |
| `far fa-calendar-check` | `calendar-check` | `calendar-check` |
| `fa fa-caret-down` | `arrow-down-01`<br>Approximation: HI uses a chevron instead of a filled triangle. | `caret-down` |
| `fa fa-shopping-cart`<br>`fal fa-shopping-cart`<br>`far fa-shopping-cart` | `shopping-cart-02` | `cart` |
| `fa fa-cart-arrow-down` | `shopping-cart-add-02` | `cart-add` |
| `fal fa-certificate`<br>`far fa-certificate` | `certificate-01` | `certificate` |
| `fa fa-check`<br>`far fa-check`<br>`fas fa-check` | `check?stroke-scale=1.5` | `check` |
| `fa fa-check-double` | `tick-double-02` | `check-double` |
| `fa fa-angle-down`<br>`fal fa-angle-down`<br>`fas fa-angle-down`<br>`fa fa-chevron-down`<br>`fas fa-chevron-down` | `chevron-down` | `chevron-down` |
| `fa fa-angle-left`<br>`fa fa-chevron-left`<br>`far fa-chevron-left` | `chevron-left` | `chevron-left` |
| `fa fa-angle-right`<br>`fa fa-chevron-right`<br>`far fa-chevron-right`<br>`fas fa-chevron-right` | `chevron-right` | `chevron-right` |
| `fa fa-angle-up`<br>`fas fa-angle-up`<br>`fa fa-chevron-up` | `chevron-up` | `chevron-up` |
| `fa fa-angle-double-down` | `arrow-down-double` | `chevrons-down` |
| `fa fa-angles-left` | `arrow-left-double` | `chevrons-left` |
| `fa fa-angles-right` | `arrow-right-double` | `chevrons-right` |
| `fa fa-angle-double-up` | `arrow-up-double` | `chevrons-up` |
| `fa fa-circle`<br>`fas fa-circle` | `circle` | `circle` |
| `fa fa-clipboard`<br>`far fa-clipboard` | `clipboard` | `clipboard` |
| `fa fa-clock`<br>`far fa-clock` | `clock-04` | `clock` |
| `fa fa-times`<br>`fal fa-times`<br>`fas fa-times`<br>`fa fa-xmark` | `cancel-01` | `close` |
| `fas fa-cloud-arrow-up` | `cloud-upload` | `cloud-upload` |
| `fa fa-code` | `code` | `code` |
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
| `far fa-droplet-slash` | `droplet-off` | `droplet-slash` |
| `fa fa-edit`<br>`far fa-edit` | `pencil-edit-02` | `edit` |
| `fa fa-ellipsis`<br>`fa fa-ellipsis-h` | `ellipsis` | `ellipsis` |
| `fa fa-ellipsis-v` | `more-vertical` | `ellipsis-vertical` |
| `fa fa-envelope`<br>`fal fa-envelope`<br>`far fa-envelope`<br>`fas fa-envelope` | `mail-01` | `envelope` |
| `fal fa-envelope-open-text` | `mail-open-01` | `envelope-open-text` |
| `fas fa-euro-sign` | `euro` | `euro` |
| `fa fa-right-left` | `arrow-left-right` | `exchange` |
| `fa fa-exclamation` | `alert-01` | `exclamation` |
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
| `fa fa-globe`<br>`far fa-globe` | `globe` | `globe` |
| `fab fa-google` | `google` | `google` |
| `fa fa-th` | `grid` | `grid` |
| `fa fa-th-large` | `grid-view` | `grid-large` |
| `fa fa-grip` | `grip` | `grip` |
| `fas fa-grip-vertical` | `grip-vertical` | `grip-vertical` |
| `far fa-hammer` | `hammer` | `hammer` |
| `fa fa-dolly` | `delivery-truck-01`<br>Approximation: truck candidate; a hand truck is still needed. | `hand-truck` |
| `fal fa-heart` | `favourite` | `heart` |
| `fa fa-home` | `home-02` | `home` |
| `fa fa-hourglass-start` | `hourglass` | `hourglass` |
| `fab fa-html5` | `html-5` | `html5` |
| `fa fa-image`<br>`far fa-image` | `image-02` | `image` |
| `fa fa-inbox` | `inbox` | `inbox` |
| `fa fa-info` | `info` | `info` |
| `fa fa-circle-info`<br>`fa fa-info-circle` | `information-circle` | `info-circle` |
| `fab fa-instagram` | `instagram` | `instagram` |
| `fa fa-italic` | `text-italic` | `italic` |
| `fa fa-key` | `key-01` | `key` |
| `fa fa-language` | `language-circle` | `language` |
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
| `fa fa-wand-magic-sparkles` | `magic-wand-01` | `magic-wand` |
| `fa fa-mail-forward` | `mail-send-01` | `mail-forward` |
| `fa fa-expand` | `expand` | `maximize` |
| `fa fa-bullhorn` | `megaphone-01` | `megaphone` |
| `fa fa-compress` | `arrow-shrink` | `minimize` |
| `fa fa-minus` | `minus` | `minus` |
| `far fa-mobile-alt`<br>`fa fa-mobile-screen-button` | `smart-phone-01` | `mobile` |
| `far fa-money-bill-1` | `money-01` | `money` |
| `fa fa-desktop`<br>`far fa-desktop`<br>`fa fa-display` | `computer` | `monitor` |
| `fa fa-up-down-left-right` | `arrow-all-direction` | `move` |
| `far fa-newspaper` | `newspaper` | `newspaper` |
| `fa fa-fill-drip`<br>`fas fa-fill-drip` | `paint-bucket` | `paint-bucket` |
| `fa fa-paint-brush`<br>`fa fa-paintbrush` | `paint-brush-01` | `paintbrush` |
| `fa fa-paperclip` | `paperclip` | `paperclip` |
| `fa fa-pause` | `pause` | `pause` |
| `far fa-pause-circle` | `pause-circle` | `pause-circle` |
| `fa fa-pencil`<br>`fas fa-pencil` | `edit-02` | `pencil` |
| `fal fa-phone`<br>`fas fa-phone-flip` | `call` | `phone` |
| `fa fa-eye-dropper` | `pipette` | `pipette` |
| `fa fa-caret-right`<br>`fa fa-play` | `play` | `play` |
| `far fa-play-circle` | `play-circle` | `play-circle` |
| `fa fa-plus` | `plus?stroke-scale=1.5` | `plus` |
| `fa fa-plus-circle` | `add-circle` | `plus-circle` |
| `fa fa-plus-square`<br>`fal fa-plus-square` | `add-square` | `plus-square` |
| `fa fa-power-off` | `power` | `power` |
| `fa fa-print` | `printer` | `printer` |
| `fa fa-puzzle-piece` | `puzzle` | `puzzle-piece` |
| `fa fa-question`<br>`fas fa-question` | `chat-question` | `question` |
| `fa fa-circle-question`<br>`far fa-circle-question`<br>`fa fa-question-circle`<br>`far fa-question-circle`<br>`fas fa-question-circle` | `circle-question-mark` | `question-circle` |
| `fa fa-quote-left` | `quote` | `quotes` |
| `fa fa-recycle` | `recycle-01` | `recycle` |
| `fa fa-redo` | `redo-02` | `redo` |
| `fa fa-rotate`<br>`fa fa-sync` | `refresh` | `refresh` |
| `fa fa-retweet` | `repeat` | `repeat` |
| `fa fa-reply` | `reply` | `reply` |
| `fa fa-reply-all`<br>`fal fa-reply-all` | `reply-all` | `reply-all` |
| `fa fa-robot` | `bot` | `robot` |
| `fas fa-rotate-left` | `rotate-left-01` | `rotate-left` |
| `fa fa-rotate-right` | `rotate-right-01` | `rotate-right` |
| `fa fa-rss` | `rss` | `rss` |
| `fas fa-ruler-combined` | `ruler` | `ruler` |
| `fas fa-balance-scale` | `balance-scale` | `scales` |
| `fa fa-cut` | `scissor` | `scissors` |
| `fa fa-magnifying-glass`<br>`fa fa-search`<br>`far fa-search` | `search-01` | `search` |
| `fa fa-paper-plane`<br>`far fa-paper-plane`<br>`fa fa-send` | `sent` | `send` |
| `fa fa-cogs` | `settings-02` | `settings-multiple` |
| `fa fa-share`<br>`far fa-share` | `share-01` | `share` |
| `far fa-share-square` | `share-01` | `share-square` |
| `fas fa-shield-halved` | `shield-half` | `shield-half` |
| `fa fa-signal` | `signal` | `signal` |
| `fa fa-sitemap` | `hierarchy-square-02` | `sitemap` |
| `fa fa-sliders-h` | `sliders-horizontal` | `sliders` |
| `fa fa-spinner`<br>`fas fa-spinner` | `loader-circle` | `spinner` |
| `far fa-square`<br>`fa fa-stop`<br>`fas fa-stop` | `square` | `square` |
| `fa fa-star` | `star` | `star` |
| `fa fa-star-half` | `star-half` | `star-half` |
| `far fa-tablet-alt`<br>`fa fa-tablet-screen-button`<br>`far fa-tablet-screen-button`<br>`fas fa-tablet-screen-button` | `tablet-02` | `tablet` |
| `fas fa-tag` | `tag-01` | `tag` |
| `fa fa-tags`<br>`fad fa-tags` | `tags` | `tags` |
| `fa fa-tasks` | `task-01` | `tasks` |
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
| `fa fa-arrow-up-from-bracket`<br>`fa fa-upload`<br>`far fa-upload` | `upload-02` | `upload` |
| `fa fa-user`<br>`fal fa-user` | `user-02` | `user` |
| `fa fa-user-plus` | `user-plus` | `user-add` |
| `fa fa-user-check` | `user-check-01` | `user-check` |
| `fal fa-user-circle`<br>`fas fa-user-circle` | `user-circle` | `user-circle` |
| `far fa-user-edit` | `user-edit-01` | `user-edit` |
| `fa fa-user-secret` | `incognito` | `user-secret` |
| `fa fa-user-shield` | `user-shield-01` | `user-shield` |
| `fa fa-group`<br>`fa fa-users` | `user-group` | `users` |
| `fa fa-wallet` | `wallet-01` | `wallet` |
| `fa fa-exclamation-triangle`<br>`fa fa-triangle-exclamation`<br>`fa fa-warning` | `alert-02` | `warning` |
| `far fa-window-restore` | `copy-01`<br>Approximation: overlapping sheets instead of window frames. | `windows` |
| `fa fa-wrench` | `wrench-01` | `wrench` |
| `fab fa-youtube` | `youtube` | `youtube` |

## Bootstrap Icons to HugeIcons

Source names refer to the legacy Bootstrap TagHelper or sprite, including original
`fill` tokens. The imported BI library preserves all upstream names unchanged in its
single `default` variant, including both terminal `-fill` and interior `-fill-` names.
Use `bi:alarm-fill@default`, for example. The former synthetic `light` and `solid`
variants no longer exist. See the [BI import recipe](bootstrap/README.md).

| Source BI icon name | HI target | Concept |
| --- | --- | --- |
| `archive` | `archive-02` | `archive` |
| `arrow-90deg-up` | `arrow-turn-up` | `arrow-turn-up` |
| `arrow-up-short` | `arrow-up-02` | `arrow-up` |
| `aspect-ratio` | `aspect-ratio` | `aspect-ratio` |
| `bar-chart-line` | `chart-column` | `bar-chart` |
| `lightning` | `zap` | `bolt` |
| `box` | `package-01` | `box` |
| `boxes` | `package-03` | `boxes` |
| `braces-asterisk` | `source-code`<br>Approximation: code symbol; asterisk detail is missing. | `braces-asterisk` |
| `building` | `building-03` | `building` |
| `camera-video` | `camera-video` | `camera-video` |
| `cart-check` | `shopping-cart-check-01` | `cart-check` |
| `check`<br>`check-lg` | `check?stroke-scale=1.5` | `check` |
| `chevron-left` | `chevron-left` | `chevron-left` |
| `chevron-right` | `chevron-right` | `chevron-right` |
| `chevron-double-left` | `arrow-left-double` | `chevrons-left` |
| `chevron-double-right` | `arrow-right-double` | `chevrons-right` |
| `circle-fill` | `circle` | `circle` |
| `clipboard` | `clipboard` | `clipboard` |
| `clock` | `clock-04` | `clock` |
| `x`<br>`x-lg` | `cancel-01` | `close` |
| `x-circle` | `cancel-circle` | `close-circle` |
| `columns-gap` | `layout-01` | `columns` |
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
| `envelope` | `mail-01` | `envelope` |
| `envelope-open-heart` | `mail-open-love` | `envelope-heart` |
| `envelope-paper` | `mail-open-01` | `envelope-open` |
| `eraser` | `eraser` | `eraser` |
| `currency-euro` | `euro` | `euro` |
| `arrow-left-right` | `arrow-left-right` | `exchange` |
| `eye`<br>`eye-fill` | `view` | `eye` |
| `facebook` | `facebook-01` | `facebook` |
| `file-earmark-pdf` | `pdf-01` | `file-pdf` |
| `file-earmark-richtext` | `file-02`<br>Approximation: generic document; image detail differs. | `file-richtext` |
| `funnel` | `filter` | `filter` |
| `gear` | `settings-01` | `gear` |
| `gift` | `gift` | `gift` |
| `globe-americas` | `earth` | `globe-americas` |
| `grid-3x3-gap` | `grid` | `grid` |
| `hdd` | `hdd` | `hdd` |
| `heart` | `favourite` | `heart` |
| `card-image`<br>`image` | `image-02` | `image` |
| `images` | `images` | `images` |
| `inboxes` | `inbox` | `inbox` |
| `info-circle` | `information-circle` | `info-circle` |
| `input-cursor-text` | `input-cursor-text` | `input-cursor-text` |
| `stack` | `layers-01` | `layers` |
| `layers-half` | `layers-01`<br>Approximation: full layers instead of a half-layer motif. | `layers-half` |
| `lightbulb` | `lightbulb` | `lightbulb` |
| `graph-up` | `chart-up` | `line-chart` |
| `graph-up-arrow` | `chart-increase` | `line-chart-arrow` |
| `link-45deg` | `link` | `link` |
| `list-ul`<br>`view-list` | `left-to-right-list-dash` | `list` |
| `geo-alt` | `location-01` | `location` |
| `box-arrow-in-right` | `login-02` | `login` |
| `box-arrow-left` | `logout-02` | `logout` |
| `megaphone` | `megaphone-01` | `megaphone` |
| `menu-button-fill` | `menu-01` | `menu-button` |
| `palette` | `paint-board` | `palette` |
| `vector-pen` | `pen-tool-01` | `pen-nib` |
| `pencil`<br>`pencil-fill` | `edit-02` | `pencil` |
| `percent` | `percent` | `percent` |
| `play-fill` | `play` | `play` |
| `power` | `power` | `power` |
| `printer` | `printer` | `printer` |
| `puzzle-fill` | `puzzle` | `puzzle-piece` |
| `question-circle-fill` | `circle-question-mark` | `question-circle` |
| `receipt` | `invoice-01` | `receipt` |
| `recycle` | `recycle-01` | `recycle` |
| `arrow-repeat`<br>`repeat` | `repeat` | `repeat` |
| `arrows-expand-vertical` | `arrow-vertical` | `resize-vertical` |
| `arrow-clockwise` | `rotate-right-01` | `rotate-right` |
| `scissors` | `scissor` | `scissors` |
| `search` | `search-01` | `search` |
| `send` | `sent` | `send` |
| `shield` | `shield-01` | `shield` |
| `shuffle` | `shuffle` | `shuffle` |
| `diagram-3` | `hierarchy-square-02` | `sitemap` |
| `sliders` | `sliders-horizontal` | `sliders` |
| `sliders2-vertical` | `sliders-vertical` | `sliders-vertical` |
| `sort-down-alt` | `sort-by-down-01` | `sort-down` |
| `sort-numeric-down` | `sorting-1-9` | `sort-numeric` |
| `sort-up-alt` | `sort-by-up-01` | `sort-up` |
| `stars-tricolor` | `sparkles`<br>Alternative only. Preserve the existing multicolor icon; see exceptions below. | `sparkles` |
| `star-half` | `star-half` | `star-half` |
| `stickies` | `note-01` | `sticky-notes` |
| `shop-window` | `store-01` | `store` |
| `tags` | `tags` | `tags` |
| `card-checklist`<br>`list-check` | `task-01` | `tasks` |
| `text-wrap` | `text-wrap` | `text-wrap` |
| `textarea-resize` | `resize-field` | `textarea-resize` |
| `hand-thumbs-down` | `thumbs-down` | `thumbs-down` |
| `hand-thumbs-up` | `thumbs-up` | `thumbs-up` |
| `tools` | `tools` | `tools` |
| `trash` | `delete-02` | `trash` |
| `trophy` | `trophy` | `trophy` |
| `truck` | `delivery-truck-01` | `truck` |
| `upload` | `upload-02` | `upload` |
| `person` | `user-02` | `user` |
| `person-badge` | `user-account` | `user-badge` |
| `person-circle` | `user-circle` | `user-circle` |
| `people`<br>`people-fill` | `user-group` | `users` |
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
- `stars-tricolor` is an explicit exception. Keep the existing multicolor rendering
  until deliberately migrated to the custom `sys:stars-tricolor` icon. The table's
  monochrome HI `sparkles` alternative is not an approved automatic replacement.
- The legacy synchronous `Html.Icon()` and `Html.BootstrapIcon()` remain during the
  transition. Their intended replacement is `await Html.IconAsync(...)`.

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
