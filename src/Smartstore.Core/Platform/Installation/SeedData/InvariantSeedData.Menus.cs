using Smartstore.Core.Catalog;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Utilities;

namespace Smartstore.Core.Installation
{
    public abstract partial class InvariantSeedData
    {
        public IList<MenuEntity> Menus()
        {
            const string entityProvider = "entity";
            const string routeProvider = "route";
            const string routeTemplate = "{{\"routename\":\"{0}\"}}";

            var resourceNames = new string[] {
                "Footer.Info",
                "Footer.Service",
                "Footer.Company",
                "Manufacturers.List",
                "Admin.Catalog.Categories",
                "Products.NewProducts",
                "Products.RecentlyViewedProducts",
                "Products.Compare.List",
                "ContactUs",
                "Blog",
                "Account.Login",
                "Menu.ServiceMenu"
            };

            var settingNames = new string[]
            {
               TypeHelper.NameOf<CatalogSettings>(x => x.RecentlyAddedProductsEnabled, true),
               TypeHelper.NameOf<CatalogSettings>(x => x.RecentlyViewedProductsEnabled, true),
               TypeHelper.NameOf<CatalogSettings>(x => x.CompareProductsEnabled, true),
               TypeHelper.NameOf<CustomerSettings>(x => x.UserRegistrationType, true)
            };

            Dictionary<string, string> resources = null;
            Dictionary<string, string> settings = null;

            var menuSet = _db.Menus;
            var menuItemSet = _db.MenuItems;
            var manufacturerCount = _db.Manufacturers.Count();
            var order = 0;

            resources = _db.LocaleStringResources.AsNoTracking()
                .Where(x => x.LanguageId == _language.Id && resourceNames.Contains(x.ResourceName))
                .Select(x => new { x.ResourceName, x.ResourceValue })
                .ToList()
                .ToDictionarySafe(x => x.ResourceName, x => x.ResourceValue, StringComparer.OrdinalIgnoreCase);

            settings = _db.Settings.AsNoTracking()
                .Where(x => x.StoreId == 0 && settingNames.Contains(x.Name))
                .Select(x => new { x.Name, x.Value })
                .ToList()
                .ToDictionarySafe(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

            #region System menus

            var mainMenu = new MenuEntity
            {
                SystemName = "Main",
                IsSystemMenu = true,
                Published = true,
                Template = "Navbar",
                Title = GetResource("Admin.Catalog.Categories")
            };

            var footerInfoMenu = new MenuEntity
            {
                SystemName = "FooterInformation",
                IsSystemMenu = true,
                Published = true,
                Template = "LinkList",
                Title = "Footer - " + GetResource("Footer.Info")
            };

            var footerServiceMenu = new MenuEntity
            {
                SystemName = "FooterService",
                IsSystemMenu = true,
                Published = true,
                Template = "LinkList",
                Title = "Footer - " + GetResource("Footer.Service")
            };

            var footerCompanyMenu = new MenuEntity
            {
                SystemName = "FooterCompany",
                IsSystemMenu = true,
                Published = true,
                Template = "LinkList",
                Title = "Footer - " + GetResource("Footer.Company")
            };

            var serviceMenu = new MenuEntity
            {
                SystemName = "HelpAndService",
                IsSystemMenu = true,
                Published = true,
                Template = "Dropdown",
                Title = GetResource("Menu.ServiceMenu").NullEmpty() ?? "Service"
            };

            #endregion

            #region Main and footer menus

            mainMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = "catalog",
                Published = true
            });

            footerInfoMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("ManufacturerList"),
                Title = GetResource("Manufacturers.List"),
                DisplayOrder = ++order,
                Published = manufacturerCount > 0
            });
            footerInfoMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("RecentlyAddedProducts"),
                Title = GetResource("Products.NewProducts"),
                DisplayOrder = ++order,
                Published = GetSetting("CatalogSettings.RecentlyAddedProductsEnabled", true)
            });
            footerInfoMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("RecentlyViewedProducts"),
                Title = GetResource("Products.RecentlyViewedProducts"),
                DisplayOrder = ++order,
                Published = GetSetting("CatalogSettings.RecentlyViewedProductsEnabled", true)
            });
            footerInfoMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("CompareProducts"),
                Title = GetResource("Products.Compare.List"),
                DisplayOrder = ++order,
                Published = GetSetting("CatalogSettings.CompareProductsEnabled", true)
            });

            order = 0;

            footerServiceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("contactus"),
                Title = GetResource("ContactUs"),
                DisplayOrder = ++order
            });
            footerServiceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:shippinginfo",
                DisplayOrder = ++order
            });
            footerServiceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:paymentinfo",
                DisplayOrder = ++order
            });
            footerServiceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("CookieManager"),
                Title = "Cookie Manager",
                DisplayOrder = ++order,
                CssClass = "cookie-manager"
            });

            order = 0;

            footerCompanyMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:aboutus",
                DisplayOrder = ++order
            });
            footerCompanyMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:imprint",
                DisplayOrder = ++order
            });
            footerCompanyMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:disclaimer",
                DisplayOrder = ++order
            });
            footerCompanyMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:privacyinfo",
                DisplayOrder = ++order
            });
            footerCompanyMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:conditionsofuse",
                DisplayOrder = ++order
            });

            if (GetSetting("CustomerSettings.UserRegistrationType", "").EqualsNoCase("Disabled"))
            {
                footerCompanyMenu.Items.Add(new MenuItemEntity
                {
                    ProviderName = routeProvider,
                    Model = routeTemplate.FormatInvariant("Login"),
                    Title = GetResource("Account.Login"),
                    DisplayOrder = ++order
                });
            }

            order = 0;

            #endregion

            #region Help & Service

            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("RecentlyAddedProducts"),
                Title = GetResource("Products.NewProducts"),
                DisplayOrder = ++order,
                Published = GetSetting("CatalogSettings.RecentlyAddedProductsEnabled", true)
            });
            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("ManufacturerList"),
                Title = GetResource("Manufacturers.List"),
                DisplayOrder = ++order,
                Published = manufacturerCount > 0
            });
            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("RecentlyViewedProducts"),
                Title = GetResource("Products.RecentlyViewedProducts"),
                DisplayOrder = ++order,
                Published = GetSetting("CatalogSettings.RecentlyViewedProductsEnabled", true)
            });
            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = routeProvider,
                Model = routeTemplate.FormatInvariant("CompareProducts"),
                Title = GetResource("Products.Compare.List"),
                DisplayOrder = ++order,
                Published = GetSetting("CatalogSettings.CompareProductsEnabled", true)
            });

            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:aboutus",
                DisplayOrder = ++order,
                BeginGroup = true
            });
            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:disclaimer",
                DisplayOrder = ++order
            });
            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:shippinginfo",
                DisplayOrder = ++order
            });
            serviceMenu.Items.Add(new MenuItemEntity
            {
                ProviderName = entityProvider,
                Model = "topic:conditionsofuse",
                DisplayOrder = ++order
            });

            order = 0;

            #endregion

            #region Localization

            var removeNames = new List<string> { "Menu.ServiceMenu" };
            var removeResources = _db.LocaleStringResources.Where(x => removeNames.Contains(x.ResourceName)).ToList();
            _db.LocaleStringResources.RemoveRange(removeResources);
            _db.SaveChanges();

            #endregion

            #region Utilities

            string GetResource(string name)
            {
                return resources.TryGetValue(name, out var value) ? value : string.Empty;
            }

            T GetSetting<T>(string name, T defaultValue = default)
            {
                try
                {
                    if (settings.TryGetValue(name, out var str) && ConvertUtility.TryConvert(str, out T value))
                    {
                        return value;
                    }
                }
                catch
                {
                }

                return defaultValue;
            }

            #endregion

            var result = new List<MenuEntity> { mainMenu, footerInfoMenu, footerServiceMenu, footerCompanyMenu, serviceMenu };
            Alter(result);
            return result;
        }
    }
}
