using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Infrastructure.Menus
{
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

            root.AppendRange(new[] 
            {
                new MenuItem
                {
                    Id = "general",
                    Text = T("Admin.Common.General"),
                    IconLibrary = "bi",
                    Icon = "sliders",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "GeneralCommon"
                },
                new MenuItem
                {
                    Id = "catalog",
                    Text = T("Admin.Catalog"),
                    IconLibrary = "bi",
                    Icon = "box",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "Catalog"
                },
                new MenuItem
                {
                    Id = "search",
                    Text = T("Search.Title"),
                    IconLibrary = "bi",
                    Icon = "search",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "Search"
                },
                new MenuItem
                {
                    Id = "customer",
                    Text = T("Admin.Customers"),
                    IconLibrary = "bi",
                    Icon = "person",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "CustomerUser"
                },
                new MenuItem
                {
                    Id = "cart",
                    Text = T("ShoppingCart"),
                    IconLibrary = "bi",
                    Icon = "cart",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "ShoppingCart"
                },
                new MenuItem
                {
                    Id = "order",
                    Text = T("Admin.Orders"),
                    IconLibrary = "bi",
                    Icon = "graph-up",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "Order"
                },
                new MenuItem
                {
                    Id = "payment",
                    Text = T("Admin.Configuration.Payment"),
                    IconLibrary = "bi",
                    Icon = "credit-card",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "Payment"
                },
                new MenuItem
                {
                    Id = "finance",
                    Text = T("Common.Finance"),
                    IconLibrary = "bi",
                    Icon = "percent",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "Finance"
                },
                new MenuItem
                {
                    Id = "shipping",
                    Text = T("Admin.Configuration.Shipping"),
                    IconLibrary = "bi",
                    Icon = "truck",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "Shipping"
                },
                new MenuItem
                {
                    Id = "reward-points",
                    Text = T("Account.RewardPoints"),
                    IconLibrary = "bi",
                    Icon = "trophy",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "RewardPoints"
                },
                new MenuItem
                {
                    Id = "media",
                    Text = T("Admin.Plugins.KnownGroup.Media"),
                    IconLibrary = "bi",
                    Icon = "images",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "Media"
                },
                new MenuItem
                {
                    Id = "dataexchange",
                    Text = T("Admin.Common.DataExchange"),
                    IconLibrary = "bi",
                    Icon = "arrow-left-right",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "DataExchange"
                },
                new MenuItem
                {
                    IsGroupHeader = true,
                    Id = "all",
                    Text = T("Admin.Configuration.Settings.AllSettings"),
                    IconLibrary = "bi",
                    Icon = "gear",
                    PermissionNames = perm,
                    ControllerName = "Setting",
                    ActionName = "AllSettings"
                }
            });

            // Add area = "Admin" to all items in one go.
            foreach (var item in root.Children)
            {
                item.Value.RouteValues["area"] = "Admin";
            }

            return Task.FromResult(root);
        }
    }
}
