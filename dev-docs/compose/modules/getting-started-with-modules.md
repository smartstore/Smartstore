# ✔️ Getting started with modules

## Overview

Modules are designed to extend Smartstore functionality in any way you can imagine. There are no limits to what features they can add.

Here are a few examples of what modules can do:

* alter the way the app operates
* change workflows
* modify / extend UI
* overwrite services

Modules are sets of extensions that compile into a single assembly in order to be re-used in other Smartstore shops. Even though it may use Smartstore APIs, they are no necessity. The only two requirements for a module project are:

* `module.json`: A manifest file describing the metadata of a module.
* `Module.cs`: A class that implements `IModule` and contains (un-)installation routines.

Some special, mostly commerce related features are encapsulated as [_providers_ ](../../framework/platform/modularity-and-providers.md)in the Smartstore ecosystem. A module can expose as many of these providers as needed.

Represented by their interfaces, provider types are:

* `IPaymentMethod`: Payment providers (PayPal, Stripe, AmazonPay, offline payment etc.)
* `ITaxProvider`: Tax calculation
* `IShippingRateComputationMethod`: Shipping fee calculation
* `IExportProvider`: Data export (Shops, products, orders etc.)
* `IMediaStorageProvider`: Storage for media file blobs
* `IOutputCacheProvider`: Storage for output cache items
* `IWidget`: Content rendering in UI
* `IExternalAuthenticationMethod`: External authenticators (Google, Facebook etc.)
* `IExchangeRateProvider`: Live currency rates

## Module structure

A module is a regular **Class Library** project in the solution. It should be placed in the _/src/Smartstore.Modules/_ directory in the root of your solution.

{% hint style="warning" %}
Do not confuse this with the **/src/Smartstore.Web/Modules/** directory, which is the build target for module. From here the application loads module assemblies into the app-domain dynamically.
{% endhint %}

If your project directory is located elsewhere, you should create a _symlink_ that links to the actual location.

{% hint style="info" %}
It is good practice to use the **-sym** suffix in symlink sources, because they are git-ignored.
{% endhint %}

For module projects we recommend the naming convention **{Organization}.{ModuleName}**, but you can choose any name you wish. It should also be the _root namespace_ and the _module system name_.

If your module is called **MyOrg.MyGreatModule**, the _.csproj_ project file should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
    <PropertyGroup>
	<Product>A great module for Smartstore</Product>
	<OutputPath>..\..\Smartstore.Web\Modules\MyOrg.MyGreatModule</OutputPath>
	<OutDir>$(OutputPath)</OutDir>
    </PropertyGroup>
</Project>
```

Each time the solution is built, your module will be compiled and copied to the `OutputPath` directory specified here.

### Smartstore.Module.props

The file _Smartstore.Build/Smartstore.Module.props_ defines shared properties for modules. It is auto-included into every project located in _Smartstore.Modules/_.

Among other things it specifies files and directories...:

* to be copied to the `OutputPath`, if they exist.
  * _module.json_
  * _wwwroot/_
  * _Localization/_
  * _Views/_
* **not** to be copied to the `OutputPath`.

{% hint style="warning" %}
This is important, because the build process would copy the whole dependency graph to the output, which produces too much noise and file redundancy.
{% endhint %}

### Project & Package references

All projects located in the _Smartstore.Modules_ directory reference `Smartstore`, `Smartstore.Core` and `Smartstore.Web.Common` projects by default.

{% hint style="info" %}
You can also reference `Smartstore.Web` to access model types declared there. But add the following lines to the project file to prevent your project copying dependent files to your `OutputPath`:

```xml
<ItemGroup>
    <ProjectReference Include="..\..\Smartstore.Web\Smartstore.Web.csproj">
        <Private>False</Private>
        <CopyLocal>False</CopyLocal>
        <CopyLocalSatelliteAssemblies>False</CopyLocalSatelliteAssemblies>
    </ProjectReference>
</ItemGroup>
```
{% endhint %}

You can reference any NuGet package you wish, but special consideration is required for packages that are **not** referenced by the app core (private). These need to be listed under `PrivateReferences` in _module.json_ (see below), otherwise a runtime error is thrown.

### Manifest: module.json

This file describes your module to the system and is used by the _Plugin Manager_ in it's administration screen.

Here is an example of a working `module.json` file.

{% code title="module.json" %}
```json
{
    "$schema": "../module.schema.json",
    "Author": "MyOrg",
    "Group": "Payment",
    // Required. Module system name.
    "SystemName": "MyOrg.MyGreatModule",
    // Required. English friendly name.
    "FriendlyName": "A great module for Smartstore",
    "Description": "Lorem ipsum",
    // Required. The current version of module.
    "Version": "5.0",
    "MinAppVersion": "5.0",
    "Order": 1,
    "ResourceRootKey": "Plugins.Payments.MyGreatModule",
    "ProjectUrl": "https://myorg.com",
    "PrivateReferences": [
        "MiniProfiler.Shared",
        "MiniProfiler.AspNetCore",
        "MiniProfiler.AspNetCore.Mvc",
        "MiniProfiler.EntityFrameworkCore"
    ]
}
```
{% endcode %}

{% hint style="info" %}
The properties `SystemName`, `FriendlyName` and `Version` are required.
{% endhint %}

The following table explains the schema.

<table><thead><tr><th width="220">Name</th><th width="104.33333333333331">Type</th><th>Description</th></tr></thead><tbody><tr><td><strong>AssemblyName</strong></td><td>string</td><td>The assembly name. <strong>Default</strong>: '{SystemName}.dll'</td></tr><tr><td><strong>Author</strong></td><td>string</td><td>The author's name.</td></tr><tr><td><strong>FriendlyName *</strong></td><td>string</td><td>A readable, easy to understand, english name.</td></tr><tr><td><strong>Group</strong></td><td>enum</td><td><p>A conceptual group name. Used to visually categorize modules in listings.</p><p><strong>Possible values</strong>: <em>Admin</em>, <em>Marketing</em>, <em>Payment</em>, <em>Shipping</em>, <em>Tax</em>, <em>Analytics</em>, <em>CMS</em>, <em>Media</em>, <em>SEO</em>, <em>Data</em>, <em>Globalization</em>, <em>Api</em>, <em>Mobile</em>, <em>Social</em>, <em>Security</em>, <em>Developer</em>, <em>Sales</em>, <em>Design</em>, <em>Performance</em>, <em>B2B</em>, <em>Storefront</em>, <em>Law</em>.</p></td></tr><tr><td><strong>DependsOn</strong></td><td>array</td><td>Array of module system names the module depends on.</td></tr><tr><td><strong>Description</strong></td><td>string</td><td>A short english description of the module.</td></tr><tr><td><strong>MinAppVersion</strong></td><td>string</td><td><p>The minimum compatible application version, e.g. '5.0.2'.</p><p>The module will be unavailable, if the current app version is lower than this value.</p></td></tr><tr><td><strong>Order</strong></td><td>integer</td><td>The display order in the module manager group.</td></tr><tr><td><strong>PrivateReferences</strong></td><td>array</td><td><p>Optional array of private dependency package names that a module references.</p><p>By default referenced packages are not copied to the <code>OutputPath</code>. It is assumed, that the application core already references them. Any private module package should be listed here, including the complete dependency chain.</p></td></tr><tr><td><strong>ProjectUrl</strong></td><td>string</td><td>Link to the project's or author's homepage.</td></tr><tr><td><strong>ResourceRootKey</strong></td><td>string</td><td>Root key for language resources (see <a href="localizing-modules.md">Localizing modules</a>).</td></tr><tr><td><strong>SystemName *</strong></td><td>string</td><td>Module system name. Usually the assembly name without the extension.</td></tr><tr><td><strong>Tags</strong></td><td>string</td><td>Comma-separated tags</td></tr><tr><td><strong>Version *</strong></td><td>string</td><td>The current version of the module e.g. '5.1.0'</td></tr></tbody></table>

### Module entry class

Every module needs an entry class containing the bare minimum of the un- and install methods. To be recognized as such the class must implement the `IModule` interface.

{% hint style="info" %}
It is also recommended to derive from the abstract [ModuleBase](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Modularity/ModuleBase.cs) instead of implementing `IModule`, because it contains some common implementations.
{% endhint %}

The installation method `IModule.InstallAsync()` is called every time the module is installed. Respectively the same goes for the uninstall method `IModule.UninstallAsync()`.

{% hint style="info" %}
It is good practice **not** to delete any custom module data from the database while uninstalling, in case the user wants to re-install the module later.
{% endhint %}

By convention the file named `Module.cs` is `internal` and is placed in the project's root directory.

{% hint style="info" %}
If your module contains exactly one feature provider, it is recommended to let the entry class implement the provider interface directly.
{% endhint %}

The `IConfigurable` interface is used to expose the route to a module configuration page linked to the _Plugin Manager_ UI.

The following code shows an example of a working `Module.cs` file.

{% code title="Module.cs" %}
```csharp
internal class Module : ModuleBase, IConfigurable
{
    public RouteInfo GetConfigurationRoute()
        => new("Configure", "MyGreatAdmin", new { area = "Admin" });

    public override async Task InstallAsync(ModuleInstallationContext context)
    {
        // Saves the default state of a settings class to the database 
        // without overwriting existing values.
        await TrySaveSettingsAsync<MyGreatModuleSettings>();
        
        // Imports all language resources for the current module from 
        // xml files in "Localization" directory (if any found).
        await ImportLanguageResourcesAsync();
        
        // VERY IMPORTANT! Don't forget to call.
        await base.InstallAsync(context);
    }

    public override async Task UninstallAsync()
    {
        // Deletes all "MyGreatModuleSettings" properties settings from the database.
        await DeleteSettingsAsync<MyGreatModuleSettings>();
        
        // Deletes all language resource for the current module 
        // if "ResourceRootKey" is module.json is not empty.
        await DeleteLanguageResourcesAsync();
        
        // VERY IMPORTANT! Don't forget to call.
        await base.UninstallAsync();
    }
}
```
{% endcode %}

### Files & Folders Best Practices

There are some conventions on how to organize the files and directories within a project. Though there is no obligation to comply, it makes things predictable and easier to maintain.

The following is an exhaustive list of files & folders.

<table><thead><tr><th width="266">Entry</th><th>Description</th></tr></thead><tbody><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>App_Data</strong></td><td>App specific (cargo) data like templates, sample files etc. that needs to be published.</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Blocks</strong></td><td>Page Builder Block implementations (see <a href="../../framework/content/page-builder-and-blocks.md">Page Builder and Blocks</a>).</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Bootstrapping</strong></td><td>Bootstrapping code like <em>Autofac</em> modules, DI extensions etc.</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Client</strong></td><td>RESTful API clients</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Components</strong></td><td>MVC View Components (see <a href="controllers-and-viewcomponents.md">Controllers and ViewComponents</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Configuration</strong></td><td>Settings class implementations (see <a href="../../framework/platform/configuration.md">Configuration</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Controllers</strong></td><td>MVC Controllers (see <a href="controllers-and-viewcomponents.md">Controllers and ViewComponents</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Domain</strong></td><td>Domain entities (see <a href="../../getting-started/domain.md">Domain</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Extensions</strong></td><td>Static extension method classes</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Filters</strong></td><td>MVC Filters (see <a href="filters.md">Filters</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Hooks</strong></td><td>Hook implementations (see <a href="../../framework/platform/hooks.md">Hooks</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Localization</strong></td><td>Localization files (see <a href="localizing-modules.md">Localizing modules</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Media</strong></td><td>Media system related classes</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Migrations</strong></td><td>Fluent data migrations (see <a href="../../framework/platform/database-migrations.md">Database Migrations</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Models</strong></td><td>View model classes (see <a href="../../framework/platform/data-modelling/">Data Modelling</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Providers</strong></td><td>Provider implementations</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Security</strong></td><td>Security related classes</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Services</strong></td><td>Service classes</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Utils</strong></td><td>Utilities</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Tasks</strong></td><td>Task scheduler jobs (see <a href="../../framework/platform/scheduling.md">Scheduling</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>TagHelpers</strong></td><td>Tag Helpers</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>Views</strong></td><td>Razor view/template files</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f4c1">📁</span> <strong>wwwroot</strong></td><td>Static files (including Sass)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f5c4">🗄️</span> AdminMenu.cs</td><td>Admin menu hook (see <a href="../../framework/content/menus.md">Menus</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f5c4">🗄️</span> CacheableRoutes.cs</td><td>Route registrar for output cache (see <a href="../../framework/platform/output-cache.md">Output Cache</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f5c4">🗄️</span> Events.cs</td><td>Event handler methods (see <a href="../../framework/platform/events.md">Events</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f5c4">🗄️</span> <a href="getting-started-with-modules.md#module-entry-class">Module.cs</a> *</td><td>Required. Module entry class (implements <code>IModule</code>).</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f5c4">🗄️</span> <a href="getting-started-with-modules.md#manifest-module.json">module.json</a> *</td><td>Required. Module metadata manifest.</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f5c4">🗄️</span> Permissions.cs</td><td>Module permissions (see <a href="../../framework/platform/security.md">Security</a>)</td></tr><tr><td><span data-gb-custom-inline data-tag="emoji" data-code="1f5c4">🗄️</span> Startup.cs</td><td>Module bootstrapper (see <a href="../../framework/platform/bootstrapping.md">Bootstrapping</a>)</td></tr></tbody></table>
