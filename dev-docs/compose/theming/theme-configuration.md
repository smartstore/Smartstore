# 🐥 Theme configuration

## Overview

The _theme.config_ file is the manifest file for a theme. It contains all the theme information the system needs and can include theme variables. If a subdirectory of _/Smartstore/src/Smartstore.Web/Themes_ exists in the file system and contains a theme.config file, Smartstore interprets this directory as a theme directory.

Here is an example of a theme.config file without variables.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Theme baseTheme="Flex" title="My Theme" description="'My Company' default theme" author="MyOrg" version="1.0">
    <Vars>
        
    </Vars>
</Theme>
```

These are the attributes you can use in the `Theme`-node:

<table><thead><tr><th width="200">Attribute</th><th>Description</th></tr></thead><tbody><tr><td><strong>baseTheme</strong></td><td>Specifies which base theme to use.</td></tr><tr><td><strong>title</strong></td><td>Defines the title of the theme.</td></tr><tr><td><strong>description</strong></td><td>Defines the description of the theme as it appears in the <em>Theme Configurator</em>.</td></tr><tr><td><strong>author</strong></td><td>Defines the author of the theme.</td></tr><tr><td><strong>version</strong></td><td>Defines the version of the theme.</td></tr></tbody></table>

In addition to the above attributes, a preview file must be stored to display the theme as a preview image in the theme configurator. This file must be located in the _wwwroot/images_ directory and be named `preview.png`. Any changes to `theme.config` will invalidate the cached stylesheet and cause Sass to recompile. As a result, changed variable values are applied right after a browser refresh at runtime, and the changes are immediately visible.

## Variables

Theme variables are configurable values provided by a theme that can be customized by the end user in the Theme Configurator. This allows you to quickly customize the look and feel of your store by defining colors, fonts, font sizes and weights, margins, border radius, and more. Each theme can define its own variables, with default values stored to best match the look and feel of the theme.

### Usage in Sass

Variables are made available to other Sass files by including the virtual Sass file _/.app/themevars.scss_. This file translates each theme variable into a Sass variable. To do this, use the following include in your `theme.scss` file. This should be the first file included in `theme.scss` so that the variables are available in other included Sass files.

```scss
@import '/.app/themevars.scss'; 
```

#### Example

A variable from `theme.config`

```xml
<Var name="my-var" type="String">1rem</Var>
```

Can be used in Sass like this

```scss
font-size: $my-var;
```

### Usage in Razor

You can use the `Display` helper to get access to theme variables in Razor views.

```cshtml
<style>
    .my-selector {
        font-size: @Display.GetThemeVariable("my-var")
    }
</style>
```

### Naming Convention

Theme variables are written in kebab case, also known as _hyphen-separated lowercase_ or _spinal case_. This convention uses hyphens to separate words in a variable's lowercase name. For example, a kebab case variable name might be `header-font-size` or `table-border-radius`.

### Variable types

The following types are available as theme variables:

```xml
String	<Var name="border-radius" type="String">0.25rem</Var>
Color	<Var name="body-bg" type="Color">#fff</Var>
Number	<Var name="todo" type="Number">10</Var>
Boolean	<Var name="inverse-menubar" type="Boolean">false</Var>
Select	<Var name="shopbar-label" type="Select#brand-colors">warning</Var>
```

To provide values through a select box, it is necessary to declare the selectable values outside the `Vars` node:

```xml
        ...
        </Vars>
        <Selects>
            <Select id="brand-colors">
                <Option>primary</Option>
                <Option>secondary</Option>
                <Option>info</Option>
                <Option>success</Option>
                <Option>warning</Option>
                <Option>danger</Option>
                <Option>light</Option>
                <Option>dark</Option>
        </Select>
    </Selects>
</Theme>
```

### Variable values

The value of a variable can be a simple string literal (e.g. `0.25rem` or `#fff`), a Sass variable (e.g. `$gray-100`), or even a Sass statement (e.g. `lighten($gray-200, 2%)`).

### Variable definition

In `theme.config`, you can introduce your own variables for themes, or override existing base theme variables. However, it is not mandatory to specify theme variables. If no variables are specified in `theme.config`, all theme variables are inherited from the base theme. This is useful if you only want to overwrite files at the theme level.

#### **Override variables**

If you want to override variables defined in the base theme, simply declare them in theme.config with the same name as the base theme variable and change the value accordingly.

Here is an example:

If you inherited the _Flex_ theme that has a `border-radius` variable set to `0.25rem`, you can set a new default value for this variable by adding the following entry to the `Vars` node in your `theme.config` file.

```xml
<Var name="border-radius" type="String">0.35rem</Var>
```

#### **Introducing variables & making them configurable**

New variables are added to the `Vars` node of `theme.config` exactly as described above. If they are to be changed by the end user, they must be defined in the `ConfigureTheme` Razor view. For this purpose, a Razor view named `ConfigureTheme.cshtml` is stored in the _Views/Shared/_ directory.

In addition to making your own variables configurable, you also need to make the variables of the base theme configurable. The best way to do this is to copy the base theme file into your own theme and add your custom variables to the appropriate places.

We wrote a helper function in `ConfigureTheme.cshtml` for the output of the controls, with which it is possible to render the editor for a variable in one line:

```cshtml
ThemeVarEditor("login-box-bg");
```

This function takes the name of the variable for which an editor is to be generated. Optionally, a group name can be specified. If the group name is specified, a heading for the group of the current and all subsequent variables is placed in front of the editor. The last optional parameter the function accepts is a help text describing the use of the variable.

The HTML helper `ThemeVarLabel` is used to render the label. The actual variable control is rendered by the HTML helper `ThemeVarEditor`. It determines the control to render based on the variable type (e.g. color picker for color type variables, dropdown box for select types & simple input elements for the rest).

Finally, the HTML helper `ThemeVarChainInfo` is used to display information about the theme's inheritance chain, so that the user knows where the value of the variable comes from.
