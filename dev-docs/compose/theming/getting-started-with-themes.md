# 🐥 Getting started with themes

## Overview

A Smartstore theme is a collection of Sass files, Razor views, images and scripts. In short, everything you need to create websites. Themes can be selected and customized by the store owner using the Theme Configurator (**Admin / Configuration / Themes**). Owners can customize them by configuring theme variables to set different colors, font sizes, margins, and more.

A lot of effort has gone into the development of the Theming Engine to make creating themes easy, flexible and convenient. In addition, we have managed to make creating themes in Smartstore very easy by using techniques such as:

* Multi-level theme inheritance
* An integrated Sass compiler, that automatically translates all changes made to Sass files into CSS at runtime in an intelligent and highly performant way.
* CSS _Autoprefixer_
* Modern CSS and icon libraries&#x20;
* And many more

Thanks to **multi-level theme inheritance**, it is possible to inherit from a base theme or from a theme that has inherited from another theme. This way, there is no need to start from scratch when developing a theme. Just use the existing components and change only what needs to be changed as you build the theme.

{% hint style="info" %}
New themes should always be derived from the _Flex_ base theme or from a theme originally derived from _Flex_.
{% endhint %}

A theme provides **variables** that can be configured by the end user. These are automatically translated into Sass variables and can be used in custom Sass files.

Sass files are automatically compiled at runtime using the **built-in Sass parser**. Razor views are also **compiled at runtime**. A built-in file watcher keeps track of all changes made to Sass and Razor files. When a Razor file is changed, the Razor views are recompiled in the background. When a Sass file is changed, the CSS is regenerated and the cache is cleared. Simply refresh the browser page while the application is running to see changes to Sass files and Razor views live.

To keep static files as small as possible, Smartstore minifies JavaScript, Sass, and CSS files. Multiple physical files of a web project are combined into one file and then minified to create a bundle.

The _Autoprefixer_ adds vendor-specific prefixes to CSS declarations coming from the Sass parser.

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

Smartstore uses `.scss` files for CSS declarations. They provide a way to use Sass variables and functions. At runtime, Sass is automatically translated into CSS by the built-in Sass parser, which, unlike Sass, can be read by any browser.

Smartstore's built-in file watcher keeps track of all changes made to the included Sass files while the application is running. When a change is detected, the cache is automatically cleared and the Sass files are retranslated into CSS. This provides you with a convenient, time-saving way to check for CSS changes on page refresh without having to restart the application.

### Autoprefixer

[Vendor prefixes](https://developer.mozilla.org/en-US/docs/Glossary/Vendor_Prefix) are a part of CSS that is added to certain properties and values. They enable experimental, non-standard features in different browsers. For example, the `-webkit-` prefix is used for properties and values supported by WebKit browsers (Google, Safari, etc.), and Mozilla Firefox uses the `-moz-` prefix.

Without using a tool like CSS Autoprefixer, you would have to take care of adding the correct prefixes yourself. This can be tedious and error-prone.

To ensure compatibility with different browsers, Smartstore has a built-in CSS Autoprefixer. It is enabled in production mode, but not in debug mode. This allows you to write CSS code without having to add vendor prefixes yourself, as the tool will add them automatically. It uses the latest available [Can I Use](https://caniuse.com/) data to add the prefix to each corresponding CSS property and value.

By using CSS Autoprefixer, developers can rest assured that all CSS styles will display correctly in all major browsers. They can focus on designing the site without worrying about compatibility.

### Cache & DiskCache

Generated assets are cached in RAM and on disk. This keeps the whole process highly performant and delays page rendering by only a few milliseconds when regenerating CSS files. The cache is automatically invalidated when an included file changes, which is done using `DiskCache`. This caching method preserves the generated assets, so they don't need to be regenerated when the application is restarted. Without it, the Sass parser would have to run on each restart, which would delay the startup process.

The cached files are located in the _App\_Data/Tenants/Default/BundleCache_ directory. `DiskCache` can be disabled programmatically by setting the `AssetCachingEnabled` property in [ThemeSettings](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Theming/Settings/ThemeSettings.cs) to `1`, or via the backend by disabling **Enable asset caching** in **Configuration / Themes / Settings**.

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

### Font Awesome

Font Awesome is an icon library that provides high quality, scalable font icons for use in websites and applications. The icons can be easily embedded using CSS classes and are available in different styles and sizes.

One of the advantages of Font Awesome is that the icons are scalable, so they look good and fit well on different screen resolutions. In addition, the icons can be customized with CSS to match the design of the website. Font Awesome offers a wide range of icons for different areas such as social media, user interfaces, signs and much more.

Font Awesome is fully integrated into Smartstore and can be used in all Razor views and CSS declarations.

{% hint style="info" %}
For more information, see [Font Awesome](https://fontawesome.com/icons).
{% endhint %}

#### Icon variants

Font Awesome icons have light, regular, and solid variants. The displayed variant is specified by a CSS class (.fa, .fas, .far, or .fal), which sets the corresponding `font-weight` value. The `font-weight` values for the variant classes are defined by `$icon-font-weight-default` and `$icon-font-variants`. Changing these values has no effect when using the free version of Font Awesome.

<table><thead><tr><th width="182">Variant</th><th width="87.33333333333331">Class</th><th width="342">Sass variable</th><th>font-weight</th></tr></thead><tbody><tr><td>Fallback to Solid</td><td>fa</td><td>$icon-font-weight-default</td><td>900</td></tr><tr><td>Solid</td><td>fa<strong>s</strong></td><td>$icon-font-variants["solid"]</td><td>900</td></tr><tr><td>Regular</td><td>fa<strong>r</strong></td><td>$icon-font-variants["regular"]</td><td>400</td></tr><tr><td>Light</td><td>fa<strong>l</strong></td><td>$icon-font-variants["light"]</td><td>300</td></tr></tbody></table>

#### Font Awesome Free

The free version of Font Awesome contains a subset of icons. All solid and few regular icons are included, but none of the light icons.

To keep the store's look consistent, the `font-weight` of all icons is set to `900`, then all supported regular icons and their light counterparts are set to `400`. This will ensure that all supported icons are displayed, even when using classes (like fal) whose icons are not available in the free version.

#### Font Awesome Pro

The professional version of Font Awesome removes the icon limitations and includes all solid, regular and light icons.

Solid, regular and light icons all follow their font-weight values from `$icon-font-variants`. The `font-weight` of the .`fa` class is set to `$icon-font-weight-default`, unless it is 900.

For licensing reasons, we cannot ship Font Awesome Pro directly. In order to use Font Awesome Pro after you bought a license, you must complete the following steps.

* The `fa-use-pro` theme variable must be set to `true` in the `theme.config` file.
* The Font Awesome Pro includes must be added to a view that is rendered globally for each page, such as `Head.cshtml`:

```cshtml
<link sm-target-zone="head_links" rel="preconnect" href="https://pro.fontawesome.com" />
<link sm-target-zone="stylesheets" rel="stylesheet" href="https://pro.fontawesome.com/releases/v5.10.1/css/all.css" integrity="your key" crossorigin="anonymous" />
```

### Bootstrap Icons

_Bootstrap Icons_ is a free, high quality, open source icon library with over 1,800 icons. It can be used in HTML and CSS code to add graphical elements to websites and mobile applications.

_Bootstrap Icons_ is only available in Smartstore's backend, as the CSS for the frontend should remain as lightweight as possible.

{% hint style="info" %}
For more information, see [Bootstrap Icons](https://icons.getbootstrap.com/).
{% endhint %}

### Fontastic

We’ve used Fontastic to integrate a special selection of icons that are relevant to e-commerce.

{% hint style="info" %}
A full list of all available icons can be found in [fontastic.css](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Web/wwwroot/lib/fontastic/fontastic.css).
{% endhint %}
