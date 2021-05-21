using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;

namespace Smartstore.Web.Infrastructure.Menus
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
                Icon = "fa fa-fw fa-sliders-h",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "GeneralCommon"
            });

            root.Append(new MenuItem
            {
                Id = "catalog",
                Text = T("Admin.Catalog"),
                Icon = "fas fa-fw fa-book",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Catalog"
            });

            root.Append(new MenuItem
            {
                Id = "search",
                Text = T("Search.Title"),
                Icon = "far fa-fw fa-search",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Search"
            });

            root.Append(new MenuItem
            {
                Id = "customer",
                Text = T("Admin.Customers"),
                Icon = "fa fa-fw fa-users",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "CustomerUser"
            });

            root.Append(new MenuItem
            {
                Id = "cart",
                Text = T("ShoppingCart"),
                Icon = "fa fa-fw fa-shopping-cart",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "ShoppingCart"
            });

            root.Append(new MenuItem
            {
                Id = "order",
                Text = T("Admin.Orders"),
                Icon = "fa fa-fw fa-chart-bar",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Order"
            });

            root.Append(new MenuItem
            {
                Id = "payment",
                Text = T("Admin.Configuration.Payment"),
                Icon = "far fa-fw fa-credit-card",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Payment"
            });

            root.Append(new MenuItem
            {
                Id = "tax",
                Text = T("Admin.Plugins.KnownGroup.Tax"),
                Icon = "fa fa-fw fa-percent",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Tax"
            });

            root.Append(new MenuItem
            {
                Id = "shipping",
                Text = T("Admin.Configuration.Shipping"),
                Icon = "fa fa-fw fa-truck",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Shipping"
            });

            root.Append(new MenuItem
            {
                Id = "reward-points",
                Text = T("Account.RewardPoints"),
                Icon = "fa fa-fw fa-trophy",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "RewardPoints"
            });

            root.Append(new MenuItem
            {
                Id = "media",
                Text = T("Admin.Plugins.KnownGroup.Media"),
                Icon = "far fa-fw fa-image",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "Media"
            });

            // TODO: (mh) (core) Blog, News & Forum must be ported in corresponding module.
            //root.Append(new MenuItem
            //{
            //    Id = "blog",
            //    Text = T("Blog"),
            //    Icon = "far fa-fw fa-edit",
            //    PermissionNames = perm,
            //    ControllerName = "Setting",
            //    ActionName = "Blog"
            //});

            //root.Append(new MenuItem
            //{
            //    Id = "news",
            //    Text = T("News"),
            //    Icon = "far fa-fw fa-rss",
            //    PermissionNames = perm,
            //    ControllerName = "Setting",
            //    ActionName = "News"
            //});

            //root.Append(new MenuItem
            //{
            //    Id = "forum",
            //    Text = T("Forum.Forums"),
            //    Icon = "fa fa-fw fa-users",
            //    PermissionNames = perm,
            //    ControllerName = "Setting",
            //    ActionName = "Forum"
            //});

            root.Append(new MenuItem
            {
                Id = "dataexchange",
                Text = T("Admin.Common.DataExchange"),
                Icon = "fa fa-fw fa-exchange-alt",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "DataExchange"
            });

            root.Append(new MenuItem
            {
                IsGroupHeader = true,
                Id = "all",
                Text = T("Admin.Configuration.Settings.AllSettings"),
                Icon = "fa fa-fw fa-cogs",
                PermissionNames = perm,
                ControllerName = "Setting",
                ActionName = "AllSettings"
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
