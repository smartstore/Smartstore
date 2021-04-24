using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Engine;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Web.Infrastructure
{
    public partial class AdminMenu : MenuBase
    {
        public Localizer T { get; set; }

        public override string Name => "Admin";

        public override bool ApplyPermissions => true;

        protected override string GetCacheKey()
        {
            var cacheKey = "{0}-{1}".FormatInvariant(
                Services.WorkContext.WorkingLanguage.Id,
                Services.WorkContext.CurrentCustomer.GetRolesIdent());

            return cacheKey;
        }

        protected override Task<TreeNode<MenuItem>> BuildAsync()
        {
            var root = new TreeNode<MenuItem>(new MenuItem { Text = "home" })
            {
                Id = "home"
            };

            var dashboard = new MenuItem().ToBuilder()
                .Id("dashboard")
                .ResKey("Admin.Dashboard")
                .Icon("icm icm-home")
                .Action("Index", "Home", new { area = "Admin" })
                .AsItem();

            var catalog = new MenuItem().ToBuilder()
                .Id("catalog")
                .ResKey("Admin.Catalog")
                .Icon("icm icm-cube")
                .PermissionNames(new[] 
                {
                    Permissions.Catalog.Product.Self,
                    Permissions.Catalog.ProductReview.Self,
                    Permissions.Catalog.Category.Self,
                    Permissions.Catalog.Manufacturer.Self,
                    Permissions.Catalog.Variant.Self,
                    Permissions.Catalog.Attribute.Self,
                    Permissions.Cart.CheckoutAttribute.Self
                })
                .AsItem();

            var sales = new MenuItem().ToBuilder()
                .Id("sales")
                .ResKey("Admin.Sales")
                .Icon("icm icm-chart-growth")
                .PermissionNames(Permissions.Order.Self)
                .AsItem();

            var users = new MenuItem().ToBuilder()
                .Id("users")
                .ResKey("Admin.Customers")
                .Icon("icm icm-users2")
                .PermissionNames(new[]
                {
                    Permissions.Customer.Self,
                    Permissions.Configuration.Authentication.Self,
                    Permissions.Configuration.ActivityLog.Self
                })
                .AsItem();

            var promotions = new MenuItem().ToBuilder()
                .Id("promotions")
                .ResKey("Admin.Promotions")
                .Icon("icm icm-bullhorn")
                .PermissionNames(Permissions.Promotion.Self)
                .AsItem();

            var cms = new MenuItem().ToBuilder()
                .Id("cms")
                .ResKey("Admin.ContentManagement")
                .Icon("icm icm-site-map")
                .PermissionNames(Permissions.Cms.Self)
                .AsItem();

            root.AppendRange(new[] { dashboard, catalog, sales, users, promotions, cms });

            return Task.FromResult(root);
        }
    }
}
