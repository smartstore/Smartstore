# Getting started with themes

## Overview

A Smartstore theme is a collection of Sass files, Razor views, images and scripts. In short, everything you need to create websites. Themes can be selected and customized by the store owner using the Theme Configurator (**Admin / Configuration / Themes**). Owners can customize them by configuring theme variables to set different colors, font sizes, margins, and more.

A lot of effort has gone into the development of the Theming Engine to make creating themes easy, flexible and convenient. In addition, we have managed to make creating themes in Smartstore very easy by using techniques such as:

* Multi-level theme inheritance
* Native embedded Dart Sass, which automatically compiles Sass files into CSS at runtime.
* Browser-targeted CSS transpilation
* Modern CSS and icon libraries
* And many more

Thanks to **multi-level theme inheritance**, it is possible to inherit from a base theme or from a theme that has inherited from another theme. This way, there is no need to start from scratch when developing a theme. Just use the existing components and change only what needs to be changed as you build the theme.

{% hint style="info" %}
New themes should always be derived from the _Flex_ base theme or from a theme originally derived from _Flex_.
{% endhint %}

A theme provides **variables** that can be configured by the end user. These are automatically translated into Sass variables and can be used in custom Sass files.

Sass files are automatically compiled at runtime using the **built-in Sass parser**. Razor views are also **compiled at runtime**. A built-in file watcher keeps track of all changes made to Sass and Razor files. When a Razor file is changed, the Razor views are recompiled in the background. When a Sass file is changed, the CSS is regenerated and the cache is cleared. Simply refresh the browser page while the application is running to see changes to Sass files and Razor views live.

To keep static files as small as possible, Smartstore minifies JavaScript, Sass, and CSS files. Multiple physical files of a web project are combined into one file and then minified to create a bundle.

Lightning CSS adds browser-targeted compatibility transforms to the generated style bundle, including vendor prefixes when needed.

Smartstore is built using the [MVC-Pattern](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-7.0). This pattern specifies that the HTML output is provided by views. Views are Razor files located in the subdirectories of the web project's _Views_ directory. They can be easily overwritten at the theme level without having to worry about preparing the model or implementing actions.

Widely used third-party components make development even easier. Among others, Smartstore has integrated:

* [Bootstrap](getting-started-with-themes.md#bootstrap): A popular HTML/CSS framework for creating responsive, mobile-friendly websites. It includes features such as forms, list groups, and custom components.
* [jQuery](getting-started-with-themes.md#jquery): A cross-platform JavaScript library designed to simplify the client-side scripting of HTML. It is designed to make it easier to navigate a document, select DOM elements, create animations, handle events, and develop Ajax applications.
* [Modern icon libraries](getting-started-with-themes.md#icon-libraries) like [Font Awesome](getting-started-with-themes.md#font-awesome): Used to add visual elements to web pages and enhance the user experience. Icons are available in a variety of styles and formats to meet different design needs.

## Anatomy of a theme

Themes are located in the `Smartstore.Web` project in the _Themes_ directory. Any directory in here that contains a `theme.config` file is treated as a theme.

### Files & Folders: Best Practices

There are some conventions for organizing files and directories within a theme. While there is no requirement to follow them, it makes things predictable and easier to maintain:

| Entry                                              | Description                                         |
| -------------------------------------------------- | --------------------------------------------------- |
| :file\_folder: **wwwroot**                         | Static files (including Sass files)                 |
| :file\_folder: **wwwroot/images**                  | Images                                              |
| :file\_folder: **wwwroot/css**                     | Static CSS files                                    |
| :file\_folder: **wwwroot/js**                      | JavaScript files                                    |
| :file\_folder: **Views**                           | Razor view / template files                         |
| :file\_cabinet: theme.config                       | Required. Theme metadata manifest.                  |
| :file\_cabinet: Views/Shared/ConfigureTheme.cshtml | Configuration view for configuring theme variables. |

## Runtime compilation

### Razor runtime compilation

Razor Runtime Compilation is a feature in ASP.NET Core that responds to changes in Razor views and applies them in real time without having to restart the application.

Smartstore has a setting for this feature that is enabled by default. If you want to disable it, open the `appsettings.json` file in the root of the `Smartstore.Web` project and change the value of the `EnableRazorRuntimeCompilation` property.

#### DebugNoRazorCompile

To run Smartstore in Visual Studio without precompiled views, select the **DebugNoRazorCompile** build configuration in Visual Studio. This also has the advantage of speeding up the compilation of the solution. However, it has the disadvantage of slowing down the page load speed for the first hit. This is due to the fact that all used views must first be compiled in the background.

When using **Hot Reload** during debugging, we recommend using the **DebugNoRazorCompile** build configuration. The **Debug** build configuration is extremely slow in detecting and applying code changes.

### Sass runtime compilation

[Sass](https://sass-lang.com/) is a CSS preprocessor, which means it extends the CSS language by adding features like variables, mixins, functions, and many other techniques. These allow you to create CSS that is more maintainable, themable and extensible.

Smartstore uses `.scss` files for CSS declarations. At runtime, the native embedded Dart Sass compiler translates them into CSS that browsers can process.

Smartstore's built-in file watcher keeps track of all changes made to the included Sass files while the application is running. When a change is detected, the cache is automatically cleared and the Sass files are retranslated into CSS. This provides you with a convenient, time-saving way to check for CSS changes on page refresh without having to restart the application.

### CSS transpilation

[Vendor prefixes](https://developer.mozilla.org/en-US/docs/Glossary/Vendor_Prefix) are a part of CSS that is added to certain properties and values. They enable experimental, non-standard features in different browsers. For example, the `-webkit-` prefix is used for properties and values supported by WebKit browsers (Google, Safari, etc.), and Mozilla Firefox uses the `-moz-` prefix.

Smartstore uses Lightning CSS to transform style bundles for the browsers specified by `Bundling:CssTranspiler:Targets`. This includes vendor prefixes and syntax fallbacks where needed, so styles can generally be written without browser-specific declarations.

CSS transpilation is controlled by `Bundling:EnableCssTranspilation`, independently of minification. In development, `Bundling:CssTranspiler:AlwaysDisableInDevMode` disables it by default when no explicit value is set for `EnableCssTranspilation`.

### Cache & DiskCache

Generated assets are cached in RAM and on disk. This keeps the whole process highly performant and delays page rendering by only a few milliseconds when regenerating CSS files. The cache is automatically invalidated when an included file changes, which is done using `DiskCache`. This caching method preserves the generated assets, so they don't need to be regenerated when the application is restarted. Without it, the Sass parser would have to run on each restart, which would delay the startup process.

The cached files are located in the _App\_Data/Tenants/Default/BundleCache_ directory. `DiskCache` can be disabled programmatically by setting the `AssetCachingEnabled` property in [ThemeSettings](../../../src/Smartstore.Core/Platform/Theming/Settings/ThemeSettings.cs) to `1`, or via the backend by disabling **Enable asset caching** in **Configuration / Themes / Settings**.

## Libraries

### jQuery

jQuery is an open-source JavaScript library that makes it easy for developers to work with the Document Object Model (DOM) and interact with HTML pages. It provides a set of methods for selecting, manipulating, and animating DOM elements. jQuery uses a short and concise syntax to simplify the use of JavaScript, and supports a variety of modern web browsers.

{% hint style="info" %}
For more information, see [jQuery](https://jquery.com/).
{% endhint %}

### Bootstrap

Bootstrap is a front-end framework based on HTML, CSS, and JavaScript. One of Bootstrap's most important features is its grid system and the classes it provides for adapting content to different device sizes.

In Smartstore, the mobile-first approach is fully implemented using Bootstrap's CSS classes. All CSS classes provided by Bootstrap can be used in Smartstore to create HTML structures.

{% hint style="info" %}
For more information, see [Bootstrap](https://getbootstrap.com/docs/4.6/layout/overview/).
{% endhint %}

## Icon Libraries

{% hint style="info" %}
**Changed in Smartstore 7:** Icon Kit replaces the previous icon rendering system with a shared SVG system for Razor views and browser components. Existing themes and plugins using Font Awesome markup or Bootstrap icon helpers need migration. See [Migrate existing icons](../../framework/content/icons.md#migrate-existing-icons) for replacement examples and mapping tables.
{% endhint %}

Use Icon Kit to add SVG icons to your theme's Razor views and client-side templates. HugeIcons Free (Stroke Rounded) is the default library; Font Awesome Free and Bootstrap Icons are also included. You can adjust their appearance and change the frontend's icon library through configuration. See [Icons](../../framework/content/icons.md) for examples, configuration and migration guidance.

### HugeIcons

HugeIcons Free (Stroke Rounded) is Smartstore's default icon library for the frontend and backend. Its SVG icons are rendered through Icon Kit and can be styled using the shared size, color and presentation options.

{% hint style="info" %}
For more information about the library, see [HugeIcons](https://hugeicons.com/).
{% endhint %}

### Font Awesome

Font Awesome Free is available through Icon Kit, including Regular, Solid and Brands. Use the SVG Tag Helper or Web Component instead of legacy font-icon CSS classes. Licensed Font Awesome Pro SVGs can be installed separately; see [Add Font Awesome Pro](../../framework/content/icons.md#add-font-awesome-pro).

{% hint style="info" %}
For more information about the library, see [Font Awesome](https://fontawesome.com/icons).
{% endhint %}

### Bootstrap Icons

Bootstrap Icons is available through Icon Kit in both the frontend and backend. Select its drawings by name or use an explicit library address, as described in [Icons](../../framework/content/icons.md).

{% hint style="info" %}
For more information about the library, see [Bootstrap Icons](https://icons.getbootstrap.com/).
{% endhint %}
