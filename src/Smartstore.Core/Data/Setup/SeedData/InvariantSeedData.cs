using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messages;
using Smartstore.Core.Rules;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Core.Theming;
using Smartstore.Domain;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;

namespace Smartstore.Core.Data.Setup
{
    public abstract partial class InvariantSeedData
    {
        private SmartDbContext _db;
        private string _sampleImagesPath;

        protected InvariantSeedData()
        {
        }

        public void Initialize(SmartDbContext db)
        {
            _db = db;
            _sampleImagesPath = CommonHelper.MapPath("~/App_Data/Samples/");
        }

        #region Mandatory data creators

        public List<MediaFile> Pictures()
        {
            var entities = new List<MediaFile>
            {
                CreatePicture("company-logo.png"),
                CreatePicture("product/allstar_charcoal.jpg"),
                CreatePicture("product/allstar_maroon.jpg"),
                CreatePicture("product/allstar_navy.jpg"),
                CreatePicture("product/allstar_purple.jpg"),
                CreatePicture("product/allstar_white.jpg"),
                CreatePicture("product/wayfarer_havana.png"),
                CreatePicture("product/wayfarer_havana_black.png"),
                CreatePicture("product/wayfarer_rayban-black.png")
            };

            Alter(entities);
            return entities;
        }

        public async Task<List<Store>> Stores()
        {
            var imgCompanyLogo = await _db.MediaFiles.Where(x => x.Name == "company-logo.png").FirstOrDefaultAsync();

            var currency = await _db.Currencies.FirstOrDefaultAsync(x => x.CurrencyCode == "EUR");
            if (currency == null)
            {
                currency = await _db.Currencies.FirstAsync();
            }

            var entities = new List<Store>()
            {
                new Store()
                {
                    Name = "Your store name",
                    Url = "http://www.yourStore.com/",
                    Hosts = "yourstore.com,www.yourstore.com",
                    SslEnabled = false,
                    DisplayOrder = 1,
                    LogoMediaFileId = imgCompanyLogo.Id,
                    PrimaryStoreCurrencyId = currency.Id,
                    PrimaryExchangeRateCurrencyId = currency.Id
                }
            };

            Alter(entities);
            return entities;
        }

        public List<MeasureDimension> MeasureDimensions()
        {
            var entities = new List<MeasureDimension>()
            {
                new MeasureDimension()
                {
                    Name = "millimetre",
                    SystemKeyword = "mm",
                    Ratio = 25.4M,
                    DisplayOrder = 1,
                },
                new MeasureDimension()
                {
                    Name = "centimetre",
                    SystemKeyword = "cm",
                    Ratio = 0.254M,
                    DisplayOrder = 2,
                },
                new MeasureDimension()
                {
                    Name = "meter",
                    SystemKeyword = "m",
                    Ratio = 0.0254M,
                    DisplayOrder = 3,
                },
                new MeasureDimension()
                {
                    Name = "in",
                    SystemKeyword = "inch",
                    Ratio = 1M,
                    DisplayOrder = 4,
                },
                new MeasureDimension()
                {
                    Name = "feet",
                    SystemKeyword = "ft",
                    Ratio = 0.08333333M,
                    DisplayOrder = 5,
                }
            };

            Alter(entities);
            return entities;
        }

        public List<MeasureWeight> MeasureWeights()
        {
            var entities = new List<MeasureWeight>()
            {
                new MeasureWeight()
                {
                    Name = "ounce", // Ounce, Unze
					SystemKeyword = "oz",
                    Ratio = 16M,
                    DisplayOrder = 5,
                },
                new MeasureWeight()
                {
                    Name = "lb", // Pound
					SystemKeyword = "lb",
                    Ratio = 1M,
                    DisplayOrder = 6,
                },

                new MeasureWeight()
                {
                    Name = "kg",
                    SystemKeyword = "kg",
                    Ratio = 0.45359237M,
                    DisplayOrder = 1,
                },
                new MeasureWeight()
                {
                    Name = "gram",
                    SystemKeyword = "g",
                    Ratio = 453.59237M,
                    DisplayOrder = 2,
                },
                new MeasureWeight()
                {
                    Name = "liter",
                    SystemKeyword = "l",
                    Ratio = 0.45359237M,
                    DisplayOrder = 3,
                },
                new MeasureWeight()
                {
                    Name = "milliliter",
                    SystemKeyword = "ml",
                    Ratio = 0.45359237M,
                    DisplayOrder = 4,
                }
            };

            Alter(entities);
            return entities;
        }

        protected virtual string TaxNameBooks => "Books";
        protected virtual string TaxNameDigitalGoods => "Downloadable Products";
        protected virtual string TaxNameJewelry => "Jewelry";
        protected virtual string TaxNameApparel => "Apparel & Shoes";
        protected virtual string TaxNameFood => "Food";
        protected virtual string TaxNameElectronics => "Electronics & Software";
        protected virtual string TaxNameTaxFree => "Tax free";
        public virtual decimal[] FixedTaxRates => new decimal[] { 0, 0, 0, 0, 0 };

        public List<TaxCategory> TaxCategories()
        {
            var entities = new List<TaxCategory>
            {
                new TaxCategory
                {
                    Name = TaxNameTaxFree,
                    DisplayOrder = 0,
                },
                new TaxCategory
                {
                    Name = TaxNameBooks,
                    DisplayOrder = 1,
                },
                new TaxCategory
                {
                    Name = TaxNameElectronics,
                    DisplayOrder = 5,
                },
                new TaxCategory
                {
                    Name = TaxNameDigitalGoods,
                    DisplayOrder = 10,
                },
                new TaxCategory
                {
                    Name = TaxNameJewelry,
                    DisplayOrder = 15,
                },
                new TaxCategory
                {
                    Name = TaxNameApparel,
                    DisplayOrder = 20,
                }
            };

            Alter(entities);
            return entities;
        }

        public List<Currency> Currencies()
        {
            var entities = new List<Currency>()
            {
                CreateCurrency("en-US", published: true, rate: 1M, order: 0),
                CreateCurrency("en-GB", published: true, rate: 0.61M, order: 5),
                CreateCurrency("en-AU", published: true, rate: 0.94M, order: 10),
                CreateCurrency("en-CA", published: true, rate: 0.98M, order: 15),
                CreateCurrency("de-DE", rate: 0.79M, order: 20/*, formatting: string.Format("0.00 {0}", "\u20ac")*/),
                CreateCurrency("de-CH", rate: 0.93M, order: 25, formatting: "CHF #,##0.00"),
                CreateCurrency("zh-CN", rate: 6.48M, order: 30),
                CreateCurrency("zh-HK", rate: 7.75M, order: 35),
                CreateCurrency("ja-JP", rate: 80.07M, order: 40),
                CreateCurrency("ru-RU", rate: 27.7M, order: 45),
                CreateCurrency("tr-TR", rate: 1.78M, order: 50),
                CreateCurrency("sv-SE", rate: 6.19M, order: 55)
            };

            Alter(entities);
            return entities;
        }

        public List<ShippingMethod> ShippingMethods(bool includeSamples)
        {
            var entities = new List<ShippingMethod>
            {
                new ShippingMethod
                {
                    Name = "In-Store Pickup",
                    Description ="Pick up your items at the store",
                    DisplayOrder = 0
                },
                new ShippingMethod
                {
                    Name = "By Ground",
                    Description ="Compared to other shipping methods, like by flight or over seas, ground shipping is carried out closer to the earth",
                    DisplayOrder = 1
                },
            };

            if (includeSamples)
            {
                entities.Add(new ShippingMethod
                {
                    Name = "Free shipping",
                    DisplayOrder = 2,
                    IgnoreCharges = true
                });
            }

            Alter(entities);
            return entities;
        }

        public List<CustomerRole> CustomerRoles(bool includeSamples)
        {
            var entities = new List<CustomerRole>
            {
                new CustomerRole
                {
                    Name = "Administrators",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.Administrators,
                },
                new CustomerRole
                {
                    Name = "Forum Moderators",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.ForumModerators,
                },
                new CustomerRole
                {
                    Name = "Registered",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.Registered,
                },
                new CustomerRole
                {
                    Name = "Guests",
                    Active = true,
                    IsSystemRole = true,
                    SystemName = SystemCustomerRoleNames.Guests,
                }
            };

            if (includeSamples)
            {
                entities.Add(new CustomerRole
                {
                    Name = "Inactive new customers",
                    Active = true,
                    IsSystemRole = false,
                    // SystemName is not required. It's only used here to assign a rule set later.
                    SystemName = "InactiveNewCustomers"
                });
            }

            Alter(entities);
            return entities;
        }

        public async Task<Address> AdminAddress()
        {
            var country = await _db.Countries.Where(x => x.ThreeLetterIsoCode == "USA").FirstOrDefaultAsync();

            var entity = new Address()
            {
                FirstName = "John",
                LastName = "Smith",
                PhoneNumber = "12345678",
                Email = "admin@myshop.com",
                FaxNumber = "",
                Company = "John Smith LLC",
                Address1 = "1234 Main Road",
                Address2 = "",
                City = "New York",
                StateProvince = country.StateProvinces.FirstOrDefault(),
                Country = country,
                ZipPostalCode = "12212",
                CreatedOnUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public Customer SearchEngineUser()
        {
            var entity = new Customer
            {
                Email = "builtin@search-engine-record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system guest record used for requests from search engines.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.SearchEngine,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public Customer BackgroundTaskUser()
        {
            var entity = new Customer
            {
                Email = "builtin@background-task-record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system record used for background tasks.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.BackgroundTask,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public Customer PdfConverterUser()
        {
            var entity = new Customer
            {
                Email = "builtin@pdf-converter-record.com",
                CustomerGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system record used for the PDF converter.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemCustomerNames.PdfConverter,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            Alter(entity);
            return entity;
        }

        public List<EmailAccount> EmailAccounts()
        {
            var entities = new List<EmailAccount>()
            {
                new EmailAccount
                {
                    Email = "test@mail.com",
                    DisplayName = "Store name",
                    Host = "smtp.mail.com",
                    Port = 25,
                    Username = "123",
                    Password = "123",
                    EnableSsl = false,
                    UseDefaultCredentials = false
                }
            };

            Alter(entities);
            return entities;
        }

        public List<Topic> Topics()
        {
            var entities = new List<Topic>()
            {
                new Topic
                    {
                        SystemName = "AboutUs",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "About Us",
                        Body = "<p>Put your &quot;About Us&quot; information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "CheckoutAsGuestOrRegister",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "",
                        Body = "<p><strong>Register and save time!</strong><br />Register with us for future convenience:</p><ul><li>Fast and easy check out</li><li>Easy access to your order history and status</li></ul>"
                    },
                new Topic
                    {
                        SystemName = "ConditionsOfUse",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Conditions of use",
                        Body = "<p>Put your conditions of use information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "ContactUs",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "Contact us",
                        Body = "<p>Put your contact information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "ForumWelcomeMessage",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "Forums",
                        Body = "<p>Put your welcome message here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "HomePageText",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "Welcome to our store",
                        Body = "<p>Online shopping is the process consumers go through to purchase products or services over the Internet. You can edit this in the admin site.</p></p>"
                    },
                new Topic
                    {
                        SystemName = "LoginRegistrationInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        RenderAsWidget = true,
                        WidgetWrapContent = false,
                        Title = "About login / registration",
                        Body = "<p><strong>Not registered yet?</strong></p><p>Create your own account now and experience our diversity. With an account you can place orders faster and will always have a&nbsp;perfect overview of your current and previous orders.</p>"
                    },
                new Topic
                    {
                        SystemName = "PrivacyInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        ShortTitle = "Privacy",
                        Title = "Privacy policy",
                        Body = "<p><strong></strong></p>"
                    },
                new Topic
                    {
                        SystemName = "ShippingInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Shipping & Returns",
                        Body = "<p>Put your shipping &amp; returns information here. You can edit this in the admin site.</p>"
                    },

                new Topic
                    {
                        SystemName = "Imprint",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Imprint",
                        Body = @"<p>Put your imprint information here. YOu can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "Disclaimer",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Disclaimer",
                        Body = "<p>Put your disclaimer information here. You can edit this in the admin site.</p>"
                    },
                new Topic
                    {
                        SystemName = "PaymentInfo",
                        IncludeInSitemap = false,
                        IsPasswordProtected = false,
                        Title = "Payment info",
                        Body = "<p>Put your payment information here. You can edit this in the admin site.</p>"
                    },
            };
            Alter(entities);
            return entities;
        }

        public async Task<List<ISettings>> Settings()
        {
            var defaultDimensionId = (await _db.MeasureDimensions.FirstOrDefaultAsync(x => x.SystemKeyword == "inch"))?.Id ?? 0;
            var defaultWeightId = (await _db.MeasureWeights.FirstOrDefaultAsync(x => x.SystemKeyword == "lb"))?.Id ?? 0;
            var defaultLanguageId = (await _db.Languages.FirstOrDefaultAsync())?.Id ?? 0;
            var defaultEmailAccountId = (await _db.EmailAccounts.FirstOrDefaultAsync())?.Id ?? 0;

            var entities = new List<ISettings>
            {
                new PdfSettings
                {
                },
                new CommonSettings
                {
                },
                new SeoSettings
                {
                },
                new SocialSettings
                {
                },
                new AdminAreaSettings
                {
                },
                new CatalogSettings
                {
                },
                new LocalizationSettings
                {
                    DefaultAdminLanguageId = defaultLanguageId
                },
                new CustomerSettings
                {
                },
                new AddressSettings
                {
                },
                new MediaSettings
                {
                },
                new StoreInformationSettings
                {
                },
                new RewardPointsSettings
                {
                },
                new CurrencySettings
                {
                },
                new MeasureSettings
                {
                    BaseDimensionId = defaultDimensionId,
                    BaseWeightId = defaultWeightId,
                },
                new ShoppingCartSettings
                {
                },
                new OrderSettings
                {
                },
                new SecuritySettings
                {
                },
                new ShippingSettings
                {
                },
                new PaymentSettings
                {
                    ActivePaymentMethodSystemNames = new List<string>
                    {
                        "Payments.CashOnDelivery",
                        "Payments.Manual",
                        "Payments.PayInStore",
                        "Payments.Prepayment"
                    }
                },
                new TaxSettings
                {
                },
                //new BlogSettings
                //{
                //},
                //new NewsSettings
                //{
                //},
                //new ForumSettings
                //{
                //},
                new EmailAccountSettings
                {
                    DefaultEmailAccountId = defaultEmailAccountId
                },
                new ThemeSettings
                {
                },
                new HomePageSettings
                {
                }
            };

            Alter(entities);
            return entities;
        }

        public List<ProductTemplate> ProductTemplates()
        {
            var entities = new List<ProductTemplate>
            {
                new ProductTemplate
                {
                    Name = "Default Product Template",
                    ViewPath = "Product",
                    DisplayOrder = 10
                }
            };

            Alter(entities);
            return entities;
        }

        public List<CategoryTemplate> CategoryTemplates()
        {
            var entities = new List<CategoryTemplate>
            {
                new CategoryTemplate
                {
                    Name = "Products in Grid or Lines",
                    ViewPath = "CategoryTemplate.ProductsInGridOrLines",
                    DisplayOrder = 1
                },
            };

            Alter(entities);
            return entities;
        }

        public List<ManufacturerTemplate> ManufacturerTemplates()
        {
            var entities = new List<ManufacturerTemplate>
            {
                new ManufacturerTemplate
                {
                    Name = "Products in Grid or Lines",
                    ViewPath = "ManufacturerTemplate.ProductsInGridOrLines",
                    DisplayOrder = 1
                },
            };

            Alter(entities);
            return entities;
        }

        public List<TaskDescriptor> TaskDescriptors()
        {
            var entities = new List<TaskDescriptor>
            {
                new TaskDescriptor
                {
                    Name = "Send emails",
                    CronExpression = "* * * * *", // every Minute
					Type = "SmartStore.Services.Messages.QueuedMessagesSendTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false,
                    Priority = TaskPriority.High
                },
                new TaskDescriptor
                {
                    Name = "Delete guests",
                    CronExpression = "*/10 * * * *", // Every 10 minutes
					Type = "SmartStore.Services.Customers.DeleteGuestsTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Delete logs",
                    CronExpression = "0 1 * * *", // At 01:00
					Type = "SmartStore.Services.Logging.DeleteLogsTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Clear cache",
                    CronExpression = "0 */12 * * *", // Every 12 hours
					Type = "SmartStore.Services.Caching.ClearCacheTask, SmartStore.Services",
                    Enabled = false,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Update currency exchange rates",
                    CronExpression = "0 */6 * * *", // Every 6 hours
					Type = "SmartStore.Services.Directory.UpdateExchangeRateTask, SmartStore.Services",
                    Enabled = false,
                    StopOnError = false,
                    Priority = TaskPriority.High
                },
                new TaskDescriptor
                {
                    Name = "Clear transient uploads",
                    CronExpression = "30 1,13 * * *", // At 01:30 and 13:30
					Type = "SmartStore.Services.Media.TransientMediaClearTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Clear email queue",
                    CronExpression = "0 2 * * *", // At 02:00
					Type = "SmartStore.Services.Messages.QueuedMessagesClearTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false,
                },
                new TaskDescriptor
                {
                    Name = "Cleanup temporary files",
                    CronExpression = "30 3 * * *", // At 03:30
					Type = "SmartStore.Services.Common.TempFileCleanupTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false
                },
                new TaskDescriptor
                {
                    Name = "Rebuild XML Sitemap",
                    CronExpression = "45 3 * * *",
                    Type = "SmartStore.Services.Seo.RebuildXmlSitemapTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false
                },
                new TaskDescriptor
                {
                    Name = "Update assignments of customers to customer roles",
                    CronExpression = "15 2 * * *", // At 02:15
                    Type = "SmartStore.Services.Customers.TargetGroupEvaluatorTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false
                },
                new TaskDescriptor
                {
                    Name = "Update assignments of products to categories",
                    CronExpression = "20 2 * * *", // At 02:20
                    Type = "SmartStore.Services.Catalog.ProductRuleEvaluatorTask, SmartStore.Services",
                    Enabled = true,
                    StopOnError = false
                }
            };

            Alter(entities);
            return entities;
        }

        #endregion

        #region Sample data creators

        //public IList<ForumGroup> ForumGroups()
        //{
        //    var forumGroupGeneral = new ForumGroup
        //    {
        //        Name = "General",
        //        Description = "",
        //        DisplayOrder = 1
        //    };

        //    var entities = new List<ForumGroup>
        //    {
        //        forumGroupGeneral
        //    };

        //    Alter(entities);
        //    return entities;
        //}

        //public IList<Forum> Forums()
        //{
        //    var group = _db.Set<ForumGroup>().FirstOrDefault(c => c.DisplayOrder == 1);

        //    var newProductsForum = new Forum
        //    {
        //        ForumGroup = group,
        //        Name = "New Products",
        //        Description = "Discuss new products and industry trends",
        //        NumTopics = 0,
        //        NumPosts = 0,
        //        LastPostCustomerId = 0,
        //        LastPostTime = null,
        //        DisplayOrder = 1
        //    };

        //    var packagingShippingForum = new Forum
        //    {
        //        ForumGroup = group,
        //        Name = "Packaging & Shipping",
        //        Description = "Discuss packaging & shipping",
        //        NumTopics = 0,
        //        NumPosts = 0,
        //        LastPostTime = null,
        //        DisplayOrder = 20
        //    };


        //    var entities = new List<Forum>
        //    {
        //        newProductsForum, packagingShippingForum
        //    };

        //    Alter(entities);
        //    return entities;
        //}

        public async Task<List<Discount>> Discounts()
        {
            var ruleSets = _db.RuleSets.Include(x => x.Rules).AsQueryable();

            var couponCodeDiscount = new Discount
            {
                Name = "Sample discount with coupon code",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = false,
                DiscountAmount = 10,
                RequiresCouponCode = true,
                CouponCode = "123"
            };

            var orderTotalDiscount = new Discount
            {
                Name = "20% order total discount",
                DiscountType = DiscountType.AssignedToOrderTotal,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 20,
                StartDateUtc = new DateTime(2020, 2, 1),
                EndDateUtc = new DateTime(2020, 2, 10)
            };
            orderTotalDiscount.RuleSets.Add(await ruleSets.FirstOrDefaultAsync(x => x.Rules.Any(y => y.RuleType == "CartOrderCount")));

            var weekendDiscount = new Discount
            {
                Name = "5% on weekend orders",
                DiscountType = DiscountType.AssignedToOrderSubTotal,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 5,
                StartDateUtc = new DateTime(2020, 3, 1),
                EndDateUtc = new DateTime(2020, 3, 10)
            };
            weekendDiscount.RuleSets.Add(await ruleSets.FirstOrDefaultAsync(x => x.Rules.Any(y => y.RuleType == "Weekday")));

            var manufacturersDiscount = new Discount
            {
                Name = "10% for certain manufacturers",
                DiscountType = DiscountType.AssignedToManufacturers,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 10,
                StartDateUtc = new DateTime(2020, 4, 5),
                EndDateUtc = new DateTime(2020, 4, 10)
            };

            var categoriesDiscount = new Discount
            {
                Name = "20% for certain categories",
                DiscountType = DiscountType.AssignedToCategories,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 20,
                StartDateUtc = new DateTime(2020, 6, 1),
                EndDateUtc = new DateTime(2020, 6, 30)
            };

            var productsDiscount = new Discount
            {
                Name = "25% on certain products",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountLimitation = DiscountLimitationType.Unlimited,
                UsePercentage = true,
                DiscountPercentage = 25,
                StartDateUtc = new DateTime(2020, 5, 10),
                EndDateUtc = new DateTime(2020, 5, 15)
            };

            var entities = new List<Discount>
            {
                couponCodeDiscount, orderTotalDiscount, weekendDiscount,
                manufacturersDiscount, categoriesDiscount, productsDiscount
            };

            Alter(entities);
            return entities;
        }

        public IList<DeliveryTime> DeliveryTimes()
        {
            var entities = new List<DeliveryTime>()
            {
                new DeliveryTime
                    {
                        Name = "available and ready to ship",
                        DisplayOrder = 0,
                        ColorHexValue = "#008000",
                        MinDays = 1,
                        MaxDays = 3
                    },
                new DeliveryTime
                    {
                        Name = "2-5 woking days",
                        DisplayOrder = 1,
                        ColorHexValue = "#FFFF00",
                        MinDays = 2,
                        MaxDays = 5
                    },
                new DeliveryTime
                    {
                        Name = "7 working days",
                        DisplayOrder = 2,
                        ColorHexValue = "#FF9900",
                        MinDays = 7,
                        MaxDays = 14
                    },
            };
            Alter(entities);
            return entities;
        }

        public IList<QuantityUnit> QuantityUnits()
        {
            var count = 0;
            var entities = new List<QuantityUnit>();

            var quPluralEn = new Dictionary<string, string>
            {
                { "Piece", "Pieces" },
                { "Box", "Boxes" },
                { "Parcel", "Parcels" },
                { "Palette", "Pallets" },
                { "Unit", "Units" },
                { "Sack", "Sacks" },
                { "Bag", "Bags" },
                { "Can", "Cans" },
                { "Packet", "Packets" },
                { "Bar", "Bars" },
                { "Bottle", "Bottles" },
                { "Glass", "Glasses" },
                { "Bunch", "Bunches" },
                { "Roll", "Rolls" },
                { "Cup", "Cups" },
                { "Bundle", "Bundles" },
                { "Barrel", "Barrels" },
                { "Set", "Sets" },
                { "Bucket", "Buckets" }
            };

            foreach (var qu in quPluralEn)
            {
                entities.Add(new QuantityUnit
                {
                    Name = qu.Key,
                    NamePlural = qu.Value,
                    Description = qu.Key,
                    IsDefault = qu.Key == "Piece",
                    DisplayOrder = count++
                });
            }

            Alter(entities);
            return entities;
        }

        //public IList<PollAnswer> PollAnswers()
        //{
        //    var pollAnswer1 = new PollAnswer()
        //    {
        //        Name = "Excellent",
        //        DisplayOrder = 1,
        //    };
        //    var pollAnswer2 = new PollAnswer()
        //    {
        //        Name = "Good",
        //        DisplayOrder = 2,
        //    };
        //    var pollAnswer3 = new PollAnswer()
        //    {
        //        Name = "Poor",
        //        DisplayOrder = 3,
        //    };
        //    var pollAnswer4 = new PollAnswer()
        //    {
        //        Name = "Very bad",
        //        DisplayOrder = 4,
        //    };
        //    var pollAnswer5 = new PollAnswer()
        //    {
        //        Name = "Daily",
        //        DisplayOrder = 5,
        //    };
        //    var pollAnswer6 = new PollAnswer()
        //    {
        //        Name = "Once a week",
        //        DisplayOrder = 6,
        //    };
        //    var pollAnswer7 = new PollAnswer()
        //    {
        //        Name = "Every two weeks",
        //        DisplayOrder = 7,
        //    };
        //    var pollAnswer8 = new PollAnswer()
        //    {
        //        Name = "Once a month",
        //        DisplayOrder = 8,
        //    };

        //    var entities = new List<PollAnswer>
        //    {
        //        pollAnswer1, pollAnswer2, pollAnswer3, pollAnswer4, pollAnswer5,  pollAnswer6,  pollAnswer7,  pollAnswer8
        //    };

        //    Alter(entities);
        //    return entities;
        //}

        //public IList<Poll> Polls()
        //{
        //    var defaultLanguage = _db.Set<Language>().FirstOrDefault();
        //    var poll1 = new Poll
        //    {
        //        Language = defaultLanguage,
        //        Name = "How do you like the shop?",
        //        SystemKeyword = "Blog",
        //        Published = true,
        //        DisplayOrder = 1,
        //    };

        //    poll1.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Excellent",
        //        DisplayOrder = 1,
        //    });

        //    poll1.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Good",
        //        DisplayOrder = 2,
        //    });

        //    poll1.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Poor",
        //        DisplayOrder = 3,
        //    });

        //    poll1.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Very bad",
        //        DisplayOrder = 4,
        //    });


        //    var poll2 = new Poll
        //    {
        //        Language = defaultLanguage,
        //        Name = "How often do you buy online?",
        //        SystemKeyword = "Blog",
        //        Published = true,
        //        DisplayOrder = 2,
        //    };

        //    poll2.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Daily",
        //        DisplayOrder = 1,
        //    });

        //    poll2.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Once a week",
        //        DisplayOrder = 2,
        //    });

        //    poll2.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Every two weeks",
        //        DisplayOrder = 3,
        //    });

        //    poll2.PollAnswers.Add(new PollAnswer
        //    {
        //        Name = "Once a month",
        //        DisplayOrder = 4,
        //    });


        //    var entities = new List<Poll>
        //    {
        //        poll1, poll2
        //    };

        //    Alter(entities);
        //    return entities;
        //}

        public List<Campaign> Campaigns()
        {
            var entities = new List<Campaign>
            {
                new Campaign
                {
                    Name = "Reminder of inactive new customers",
                    Subject = "New, exciting products are waiting for you to be discovered.",
                    Body = "<p>Efficiently unleash client-centric technologies and go forward information. Conveniently benchmark client-focused resources vis-a-vis interdependent paradigms. Synergistically disseminate interdependent supply chains via equity invested internal or 'organic' sources. Objectively exploit seamless growth strategies without orthogonal methodologies. Intrinsicly disseminate bricks-and-clicks web-readiness and e-business e-services.</p><p>Objectively develop performance based e-business and interdependent sources. Objectively evolve flexible markets via leveraged interfaces. Professionally deliver focused 'outside the box' thinking rather than global sources. Energistically redefine leveraged supply chains through customized relationships. Dramatically actualize resource sucking content rather than cross-platform e-business.</p><p>Seamlessly synthesize vertical mindshare without flexible sources. Distinctively productize timely infrastructures rather than cross-media niches. Dynamically evisculate pandemic convergence and scalable mindshare. Seamlessly embrace fully tested relationships whereas go forward initiatives. Globally actualize user-centric channels.</p>",
                    CreatedOnUtc = DateTime.UtcNow,
                    SubjectToAcl = true
                }
            };

            Alter(entities);
            return entities;
        }

        public async Task<List<RuleSetEntity>> RuleSets()
        {
            // Cart: weekends.
            var weekends = new RuleSetEntity
            {
                Scope = RuleScope.Cart,
                Name = "Weekends",
                IsActive = true
            };
            weekends.Rules.Add(new RuleEntity
            {
                RuleType = "Weekday",
                Operator = RuleOperator.In,
                Value = $"{(int)DayOfWeek.Sunday},{(int)DayOfWeek.Saturday}"
            });

            // Cart: major customers.
            var majorCustomers = new RuleSetEntity
            {
                Scope = RuleScope.Cart,
                Name = "Major customers",
                Description = "3 or more orders and current order value at least 200,- Euro.",
                IsActive = true
            };
            majorCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "CartSubtotal",
                Operator = RuleOperator.GreaterThanOrEqualTo,
                Value = "200"
            });
            majorCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "CartOrderCount",
                Operator = RuleOperator.GreaterThanOrEqualTo,
                Value = "3"
            });

            // Product: sale.
            var saleProducts = new RuleSetEntity
            {
                Scope = RuleScope.Product,
                Name = "Sale",
                Description = "Products with applied discounts.",
                IsActive = true
            };
            saleProducts.Rules.Add(new RuleEntity
            {
                RuleType = "Discount",
                Operator = RuleOperator.IsEqualTo,
                Value = "true"
            });

            // Customer: inactive new customers.
            var inactiveNewCustomers = new RuleSetEntity
            {
                Scope = RuleScope.Customer,
                Name = "Inactive new customers",
                Description = "One completed order placed at least 90 days ago.",
                IsActive = true
            };
            inactiveNewCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "CompletedOrderCount",
                Operator = RuleOperator.IsEqualTo,
                Value = "1"
            });
            inactiveNewCustomers.Rules.Add(new RuleEntity
            {
                RuleType = "LastOrderDateDays",
                Operator = RuleOperator.GreaterThanOrEqualTo,
                Value = "90"
            });


            // Offer free shipping method for major customers.
            var freeShipping = await _db.ShippingMethods.FirstOrDefaultAsync(x => x.DisplayOrder == 2);
            if (freeShipping != null)
            {
                freeShipping.RuleSets.Add(majorCustomers);
            }

            // Assign rule conditions for inactive new customers to the related customer role.
            // We later bind the reminder campaign to it.
            var inactiveNewCustomersRole = await _db.CustomerRoles.FirstOrDefaultAsync(x => x.SystemName == "InactiveNewCustomers");
            if (inactiveNewCustomersRole != null)
            {
                inactiveNewCustomersRole.RuleSets.Add(inactiveNewCustomers);
            }


            var entities = new List<RuleSetEntity>
            {
                weekends, majorCustomers, saleProducts, inactiveNewCustomers
            };

            Alter(entities);
            return entities;
        }

        public async Task FinalizeSamples()
        {
            // Bind the reminder campaign to the rule conditions for inactive new customers.
            var reminderCampaign = await _db.Campaigns.FirstOrDefaultAsync(x => x.SubjectToAcl);
            if (reminderCampaign != null)
            {
                var inactiveNewCustomersRole = await _db.CustomerRoles.FirstOrDefaultAsync(x => x.SystemName == "InactiveNewCustomers");
                if (inactiveNewCustomersRole != null)
                {
                    await _db.AclRecords.AddAsync(new AclRecord
                    {
                        EntityId = reminderCampaign.Id,
                        EntityName = nameof(Campaign),
                        CustomerRoleId = inactiveNewCustomersRole.Id,
                        IsIdle = false
                    });
                }
            }
        }

        #region Alterations

        protected virtual void Alter(IList<MeasureDimension> entities)
        {
        }

        protected virtual void Alter(IList<MeasureWeight> entities)
        {
        }

        protected virtual void Alter(IList<TaxCategory> entities)
        {
        }

        protected virtual void Alter(IList<Currency> entities)
        {
        }

        protected virtual void Alter(IList<Country> entities)
        {
        }

        protected virtual void Alter(IList<ShippingMethod> entities)
        {
        }

        protected virtual void Alter(IList<CustomerRole> entities)
        {
        }

        protected virtual void Alter(Address entity)
        {
        }

        protected virtual void Alter(Customer entity)
        {
        }

        protected virtual void Alter(IList<DeliveryTime> entities)
        {
        }

        protected virtual void Alter(IList<QuantityUnit> entities)
        {
        }

        protected virtual void Alter(IList<EmailAccount> entities)
        {
        }

        protected virtual void Alter(IList<MessageTemplate> entities)
        {
        }

        protected virtual void Alter(IList<Topic> entities)
        {
        }

        protected virtual void Alter(IList<Store> entities)
        {
        }

        protected virtual void Alter(IList<MediaFile> entities)
        {
        }

        protected virtual void Alter(IList<ISettings> settings)
        {
        }

        protected virtual void Alter(IList<StoreInformationSettings> settings)
        {
        }

        protected virtual void Alter(IList<OrderSettings> settings)
        {
        }

        protected virtual void Alter(IList<MeasureSettings> settings)
        {
        }

        protected virtual void Alter(IList<ShippingSettings> settings)
        {
        }

        protected virtual void Alter(IList<PaymentSettings> settings)
        {
        }

        protected virtual void Alter(IList<TaxSettings> settings)
        {
        }

        protected virtual void Alter(IList<EmailAccountSettings> settings)
        {
        }

        protected virtual void Alter(IList<ActivityLogType> entities)
        {
        }

        protected virtual void Alter(IList<ProductTemplate> entities)
        {
        }

        protected virtual void Alter(IList<CategoryTemplate> entities)
        {
        }

        protected virtual void Alter(IList<ManufacturerTemplate> entities)
        {
        }

        protected virtual void Alter(IList<TaskDescriptor> entities)
        {
        }

        protected virtual void Alter(IList<SpecificationAttribute> entities)
        {
        }

        protected virtual void Alter(IList<ProductAttribute> entities)
        {
        }

        protected virtual void Alter(IList<ProductAttributeOptionsSet> entities)
        {
        }

        protected virtual void Alter(IList<ProductAttributeOption> entities)
        {
        }

        protected virtual void Alter(IList<ProductVariantAttribute> entities)
        {
        }

        protected virtual void Alter(IList<Category> entities)
        {
        }

        protected virtual void Alter(IList<Manufacturer> entities)
        {
        }

        protected virtual void Alter(IList<Product> entities)
        {
        }

        //protected virtual void Alter(IList<ForumGroup> entities)
        //{
        //}

        //protected virtual void Alter(IList<Forum> entities)
        //{
        //}

        //protected virtual void Alter(IList<BlogPost> entities)
        //{
        //}

        //protected virtual void Alter(IList<NewsItem> entities)
        //{
        //}

        //protected virtual void Alter(IList<Poll> entities)
        //{
        //}

        //protected virtual void Alter(IList<PollAnswer> entities)
        //{
        //}

        protected virtual void Alter(IList<Discount> entities)
        {
        }

        protected virtual void Alter(IList<Campaign> entities)
        {
        }

        protected virtual void Alter(IList<RuleSetEntity> entities)
        {
        }

        protected virtual void Alter(IList<ProductTag> entities)
        {
        }

        protected virtual void Alter(IList<ProductBundleItem> entities)
        {
        }

        protected virtual void Alter(UrlRecord entity)
        {
        }

        #endregion Alterations

        #endregion Sample data creators

        #region Helpers

        protected SmartDbContext DbContext => _db;

        protected string SampleImagesPath => _sampleImagesPath;

        public virtual UrlRecord CreateUrlRecordFor<T>(T entity) where T : BaseEntity, ISlugSupported, new()
        {
            var name = entity switch
            {
                var x when x is Category or Manufacturer or Product => x.GetDisplayName(),
                //BlogPost or NewsItem => x.Title,
                Topic y => GetSeName(y.SystemName).Truncate(400),
                _ => string.Empty
            };

            if (name.HasValue())
            {
                var result = new UrlRecord
                {
                    EntityId = entity.Id,
                    EntityName = entity.GetEntityName(),
                    LanguageId = 0,
                    Slug = name,
                    IsActive = true
                };

                Alter(result);
                return result;
            }

            return null;
        }

        protected MediaFile CreatePicture(string fileName, string seoFilename = null)
        {
            try
            {
                var ext = Path.GetExtension(fileName);
                var path = Path.Combine(_sampleImagesPath, fileName).Replace('/', '\\');
                var mimeType = MimeTypes.MapNameToMimeType(ext);
                var buffer = File.ReadAllBytes(path);
                var now = DateTime.UtcNow;

                var name = seoFilename.HasValue()
                    ? seoFilename.Truncate(100) + ext
                    : Path.GetFileName(fileName).ToLower().Replace('_', '-');

                var file = new MediaFile
                {
                    Name = name,
                    MediaType = "image",
                    MimeType = mimeType,
                    Extension = ext.EmptyNull().TrimStart('.'),
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now,
                    Size = buffer.Length,
                    MediaStorage = new MediaStorage { Data = buffer },
                    Version = 1 // so that FolderId is set later during track detection
                };

                return file;
            }
            catch (Exception ex)
            {
                //throw ex;
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }

        protected void AddProductPicture(
            Product product,
            string imageName,
            string seName = null,
            int displayOrder = 1)
        {
            if (seName == null)
            {
                seName = GetSeName(Path.GetFileNameWithoutExtension(imageName));
            }

            product.ProductPictures.Add(new ProductMediaFile
            {
                MediaFile = CreatePicture(imageName, seName),
                DisplayOrder = displayOrder
            });
        }

        protected string GetSeName(string name)
        {
            return SeoHelper.BuildSlug(name);
        }

        protected Currency CreateCurrency(string locale, decimal rate = 1M, string formatting = "", bool published = false, int order = 1)
        {
            Currency currency = null;
            try
            {
                var info = new RegionInfo(locale);
                if (info != null)
                {
                    currency = new Currency
                    {
                        DisplayLocale = locale,
                        Name = info.CurrencyNativeName,
                        CurrencyCode = info.ISOCurrencySymbol,
                        Rate = rate,
                        CustomFormatting = formatting,
                        Published = published,
                        DisplayOrder = order
                    };
                }
            }
            catch
            {
                return null;
            }

            return currency;
        }

        protected string FormatAttributeXml(int attributeId, int valueId, bool withRootTag = true)
        {
            var xml = $"<ProductVariantAttribute ID=\"{attributeId}\"><ProductVariantAttributeValue><Value>{valueId}</Value></ProductVariantAttributeValue></ProductVariantAttribute>";

            if (withRootTag)
            {
                return string.Concat("<Attributes>", xml, "</Attributes>");
            }

            return xml;
        }
        protected string FormatAttributeXml(int attributeId1, int valueId1, int attributeId2, int valueId2)
        {
            return string.Concat(
                "<Attributes>",
                FormatAttributeXml(attributeId1, valueId1, false),
                FormatAttributeXml(attributeId2, valueId2, false),
                "</Attributes>");
        }
        protected string FormatAttributeXml(int attributeId1, int valueId1, int attributeId2, int valueId2, int attributeId3, int valueId3)
        {
            return string.Concat(
                "<Attributes>",
                FormatAttributeXml(attributeId1, valueId1, false),
                FormatAttributeXml(attributeId2, valueId2, false),
                FormatAttributeXml(attributeId3, valueId3, false),
                "</Attributes>");
        }

        #endregion
    }
}