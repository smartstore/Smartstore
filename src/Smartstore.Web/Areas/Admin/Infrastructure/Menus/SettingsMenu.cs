using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Infrastructure.Menus;

public partial class SettingsMenu : MenuBase
{
    public override string Name => "Settings";

    protected override string GetCacheKey()
    {
        var cacheKey = "{0}-{1}".FormatInvariant(
            Services.WorkContext.WorkingLanguage.Id,
            Services.WorkContext.CurrentCustomer.GetRolesIdent());

        return cacheKey;
    }

    protected override Task<TreeNode<MenuItem>> BuildAsync(CacheEntryOptions cacheEntryOptions)
    {
        var store = Services.StoreContext.CurrentStore;
        var customer = Services.WorkContext.CurrentCustomer;
        var perm = Permissions.Configuration.Setting.Read;

        var root = new TreeNode<MenuItem>(new MenuItem { Text = T("Admin.Configuration.Settings") })
        {
            Id = Name
        };

        root.AppendRange(
        [
            new MenuItem
            {
                Id = "general",
                Text = T("Admin.Common.General"),
                Icon = "sliders",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "GeneralCommon"
            },
            new MenuItem
            {
                Id = "catalog",
                Text = T("Admin.Catalog"),
                Icon = "box",
                PermissionNames = perm,
                ControllerName = "Product",
                ActionName = "CatalogSettings"
            },
            new MenuItem
            {
                Id = "search",
                Text = T("Search.Title"),
                Icon = "search",
                PermissionNames = perm,
                ControllerName = "Search",
                ActionName = "SearchSettings"
            },
            new MenuItem
            {
                Id = "customer",
                Text = T("Admin.Customers"),
                Icon = "user",
                PermissionNames = perm,
                ControllerName = "Customer",
                ActionName = "CustomerUserSettings"
            },
            new MenuItem
            {
                Id = "cart",
                Text = T("ShoppingCart"),
                Icon = "cart",
                PermissionNames = perm,
                ControllerName = "ShoppingCart",
                ActionName = "ShoppingCartSettings"
            },
            new MenuItem
            {
                Id = "order",
                Text = T("Admin.Orders"),
                Icon = "chart-up",
                PermissionNames = perm,
                ControllerName = "Order",
                ActionName = "OrderSettings"
            },
            new MenuItem
            {
                Id = "payment",
                Text = T("Admin.Configuration.Payment"),
                Icon = "creditcard",
                PermissionNames = perm,
                ControllerName = "Payment",
                ActionName = "PaymentSettings"
            },
            new MenuItem
            {
                Id = "finance",
                Text = T("Common.Finance"),
                Icon = "discount",
                PermissionNames = perm,
                ControllerName = "Tax",
                ActionName = "FinanceSettings"
            },
            new MenuItem
            {
                Id = "shipping",
                Text = T("Admin.Configuration.Shipping"),
                Icon = "truck",
                PermissionNames = perm,
                ControllerName = "Shipping",
                ActionName = "ShippingSettings"
            },
            new MenuItem
            {
                Id = "reward-points",
                Text = T("Account.RewardPoints"),
                Icon = "trophy",
                PermissionNames = perm,
                ControllerName = "Customer",
                ActionName = "RewardPointsSettings"
            },
            new MenuItem
            {
                Id = "media",
                Text = T("Admin.Plugins.KnownGroup.Media"),
                Icon = "images",
                PermissionNames = perm,
                ControllerName = "Media",
                ActionName = "MediaSettings"
            },
            new MenuItem
            {
                Id = "dataexchange",
                Text = T("Admin.Common.DataExchange"),
                Icon = "exchange",
                PermissionNames = perm,
                ControllerName = "Import",
                ActionName = "DataExchangeSettings"
            },
            new MenuItem
            {
                Id = "performance",
                Text = T("Admin.Configuration.Settings.Performance"),
                Icon = "bi:speedometer2!",
                PermissionNames = perm,
                ControllerName = "Maintenance",
                ActionName = "PerformanceSettings"
            },
            new MenuItem
            {
                IsGroupHeader = true,
                Id = "all",
                Text = T("Admin.Configuration.Settings.AllSettings"),
                Icon = "gear",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "AllSettings"
            }
        ]);

        // Add area = "Admin" to all items in one go.
        foreach (var item in root.Children)
        {
            item.Value.RouteValues["area"] = "Admin";
        }

        return Task.FromResult(root);
    }
}