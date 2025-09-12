# 🐥 Theme styling

## Overview

To create a completely different look & feel of the store frontend, it is sufficient to create a manifest file (`theme.config`) in a separate directory in the theme directory and define the variables. For deeper changes, add your own CSS to modify existing declarations, overwrite entire sections with your own CSS, or change the given HTML structure by overwriting it with your own Razor views.

## theme.scss

After `theme.config`, the most important file in a theme is `theme.scss`. It is the root Sass file that includes all other Sass files. Our Sass files are organized in a very granular way, representing each section of the store separately. For example, there is one file that contains all the CSS for the checkout process, one for the footer, one for the product detail page, and so on. A complete list of all included Sass files can be found below.

Bootstrap's Sass files are included very early in `theme.scss`, so you get access to Bootstrap's variables and mixins in subsequent includes.

For example, it's possible to use the mixins provided by Bootstrap for responsive rendering everywhere:

```scss
@include media-breakpoint-up(lg) {
    --content-padding-x: #{$content-padding-x};
} 
```

## AutoPrefixer

Smartstore has an integrated CSS autoprefixer to ensure compatibility with different browsers. It is enabled in production mode, but not in debug mode. This allows writing CSS code without vendor prefixes, because the Autoprefixer adds them automatically. It uses the latest available [Can I Use](https://caniuse.com/) data to add the prefix to each corresponding CSS property and value.

## All `.scss` files in Flex

<table><thead><tr><th width="244">File</th><th>Description</th></tr></thead><tbody><tr><td>/.app/themevars.scss</td><td>Virtual file that fetches all theme variables from the database. We use these variables to override Bootstrap defaults.</td></tr><tr><td>_variables-custom.scss</td><td>Empty file to override defaults without modifying source files.</td></tr><tr><td>_variables.scss</td><td>Includes theme-specific variable defaults and Bootstrap variable overrides.</td></tr><tr><td>_fonts.scss</td><td>Empty file with predefined CSS declarations that can be overridden for language-specific font families.</td></tr></tbody></table>

### Bootstrap 4 + variables reset

<table><thead><tr><th width="321">File</th><th>Description</th></tr></thead><tbody><tr><td>/lib/bs4/scss/bootstrap-head.scss</td><td>Custom Smartstore file that contains only the import of Bootstrap functions and variables.</td></tr><tr><td>_variables-reset.scss</td><td>Used to omit, reset, or redefine imported bootstrap variables with custom values before importing Bootstrap main.</td></tr><tr><td>/lib/bs4/scss/bootstrap-main.scss</td><td>Main file to include Bootstrap Sass files.</td></tr></tbody></table>

### Vendor components (neutral)

<table><thead><tr><th width="294">File</th><th>Description</th></tr></thead><tbody><tr><td>lib/rfs/_rfs.scss</td><td>Used to set automated, responsive values for font sizes, padding, margins, and more.</td></tr><tr><td>lib/photoswipe/photoswipe.scss</td><td>Styles for basic PhotoSwipe functionality (sliding area, open / close transitions)</td></tr><tr><td>lib/slick/slick.scss</td><td>Styles for Slick Slider</td></tr><tr><td>lib/select2/scss/core.scss</td><td>Styles for Select2, which gives you a customizable select box with support for searching, tagging, remote records, infinite scrolling, and many more popular options.</td></tr><tr><td>lib/aos/scss/aos.scss</td><td>Styles for AOS (Animate On Scroll) library.</td></tr></tbody></table>

### Button tweaks

<table><thead><tr><th width="284">File</th><th>Description</th></tr></thead><tbody><tr><td>_buttons.scss</td><td>Used to override some Bootstrap values for buttons.</td></tr></tbody></table>

### Global / Shared

<table><thead><tr><th width="278">File</th><th>Description</th></tr></thead><tbody><tr><td>shared/_colors.scss</td><td>The Material Design color palette (commented out for performance reasons).</td></tr><tr><td>shared/_variables-shared.scss</td><td>Used to define &#x26; override variables previously included by third-party libraries.</td></tr><tr><td>shared/_mixins.scss</td><td>Contains our own mixins.</td></tr><tr><td>shared/_spacing.scss</td><td>Mixins for responsive spacing (replaces the default Bootstrap spacing include).</td></tr><tr><td>shared/_typo.scss</td><td>Defines typography styles.</td></tr><tr><td>shared/_fa.scss</td><td>Contains custom styles for Font Awesome.</td></tr><tr><td>shared/_alert.scss</td><td>Contains enhancements for Bootstrap's alert classes.</td></tr><tr><td>shared/_buttons.scss</td><td>Contains enhancements for Bootstrap's button classes &#x26; introduces own button classes.</td></tr><tr><td>shared/_dropdown.scss</td><td>Contains enhancements for Bootstrap's dropdown classes.</td></tr><tr><td>shared/_card.scss</td><td>Contains enhancements for Bootstrap's card classes.</td></tr><tr><td>shared/_forms.scss</td><td>Contains enhancements for Bootstrap's form classes.</td></tr><tr><td>shared/_numberinput.scss</td><td>Styles for our own number input control.</td></tr><tr><td>shared/_breadcrumb.scss</td><td>Contains enhancements for Bootstrap's breadcrumb classes.</td></tr><tr><td>shared/_pagination.scss</td><td>Contains enhancements for Bootstrap's pagination classes.</td></tr><tr><td>shared/_nav.scss</td><td>Contains enhancements for Bootstrap's Navbar classes &#x26; introduces own components based on Bootstraps Navbar classes.</td></tr><tr><td>shared/_nav-collapsible.scss</td><td>Contains styles for a collapsible navbar.</td></tr><tr><td>shared/_modal.scss</td><td>Contains enhancements for Bootstrap's modal classes.</td></tr><tr><td>shared/_throbber.scss</td><td>Styles used by our throbber plugin.</td></tr><tr><td>shared/_spinner.scss</td><td>Styles for a spinner that uses the entire available space of the browser window.</td></tr><tr><td>shared/_star-rating.scss</td><td>Styles to display beautiful stars for ratings.</td></tr><tr><td>shared/_sortable-grip.scss</td><td>Styles to display a gripper.</td></tr><tr><td>shared/_choice.scss</td><td>Styles to display our choice templates (used by Variants etc.)</td></tr><tr><td>shared/_offcanvas.scss</td><td>Contains styles for our offcanvas components (e.g. Offcanvas-Cart)</td></tr><tr><td>shared/_sections.scss</td><td>Contains styles to define theme colored sections.</td></tr><tr><td>shared/_bg.scss</td><td>Contains declarations to be used as background classes.</td></tr><tr><td>shared/_custom-scrollbar.scss</td><td>Defines a slimmer custom scrollbar.</td></tr><tr><td>shared/_box.scss</td><td>Contains classes &#x26; effects used to display content in boxes (e.g. box image, box scale, box rise).</td></tr><tr><td>shared/_utils.scss</td><td>Contains utility classes.</td></tr><tr><td>shared/_switch.scss</td><td>Styles for our own (boolean) switch control.</td></tr><tr><td>shared/_media.scss</td><td>Styles for media file display (in the context of Media Manager)</td></tr><tr><td>shared/_text-expander.scss</td><td>Styles for our own text expander control.</td></tr><tr><td>shared/_entity-picker.scss</td><td>Styles for our own entity picker control.</td></tr></tbody></table>

### Vendor component (skinning)

| File                       | Description                |
| -------------------------- | -------------------------- |
| skinning/\_select2.scss    | Skins Select2 component    |
| skinning/\_pnotify.scss    | Skins PNotify component    |
| skinning/\_photoswipe.scss | Skins PhotoSwipe component |
| skinning/\_slick.scss      | Skins Slick component      |
| skinning/\_drift.scss      | Skins Drift component      |
| skinning/\_fileupload.scss | Skins Fileupload component |
| skinning/\_summernote.scss | Skins Summernote component |

### Main

<table><thead><tr><th width="224">File</th><th>Description</th></tr></thead><tbody><tr><td>_layout.scss</td><td>Contains general layout styles.</td></tr><tr><td>_block.scss</td><td>Contains styles to be used when a topic is rendered as a widget and should be wrapped by a container.</td></tr><tr><td>_shopbar.scss</td><td>Contains styles used by the shopbar.</td></tr><tr><td>_footer.scss</td><td>Contains styles used in the footer.</td></tr><tr><td>_menu.scss</td><td>Contains styles used by menus.</td></tr><tr><td>_megamenu.scss</td><td>Contains styles used by the mega menu.</td></tr><tr><td>_search.scss</td><td>Contains styles used in instant search and search result page.</td></tr><tr><td>_login.scss</td><td>Contains styles used on the login page.</td></tr><tr><td>_rating.scss</td><td>Contains styles used for reviews and ratings.</td></tr><tr><td>_artlist.scss</td><td>Contains styles used to display article lists.</td></tr><tr><td>_product.scss</td><td>Contains styles used on product pages.</td></tr><tr><td>_gallery.scss</td><td>Contains styles used by the Media Gallery on product pages.</td></tr><tr><td>_cart.scss</td><td>Contains styles used on the shopping cart page.</td></tr><tr><td>_checkout.scss</td><td>Contains styles used in the checkout process.</td></tr><tr><td>_accordion.scss</td><td>Contains styles used to display an accordion.</td></tr><tr><td>_cookie-manager.scss</td><td>Contains styles used by the Cookie Manager window.</td></tr><tr><td>_misc.scss</td><td>Contains various styles that do not fit anywhere else.</td></tr><tr><td>_print.scss</td><td>Contains styles used when printing documents.</td></tr></tbody></table>

### Custom imports from modules

<table><thead><tr><th width="257">File</th><th>Description</th></tr></thead><tbody><tr><td>/.app/moduleimports.scss</td><td>Virtual file that fetches all styles from modules defined by public.scss in the wwwroot directories in Modules.</td></tr><tr><td>_custom.scss</td><td>The _custom.scss file can be used, for example, to override CSS declarations already made in the base theme. This file is the last to be included in our main theme (Flex), so its declarations have the highest priority.</td></tr><tr><td>_user.scss</td><td>The _user.scss file is for the end user, and should be present in every theme, but empty, so that the end user can place their own CSS here, which will remain untouched by updates to the actual theme.</td></tr></tbody></table>
