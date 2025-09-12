# 🐥 Localizing modules

## Overview

Smartstore is an application where different languages can be easily downloaded and activated in the admin area. Over 30 languages are available for download in the backend via **Configuration > Regional Settings > Languages**. Activated languages are displayed in the frontend as well as in the backend.

Smartstore modules are shipped with localized text resources for English and German by default. The localized values are located as xml files in the modules folder _Localization_. The naming convention is `resources.{CultureCode}.xml`, e.g. `resources.de-de.xml` for German.

Resource files can also be named differently. Below are all the file names that will be searched for, using the target language with the `CultureCode` `de-de` as an example.

1. Specified language and specified region: `resources.de-de.xml`
2. Specified language and no region: `resources.de.xml`
3. Specified language and all other region variants:
   * `resources.de-at.xml`
   * `resources.de-ch.xml`
   * `resources.de-li.xml`
   * `resources.de-lu.xml`
4. American english: `resources.en.us.xml`
5. General english: `resources.en.xml`

Resource files are simple XML files whose root node is the `Language` node. This node contains several `LocaleResource` nodes, which have a unique name and a `Value` subnode for the value they contain.

```xml
<Language Name="Deutsch" IsDefault="true" IsRightToLeft="false">
    <LocaleResource Name="MyResource">
        <Value>My resource</Value>
    </LocaleResource>
	...
</Language>
```

When the application starts, each resource file of all installed modules is checked to see if all entries already exist in the database. Resources whose names do not exist in the database are added.

## ResourceRootKey & Children

The `ResourceRootKey` is set in the `module.json` file and is prepended to each resource in the XML files.

```json
"ResourceRootKey": "Plugins.MyOrg.MyModule"
```

With this `ResourceRootKey` value, the full name of the resource in the example above would be `Plugins.MyOrg.MyModule.MyResource`.

If you don't want the `ResourceRootKey` to be applied, you can use the `AppendRootKey` attribute to prevent it.

```xml
<LocaleResource Name="Plugins.MyOrg.MyModule.MyAlternateNamespace.MyResource" AppendRootKey="false">
    <Value>My resource</Value>
</LocaleResource>
```

For `LocaleResource` nodes the use of child nodes is allowed, which in turn can contain their own `LocaleResource` nodes. The names of the nodes are concatenated. The full name of the resource in the following example results in `Plugins.MyOrg.MyModule.MyExtraNamespace.MyResource`.

```xml
<LocaleResource Name="MyExtraNamespace">
    <Children>
        <LocaleResource Name="MyResource">
            <Value>My resource</Value>
        </LocaleResource>
    </Children>
</LocaleResource>
```

## Annotating Models

Of course, if there are settings in a module, they need to be labeled. We have introduced a convention for this. The textual resources associated with a setting should always have the same name as the setting itself. For example, if there is a setting for the value `MySetting` in a configuration model of a module, the resource for it should be stored as follows:

```xml
<LocaleResource Name="Plugins.MyOrg.MyModule.MySetting">
    <Value>My setting</Value>
</LocaleResource>
```

The localized value is assigned using the annotation of the property with the `LocalizedDisplay` attribute.

```csharp
[LocalizedDisplay("Plugins.MyOrg.MyModule.MySetting")]
public bool IsActive { get; set; }
```

If you now use the `smart-label` tag with the `asp-for` attribute in the Razor view of the configuration page, the value **My setting** is automatically rendered as a label.

```cshtml
<smart-label asp-for="MySetting" />
```

The `LocalizedDisplay` attribute can also be used at the class level. Since the namespace of a model's textual resources is usually the same, it's class can be designed in a cleaner way. The values of the class attribute and the property are concatenated. The full resource name from the following example is `Plugins.MyOrg.MyModule.MySetting`.

```csharp
[LocalizedDisplay("Plugins.MyOrg.MyModule.")]
public class ConfigurationModel : ModelBase
{
    [LocalizedDisplay("*MySetting")]
    public bool MySetting { get; set; }
}
```

## Display Hints

If you want to add a hint to a setting's label, describing it in more detail, use a `LocaleResource`. Name it the same as the resource for labeling the setting but add the `.Hint` suffix.

```xml
<LocaleResource Name="Plugins.MyOrg.MyModule.MySetting.Hint">
    <Value>My setting does cool things</Value>
</LocaleResource>   
```

Now, when you hover over the small question mark next to the setting, a hint appears.

Alternatively, you can define the hint explicitly by using the `sm-hint` attribute in the `smart-label` tag.

```cshtml
<smart-label asp-for="MySetting" sm-hint="Alternative way to set an explanation hint." />
```

## Enumerations

Textual resources for the values of an enumeration are stored a bit differently. They consist of the literal `Enums`, then the name of the enumeration, followed by the name of the enumeration member (e.g. `Enums.MyEnum.Value1`).

Let's take the following enumeration:

```csharp
public enum MyEnum
{
    Value1,
    Value2
}  
```

This would be localized as follows:

```xml
<LocaleResource Name="Enums" AppendRootKey="false">
    <Children>
        <LocaleResource Name="MyEnum.Value1">
            <Value>Value 1</Value>
        </LocaleResource>
        <LocaleResource Name="MyEnum.Value2">
            <Value>Value 2</Value>
        </LocaleResource>
    </Children>
</LocaleResource>
```

And accessed like this:

```cshtml
<select asp-for="MyEnumSetting" asp-items="Html.GetLocalizedEnumSelectList(typeof(MyEnum))"></select>
```

## FriendlyName & Description

A resource file should also contain the localized name of the module and a description that will be displayed in the Module-Manager when the module is installed.

These are composed of the literal `Plugins.FriendlyName` for the name of the module or `Plugins.Description` for the description, followed by the system name of the module as specified in `module.json`.

```xml
<LocaleResource Name="Plugins.FriendlyName.MyOrg.MyModule" AppendRootKey="false">
    <Value>My module</Value>
</LocaleResource>  

<LocaleResource Name="Plugins.Description.MyOrg.MyModule" AppendRootKey="false">
    <Value>This module is for demonstration purposes only.</Value>
</LocaleResource>  
```

## PageBuilder

If a PageBuilder block is implemented in a module, you must store the label of the block that is displayed in the Pagebuilder according to the following convention.

```xml
<LocaleResource Name="BlockMetadata.FriendlyName.MyBlock" AppendRootKey="false">
    <Value>My block</Value>
</LocaleResource>
```

## Permissions

When a module implements its own `PermissionProvider`, the permission labels must be stored according to the following convention. We'll use this sample `PermissionProvider` implementation to illustrate.

```csharp
public static class MyModulePermissions
{
    public const string Self = "mymodule";
    public const string Read = "mymodule.read";
    public const string Update = "mymodule.update";
    public const string Create = "mymodule.create";
    public const string Delete = "mymodule.delete";

    public const string AnotherPermission = "mymodule.anotherpermission";
}
```

`Self` defines the namespace for all permissions used by the module. It also represents the main permission node of the module in the store backend, under which all other permissions are grouped.

Use this code to localize that node.

```xml
<LocaleResource Name="Plugins.Permissions.DisplayName.MyModule" AppendRootKey="false">
    <Value>My module</Value>
</LocaleResource>
```

There is no need to provide resources for common permissions such as `read`, `update`, `create`, and `delete`. They are resolved automatically, unlike special permissions such as `anotherpermission`, which are localized as follows:

```xml
<LocaleResource Name="Plugins.Permissions.DisplayName.AnotherPermission" AppendRootKey="false">
    <Value>Another permission</Value>
</LocaleResource>
```

## Shortcut T

In all views, localized resources can be displayed via the _T-helper_. In a Razor view, the following statement can be used anywhere.

```cshtml
@T("Plugins.MyOrg.MyModule.MyResource")
```
