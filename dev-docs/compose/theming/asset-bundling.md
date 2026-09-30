# Asset bundling

## Overview

JavaScript, Sass and CSS files can be minified in Smartstore. Style bundles also support browser-targeted CSS transpilation, including vendor prefixes and syntax fallbacks. To do this, the files must be exposed to the system using a `BundleProvider`, as bundles are provided by `BundleProviders` that inherit the `IBundleProvider` interface.

## JavaScript

```csharp
internal class Bundles : IBundleProvider
{
    public int Priority => 1;

    public void RegisterBundles(IApplicationContext appContext, IBundleCollection bundles)
    {
        var jsRoot = "/Modules/Smartstore.MyModule/js/";

        bundles.Add(new ScriptBundle("/bundle/js/my-module.js").Include(
            jsRoot + "my-javascript-1.js",
            jsRoot + "my-javascript-2.js",
	    jsRoot + "my-javascript-n.js"));
    }
}
```

Once the bundle is registered this way, you can include it using a standard script include.

```cshtml
<script src="~/bundle/js/my-module.js" sm-target-zone="scripts" sm-key="my-module"></script>
```

When a request is made, all included script files are now combined into one (`my-module.js`) and returned in a minified form.

## CSS

Bundling the `.scss` files is done similarly by adding a `StyleBundle` to the `BundleCollection`.

```csharp
internal class Bundles : IBundleProvider
{
    public int Priority => 1;

    public void RegisterBundles(IApplicationContext appContext, IBundleCollection bundles)
    {
        var cssRoot = "/Modules/Smartstore.MyModule/css/";

        bundles.Add(new StyleBundle("/bundle/js/my-module.css").Include(
            jsRoot + "my-styles-1.scss",
            jsRoot + "my-styles-2.scss",
	    jsRoot + "my-styles-n.jscss"));
    }
}
```

When a request is made, all included Sass or CSS files are combined into one (`my-module.css`) and can be minified. The CSS transpiler also applies browser-targeted compatibility transforms when enabled.

### Settings

When developing UI components, bundling or caching assets can be a hindrance because you may want to inspect your code in the browser. Therefore, there is an option to disable these two features completely: **Admin > Konfiguration > Themes > Settings**.
