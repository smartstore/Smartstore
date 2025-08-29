# ✔️ Building a simple "Hello World" module

Before we start, please look at the introduction to [creating modules](../getting-started-with-modules.md). The basic files needed to create a module are already described there.

## Creating a project file

Start by creating a project file for your modules.

1. Open the Smartstore Solution _Smartstore.sln_
2. Right click on the _Modules_ Folder in the Solution Explorer
3. Add a **New Project** of type _Class Library_
4. Name it _MyOrg.HelloWorld_
5. Make sure the physical path of the project is _Smartstore/src/Smartstore.Modules_

Now alter `MyOrg.HelloWorld.csproj` to look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
    <PropertyGroup>
        <Product>A Hello World module for Smartstore</Product>
        <OutputPath>..\..\Smartstore.Web\Modules\MyOrg.HelloWorld</OutputPath>
        <OutDir>$(OutputPath)</OutDir>
    </PropertyGroup>
</Project>
```

## Adding module metadata

Add module.json next. For more information, refer to [the manifest](../getting-started-with-modules.md#manifest-module.json).

1. Right click on the project in the Solution Explorer.
2. **Add / New Item / Javascript JSON Configuration File**.
3. Name it `module.json`
4. Make another right click, select the **Properties** context item and change:

| Property                 | Value         |
| ------------------------ | ------------- |
| Build Action             | Content       |
| Copy to Output Directory | Copy if newer |

&#x20;  5\. Add the following content

{% code title="module.json" %}
```json
{
  "$schema": "../module.schema.json",
  "Author": "My Org",
  "Group": "Admin",
  "SystemName": "MyOrg.HelloWorld",
  "FriendlyName": "Hello World",
  "Description": "This module says Hello World",
  "Version": "5.0",
  "MinAppVersion": "5.0",
  "Order": 1,
  "ResourceRootKey": "Plugins.MyOrg.HelloWorld",
  "ProjectUrl": "https://myorg.com"
}
```
{% endcode %}

{% hint style="danger" %}
**Version numbers in module.json must match** the current version of Smartstore.

A module is generally compatible if both the app version and the `MinAppVersion` of the module are the same, OR - if the app version is greater - it is _assumed_ to be compatible if there has been no breaking changes in the core since `MinAppVersion`.

Incompatible modules **will not** be loaded by Smartstore.
{% endhint %}

## Creating the module

Change the name of `Class1.cs` to `Module.cs` and add the following code:

{% code title="Module.cs" %}
```csharp
using System.Threading.Tasks;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

internal class Module : ModuleBase, IConfigurable
{
    public RouteInfo GetConfigurationRoute()
        => new("Configure", "HelloWorldAdmin", new { area = "Admin" });

    public override async Task InstallAsync(ModuleInstallationContext context)
    {
        // Saves the default state of a settings class to the database 
        // without overwriting existing values.
        //await TrySaveSettingsAsync<HelloWorldSettings>();
        
        // Imports all language resources for the current module from 
        // xml files in "Localization" directory (if any found).
        await ImportLanguageResourcesAsync();
        
        // VERY IMPORTANT! Don't forget to call.
        await base.InstallAsync(context);
    }

    public override async Task UninstallAsync()
    {
        // Deletes all "HelloWorldSettings" properties settings from the database.
        //await DeleteSettingsAsync<HelloWorldSettings>();
        
        // Deletes all language resource for the current module 
        // if "ResourceRootKey" is module.json is not empty.
        await DeleteLanguageResourcesAsync();
        
        // VERY IMPORTANT! Don't forget to call.
        await base.UninstallAsync();
    }
}
```
{% endcode %}

After compiling the project, the module is recognized by Smartstore and can be installed via **Admin / Plugins / Manage Plugins / Hello World / Install**.

There are two things to note here:

1. Clicking on **Configure** will lead you to a 404 page. This is because no controller has been added and there is no action to handle the configuration route.
2. The method to add default settings to the database and the method to remove them are commented out because the _Settings_ class doesn't exist yet.

## Adding a Settings class

For more detailed information on _Settings_ visit the section [configuration.md](../../../framework/platform/configuration.md "mention"). For this tutorial, just add a simple _Setting_ class with one string property.

1. Right click on the project in the Solution Explorer.
2. Add a new folder. According to our guidelines we call it _Configuration_.
3. Place a new class called `HelloWorldSettings.cs` in this folder.

{% code title="Module.cs" %}
```csharp
using Smartstore.Core.Configuration;

namespace MyOrg.HelloWorld.Settings
{
    public class HelloWorldSettings : ISettings
    {
        public string Name { get; set; } = "John Smith";
    }
}
```
{% endcode %}

Now we can uncomment the corresponding lines in `Module.cs`, which saves the initial settings when the module is installed and removes them when the module is uninstalled. When the module is installed again, the `HelloWorldSettings.Name` setting is saved to the database with the default value of "John Smith".

## Adding configuration

Now that you have a setting for your module, add this code to make the setting configurable. In `Module.cs` implement the interface `IConfigurable`, which has the method `GetConfigurationRoute` that returns `RouteInfo`. The method will be called when clicking on the **Config** button next to the module in the **Plugin Management** section of the shops administration area.

{% code title="Module.cs" %}
```csharp
public RouteInfo GetConfigurationRoute()
    => new("Configure", "HelloWorldAdmin", new { area = "Admin" });
```
{% endcode %}

Using the `RouteInfo`, Smartstore looks for an action called `Configure` in a Controller named `HelloWorldAdminController` in the `Admin` area.

## MVC parts

### Controller

Add the controller:

1. Right click on the project in the Solution Explorer.
2. Add a new folder. According to our guidelines we call it _Controllers_.
3. Place a new class called `HelloWorldAdminController.cs` in this folder.

{% code title="HelloWorldAdminController.cs" %}
```csharp
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using MyOrg.HelloWorld.Models;
using MyOrg.HelloWorld.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace MyOrg.HelloWorld.Controllers
{
    public class HelloWorldAdminController : AdminController
    {
        [LoadSetting, AuthorizeAdmin]
        public IActionResult Configure(HelloWorldSettings settings)
        {
            var model = MiniMapper.Map<HelloWorldSettings, ConfigurationModel>(settings);
            return View(model);
        }

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult Configure(ConfigurationModel model, HelloWorldSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(Configure));
        }
    }
}
```
{% endcode %}

The configuration model is still missing, so there will be 3 errors right now. Notice the `Area` attribute the controller is decorated with. This means that all its actions are reachable only within this area. If you want to add actions to the module within another area don't forget to decorate these actions with the desired area or add another controller.

{% hint style="info" %}
By inheriting from AdminController the _admin_ `Area` attribute is added automatically.
{% endhint %}

According to the MVC pattern, there are two actions in this controller to handle the configure view. The first action is for the `GET` request and the second handles the `POST` request.

| Attributes       | Description                                                                                                                                                                                           |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AuthorizeAdmin` | Makes sure the current user has the right to access this view                                                                                                                                         |
| `LoadSetting`    | Loads the setting values of the settings class passed as the action parameter automatically from the database                                                                                         |
| `SaveSetting`    | Saves the setting values of the settings class passed as the action parameter automatically to the database after the action was executed. So you can store your model values in the settings object. |

Use the `SaveSetting` attribute in combination with the `MiniMapper`, which maps simple properties with the same name to each other. Calling `MiniMapper.Map(model, settings)` maps the _Name_ property of the setting class to the `Name` property of the model class.

{% hint style="info" %}
Refer to [MiniMapper](../../../framework/platform/data-modelling/model-mapping.md#minimapper) for more information.
{% endhint %}

If the `ModelState` isn’t valid, do a post back by returning `Configure(settings)` to display model validation errors, otherwise redirect to the `GET` action to prevent unnecessary form posts.

### Model

As mentioned above, in this simple use case, the configuration model for the module settings is a simple copy of the settings class.

1. Right click on the project in the Solution Explorer.
2. Add a new folder. According to our guidelines we call it _Models_.
3. Place a new class called `ConfigurationModel.cs` in this folder.

{% code title="ConfigurationModel.cs" %}
```csharp
using Smartstore.Web.Modelling;

namespace MyOrg.HelloWorld.Models
{
    [LocalizedDisplay("Plugins.MyOrg.HelloWorld.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }
    }
}
```
{% endcode %}

### View

Now add the view that is rendered by the `GET` action of the controller.

1. Right click on the project in the Solution Explorer.
2. Add a new folder and call it _Views/HelloWorldAdmin_.
3. Place a new **Empty Razor View** called `Configure.cshtml` in this folder.

{% code title="Configure.cshtml" %}
```cshtml
@model ConfigurationModel

@{
    Layout = "_ConfigureModule";
}

@* 
    Render "StoreScope" component if your setting class has 
    one or more multi-store enabled properties.
    It renders a store chooser that sets the current store scope.
    This way individual settings can be overridden on store level.
*@

@await Component.InvokeAsync("StoreScope")

@* Render the save button in admin toolbar *@
<widget target-zone="admin_button_toolbar_before">
    <button id="SaveConfigButton" type="submit" name="save" class="btn btn-warning" value="save">
        <i class="fa fa-check"></i>
        <span>@T("Admin.Common.Save")</span>
    </button>
</widget>

<form asp-action="Configure">
    <div asp-validation-summary="All"></div>
    <div class="adminContent">
        <div class="adminRow">
            <div class="adminTitle">
                <smart-label asp-for="Name" />
            </div>
            <div class="adminData">
                <setting-editor asp-for="Name"></setting-editor>
                <span asp-validation-for="Name"></span>
            </div>
        </div>
    </div>
</form>
```
{% endcode %}

To reduce the amount of _using directives_ in the view, it is recommended to add a `_ViewImports.cshtml` file directly in the _Views_ directory. Add the most important namespaces and the model namespace. This file includes the built-in Microsoft Tag Helpers as well as the Smartstore Tag Helpers.

{% code title="_ViewImports.cshtml" %}
```cshtml
@inherits Smartstore.Web.Razor.SmartRazorPage<TModel>

@using System
@using System.Globalization
@using Smartstore.Web.TagHelpers.Admin
@using Smartstore.Web.Rendering
@using MyOrg.HelloWorld.Models

@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper Smartstore.Web.TagHelpers.Shared.*, Smartstore.Web.Common
@addTagHelper Smartstore.Web.TagHelpers.Admin.*, Smartstore.Web.Common
```
{% endcode %}

After building the module, you can click on the **Configure** button to store a value for the `HelloWorldSettings.Name` setting. It’s stored in the database by simply entering it in the provided input field of the configuration view.

## Adding localization

If you take a look at the `ConfigurationModel`, you'll see that the properties are decorated with the `LocalizedDisplay` attribute. This is a way to add localized display values to describe the property. On property level, the attribute can either contain the full resource key `[LocalizedDisplay("Plugins.MyOrg.HelloWorld.Name")]` or inherit a part of the declaring class also decorated with the attribute.

The resource values are added using `resource.*.xml` XML files, where \* represents the culture code of the target language:

1. Right click on the project in the Solution Explorer.
2. Add a new folder. The folder must be called _Localization_.
3. Place a new XML file called `resources.en-us.xml` in this folder.
4. Make another right click, select the **Properties** context item and change&#x20;

| Property                 | Value         |
| ------------------------ | ------------- |
| Build Action             | Content       |
| Copy to Output Directory | Copy if newer |

```xml
<Language Name="English" IsDefault="false" IsRightToLeft="false">
    <LocaleResource Name="Plugins.FriendlyName.MyOrg.HelloWorld" AppendRootKey="false">
        <Value>Hello World</Value>
    </LocaleResource>
    <LocaleResource Name="Plugins.Description.MyOrg.HelloWorld" AppendRootKey="false">
        <Value>This plugin says Hello World.</Value>
    </LocaleResource>

    <LocaleResource Name="Plugins.MyOrg.HelloWorld" AppendRootKey="false">
        <Children>
            <LocaleResource Name="Name">
                <Value>Name to greet</Value>
            </LocaleResource>
            <LocaleResource Name="Name.Hint">
                <Value>Enter the name of the person to be greeted.</Value>
            </LocaleResource>
        </Children>
    </LocaleResource>
</Language>
```

After building the module you can press the button **Update resources** to update the newly added localized resources from your XML file.

## Say Hello

Now that you can configure the name of the person to be greeted, add another controller, a model and a view for the public action.

1. Right click on the _Controllers_ directory in the Solution Explorer.
2. Add a new class called `HelloWorldController.cs`&#x20;

{% code title="Controllers/HelloWorldController.cs" %}
```csharp
using Microsoft.AspNetCore.Mvc;
using MyOrg.HelloWorld.Models;
using MyOrg.HelloWorld.Settings;
using Smartstore.ComponentModel;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;

namespace MyOrg.HelloWorld.Controllers
{
    public class HelloWorldController : PublicController
    {
        [LoadSetting]
        public IActionResult PublicInfo(HelloWorldSettings settings)
        {
            var model = MiniMapper.Map<HelloWorldSettings, PublicInfoModel>(settings);
            return View(model);
        }
    }
}
```
{% endcode %}

1. Right click on the _Models_ directory in the Solution Explorer.
2. Add a new class called `PublicInfoModel.cs`&#x20;

{% code title="Models/PublicInfoModel.cs" %}
```csharp
using Smartstore.Web.Modelling;

namespace MyOrg.HelloWorld.Models
{
    public class PublicInfoModel : ModelBase
    {
        public string Name { get; set; }
    }
}
```
{% endcode %}

1. Right click on the _Views_ directory in the Solution Explorer.
2. Add a new folder named _HelloWorld_
3. Add a new Razor View called `PublicInfo.cshtml`&#x20;

{% code title="Views\HelloWorld\PublicInfo.cshtml" %}
```html
@model PublicInfoModel

@{
    Layout = "_Layout";
}

<div>
    Hello @Model.Name
</div>
```
{% endcode %}

The public view will be displayed when opening the URL: [http://localhost:59318/helloworld/publicInfo](http://localhost:59318/helloworld/publicInfo)&#x20;

{% hint style="success" %}
Feeling overwhelmed? Don't know why values are suddenly appearing all over the place? 😵

Try debugging your project with breakpoints. Breakpoints are a powerful tool that allows you to stop the execution of your code at specific lines, giving you the ability to inspect what's happening at each step.

By setting a breakpoint, the program will stop before that line of code is executed. From there, you can check the values of variables, step through the code line by line, and see how the state of the program changes over time. This can help you pinpoint where things might be going wrong, or simply give you a clearer understanding of how the code flows.

To set a breakpoint in Visual Studio, simply click in the margin next to the line of code where you'd like to pause, or press **F9**. Once your project hits the breakpoint, you can use **F10** to step over any line of code or **F11** to step into methods.

Debugging with breakpoints can save you a lot of time and frustration, and is a great way to build confidence in understanding how your module works from the inside out.
{% endhint %}

## Conclusion

Open the project file and remove all `ItemGroup` properties because they are not needed in the _Smartstore_ build process.

Now you have built a simple module that stores a setting and renders its value in the frontend, when accessing the route _helloworld / publicinfo_. Of course this is the starting point on the way to build more complex modules by using: Action Filters, own _DataContext_ and _View Components_, which are rendered in Widget Zones.

{% hint style="info" %}
The code for [this tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.HelloWorld) can be found in the examples repository.
{% endhint %}
