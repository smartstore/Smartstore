using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Infrastructure.Menus
{
    public partial class SettingsMenu : MenuBase
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

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

            root.Append(new MenuItem
            {
                Id = "general",
                Text = T("Admin.Common.General"),
                IconLibrary = "bi",
                Icon = "sliders",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "GeneralCommon"
            });

            root.Append(new MenuItem
            {
                Id = "catalog",
                Text = T("Admin.Catalog"),
                IconLibrary = "bi",
                Icon = "box",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Catalog"
            });

            root.Append(new MenuItem
            {
                Id = "search",
                Text = T("Search.Title"),
                IconLibrary = "bi",
                Icon = "search",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Search"
            });

            root.Append(new MenuItem
            {
                Id = "customer",
                Text = T("Admin.Customers"),
                IconLibrary = "bi",
                Icon = "person",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "CustomerUser"
            });

            root.Append(new MenuItem
            {
                Id = "cart",
                Text = T("ShoppingCart"),
                IconLibrary = "bi",
                Icon = "cart",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "ShoppingCart"
            });

            root.Append(new MenuItem
            {
                Id = "order",
                Text = T("Admin.Orders"),
                IconLibrary = "bi",
                Icon = "graph-up",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Order"
            });

            root.Append(new MenuItem
            {
                Id = "payment",
                Text = T("Admin.Configuration.Payment"),
                IconLibrary = "bi",
                Icon = "credit-card",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Payment"
            });

            root.Append(new MenuItem
            {
                Id = "tax",
                Text = T("Admin.Plugins.KnownGroup.Tax"),
                IconLibrary = "bi",
                Icon = "percent",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Tax"
            });

            root.Append(new MenuItem
            {
                Id = "shipping",
                Text = T("Admin.Configuration.Shipping"),
                IconLibrary = "bi",
                Icon = "truck",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Shipping"
            });

            root.Append(new MenuItem
            {
                Id = "reward-points",
                Text = T("Account.RewardPoints"),
                IconLibrary = "bi",
                Icon = "trophy",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "RewardPoints"
            });

            root.Append(new MenuItem
            {
                Id = "media",
                Text = T("Admin.Plugins.KnownGroup.Media"),
                IconLibrary = "bi",
                Icon = "images",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Media"
            });

            root.Append(new MenuItem
            {
                Id = "dataexchange",
                Text = T("Admin.Common.DataExchange"),
                IconLibrary = "bi",
                Icon = "arrow-left-right",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "DataExchange"
            });

            root.Append(new MenuItem
            {
                IsGroupHeader = true,
                Id = "all",
                Text = T("Admin.Configuration.Settings.AllSettings"),
                IconLibrary = "bi",
                Icon = "gear",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "AllSettings"
            });

            // Add area = "Admin" to all items in one go.
            foreach (var item in root.Children)
            {
                item.Value.RouteValues["area"] = "Admin";
                item.Id = item.Value.Id;
            }

            return Task.FromResult(root);
        }
    }
}
