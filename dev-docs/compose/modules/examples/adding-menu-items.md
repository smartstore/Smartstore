# ✔ Adding menu items

Menus are UI components that make it easy for users to find and navigate to specific or even undiscovered content and features in the store. Building on the previous tutorial, you will add a menu item to the Admin menu.

{% hint style="info" %}
For a more in-depth look at menus, see [Menus](../../../framework/content/menus.md).
{% endhint %}

## Adding a menu

First, you create the `AdminMenu.cs` class in the root folder of your module.

```csharp
namespace MyOrg.HelloWorld
{
    public class AdminMenu
    {
    }
}
```

### Implement the `AdminMenuProvider`

This class inherits from the abstract base class `AdminMenuProvider` and overrides the `BuildMenuCore` method.

```csharp
public class AdminMenu : AdminMenuProvider
{
    protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
    {
    }
}
```

### Create a menu item

Next, you create a `MenuItem` using the `ToBuilder()` method.

```csharp
var myMenuItem = new MenuItem().ToBuilder()
    .ResKey("Plugins.MyOrg.HelloWorld.MyMenuItem")
    .Icon("gear", "bi")
    .Action("Configure", "HelloWorldAdmin", new { area = "Admin" })
    .AsItem();
```

* `ResKey` is a reference to the resource in your localization files.
* `Icon` adds a menu icon.
*   `Action` creates a route to your action, defined in the specified controller.

    This menu will simply point to your `Configure` method from the `HelloWorldAdminController`.

### Add a localization string

Add a new resource to your localization to display the menu text.

```xml
<LocaleResource Name="MyMenuItem">
    <Value>Configure Module</Value>
</LocaleResource>
```

### Create tree nodes

Create a `TreeNode` _menuNode_ from `myMenuItem`.

```csharp
var menuNode = new TreeNode<MenuItem>(myMenuItem);
```

Get a reference node from `modulesNode`. This example uses the menu id `settings`.

```csharp
var refNode = modulesNode.Root.SelectNodeById("settings");
```

{% hint style="info" %}
For more menu items:

* Open the Smartstore Admin page
* Right-click on the desired menu
* Select the _Inspector_
* Search for "**data-id**"
{% endhint %}

Insert `menuNode` after `refNode`.

```csharp
menuNode.InsertAfter(refNode);
```

Now you should see a menu item in the admin configuration menu. Your code should look something like this:

{% code title="AdminMenu.cs" %}
```csharp
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace MyOrg.HelloWorld
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            var myMenuItem = new MenuItem().ToBuilder()
                .ResKey("Plugins.MyOrg.HelloWorld.MyMenuItem")
                .Icon("gear", "bi")
                .Action("Configure", "HelloWorldAdmin", new { area = "Admin" })
                .AsItem();

            var menuNode = new TreeNode<MenuItem>(myMenuItem);
            var refNode = modulesNode.Root.SelectNodeById("settings");
            menuNode.InsertAfter(refNode);
        }
    }
}
```
{% endcode %}

## Adding a submenu

Adding a submenu is very easy. First, you add a menu, just as you did above. The only difference is that you don't give the menu item an action.

```csharp
var secondMenuItem = new MenuItem().ToBuilder()
    .ResKey("Plugins.MyOrg.HelloWorld.MySecondMenuItem")
    .AsItem();
```

Then create another menu item and specify the action.

```csharp
var subMenuItem = new MenuItem().ToBuilder()
    .ResKey("Plugins.MyOrg.HelloWorld.MySubMenuItem")
    .Action("Configure", "HelloWorldAdmin", new { area = "Admin" })
    .AsItem();
```

### Add localization strings

Add two new resources to your localization.

```xml
<LocaleResource Name="MySecondMenuItem">
    <Value>Hello World</Value>
</LocaleResource>
<LocaleResource Name="MySubMenuItem">
    <Value>Another way to configure</Value>
</LocaleResource>
```

### Create tree nodes

Again, create a `TreeNode` for each menu item.

```csharp
var secondMenuNode = new TreeNode<MenuItem>(secondMenuItem);
var subMenuNode = new TreeNode<MenuItem>(subMenuItem);
```

Then use `menuNode` to insert and append the new menu items.

```csharp
secondMenuNode.InsertAfter(menuNode);
secondMenuNode.Append(subMenuNode);
```

Your final code should look like this:

```csharp
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Builders;

namespace MyOrg.HelloWorld
{
    public class AdminMenu : AdminMenuProvider
    {
        protected override void BuildMenuCore(TreeNode<MenuItem> modulesNode)
        {
            var myMenuItem = new MenuItem().ToBuilder()
                .ResKey("Plugins.MyOrg.HelloWorld.MyMenuItem")
                .Icon("gear", "bi")
                .Action("Configure", "HelloWorldAdmin", new { area = "Admin" })
                .AsItem();

            var menuNode = new TreeNode<MenuItem>(myMenuItem);
            var refNode = modulesNode.Root.SelectNodeById("settings");
            menuNode.InsertAfter(refNode);

            var secondMenuItem = new MenuItem().ToBuilder()
                .ResKey("Plugins.MyOrg.HelloWorld.MySecondMenuItem")
                .AsItem();
            var subMenuItem = new MenuItem().ToBuilder()
                .ResKey("Plugins.MyOrg.HelloWorld.MySubMenuItem")
                .Action("Configure", "HelloWorldAdmin", new { area = "Admin" })
                .AsItem();

            var secondMenuNode = new TreeNode<MenuItem>(secondMenuItem);
            var subMenuNode = new TreeNode<MenuItem>(subMenuItem);

            secondMenuNode.InsertAfter(menuNode);
            secondMenuNode.Append(subMenuNode);
        }
    }
}
```

## Conclusion

In this tutorial, you created a menu item, added it to the Admin menu, and added a submenu.

{% hint style="info" %}
The code for [this tutorial](https://github.com/smartstore/dev-docs-code-examples/tree/main/src/MyOrg.MenuTutorial) can be found in the examples repository.
{% endhint %}
