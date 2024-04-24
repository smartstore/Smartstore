using System.Xml;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products.Utilities;
using Smartstore.Core.Common;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Theming;
using Smartstore.Data.Hooks;
using Smartstore.IO;

namespace Smartstore.Core.Installation
{
    public partial class InstallationDataSeeder : DataSeeder<SmartDbContext>
    {
        private readonly DbMigrator<SmartDbContext> _migrator;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly SeedDataConfiguration _config;
        private readonly InvariantSeedData _data;

        private IXmlResourceManager _xmlResourceManager;
        private int _defaultStoreId;

        public InstallationDataSeeder(
            IApplicationContext appContext,
            DbMigrator<SmartDbContext> migrator,
            IMessageTemplateService messageTemplateService,
            SeedDataConfiguration configuration,
            ILogger logger)
            : base(appContext, logger)
        {
            Guard.NotNull(migrator, nameof(migrator));
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(configuration.Language, "SeedDataConfiguration.Language");
            Guard.NotNull(configuration.Data, "SeedDataConfiguration.SeedData");

            _migrator = migrator;
            _messageTemplateService = messageTemplateService;
            _config = configuration;
            _data = configuration.Data;
        }

        protected IXmlResourceManager XmlResourceManager
        {
            get
            {
                if (_xmlResourceManager == null)
                {
                    _xmlResourceManager = new XmlResourceManager(
                        Context,
                        NullRequestCache.Instance,
                        null /* ILanguageService: not needed during install */,
                        null /* ILocalizationService: not needed during install */);
                }

                return _xmlResourceManager;
            }
        }

        #region IDataSeeder

        protected override async Task SeedCoreAsync()
        {
            Context.ChangeTracker.AutoDetectChangesEnabled = false;
            Context.MinHookImportance = HookImportance.Essential;

            _config.ProgressMessageCallback("Progress.CreatingRequiredData");

            // Special mandatory (non-visible) settings
            await Context.MigrateSettingsAsync(x =>
            {
                x.Add("Media.Storage.Provider", _config.StoreMediaInDB ? DatabaseMediaStorageProvider.SystemName : FileSystemMediaStorageProvider.SystemName);
            });

            // Initialize seeder AFTER we added "Media.Storage.Provider" key,
            // because SampleMediaUtility depends on it.
            _data.Initialize(Context, _config, EngineContext.Current.Application);

            Populate("PopulatePictures", () => _data.Pictures(_config.SeedSampleData));
            await PopulateAsync("PopulateCurrencies", PopulateCurrencies);
            await PopulateAsync("PopulateStores", PopulateStores);
            await PopulateAsync("InstallLanguages", () => PopulateLanguage(_config.Language));
            await PopulateAsync("PopulateMeasureDimensions", _data.MeasureDimensions());
            await PopulateAsync("PopulateMeasureWeights", _data.MeasureWeights());
            await PopulateAsync("PopulateTaxCategories", PopulateTaxCategories);
            await PopulateAsync("PopulateCountriesAndStates", PopulateCountriesAndStates);
            await PopulateAsync("PopulateShippingMethods", PopulateShippingMethods);
            await PopulateAsync("PopulateDeliveryTimes", _data.DeliveryTimes());
            await PopulateAsync("PopulateQuantityUnits", _data.QuantityUnits());
            await PopulateAsync("PopulateCustomersAndUsers", async () => await PopulateCustomersAndUsers(_config.DefaultUserName, _config.DefaultUserPassword));
            await PopulateAsync("PopulateEmailAccounts", _data.EmailAccounts());
            await PopulateAsync("PopulateMessageTemplates", PopulateMessageTemplates);
            await PopulateAsync("PopulateTopics", PopulateTopics);
            await PopulateAsync("PopulatePriceLabels", _data.PriceLabels());
            await PopulateAsync("PopulateSettings", PopulateSettings);
            await PopulateAsync("PopulateActivityLogTypes", _data.ActivityLogTypes());
            await PopulateAsync("PopulateCustomersAndUsers", async () => await HashDefaultCustomerPassword(_config.DefaultUserName, _config.DefaultUserPassword));
            await PopulateAsync("PopulateProductTemplates", _data.ProductTemplates());
            await PopulateAsync("PopulateCategoryTemplates", _data.CategoryTemplates());
            await PopulateAsync("PopulateManufacturerTemplates", _data.ManufacturerTemplates());
            await PopulateAsync("PopulateScheduleTasks", _data.TaskDescriptors());
            await PopulateAsync("PopulateLocaleResources", async () => await PopulateLocaleResources(_config.Language));
            await PopulateAsync("PopulateMenus", _data.Menus());
            
            if (_config.SeedSampleData)
            {
                Logger.Info("Seeding sample data");

                _config.ProgressMessageCallback("Progress.CreatingSampleData");

                await PopulateAsync("PopulateSpecificationAttributes", _data.SpecificationAttributes());
                await PopulateAsync("PopulateProductAttributes", _data.ProductAttributes());
                await PopulateAsync("PopulateProductAttributeOptionsSets", _data.ProductAttributeOptionsSets());
                await PopulateAsync("PopulateProductAttributeOptions", _data.ProductAttributeOptions());
                await PopulateAsync("PopulateCampaigns", _data.Campaigns());
                await PopulateAsync("PopulateRuleSets", _data.RuleSets());
                await PopulateAsync("PopulateDiscounts", _data.Discounts());
                await PopulateAsync("PopulateCategories", PopulateCategories);
                await PopulateAsync("PopulateManufacturers", PopulateManufacturers);
                await PopulateAsync("PopulateProducts", PopulateProducts);
                await PopulateAsync("PopulateProductBundleItems", _data.ProductBundleItems());
                await PopulateAsync("PopulateProductVariantAttributes", _data.ProductVariantAttributes());
                await PopulateAsync("ProductVariantAttributeCombinations", _data.ProductVariantAttributeCombinations());
                await PopulateAsync("PopulateProductTags", _data.ProductTags());
                Populate("FinalizeSamples", _data.FinalizeSamples);
            }

            // Perf
            Context.DetachEntities<BaseEntity>();
        }

        #endregion

        #region Populate

        private async Task PopulateStores()
        {
            var stores = _data.Stores();
            await SaveRangeAsync(stores);
            _defaultStoreId = stores.First().Id;
        }

        private async Task PopulateTaxCategories()
        {
            var taxCategories = _data.TaxCategories();
            await SaveRangeAsync(taxCategories);

            // Add tax rates to fixed rate provider
            int i = 0;
            var taxIds = taxCategories.OrderBy(x => x.DisplayOrder).Select(x => x.Id).ToList();
            foreach (var id in taxIds)
            {
                decimal rate = 0;
                if (_data.FixedTaxRates.Any() && _data.FixedTaxRates.Length > i)
                {
                    rate = _data.FixedTaxRates[i];
                }
                i++;

                Context.Settings.Add(new Setting
                {
                    Name = string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", id).ToLowerInvariant(),
                    Value = rate.Convert<string>(),
                });
            }

            await Context.SaveChangesAsync();
        }

        private Task PopulateLanguage(Language primaryLanguage)
        {
            primaryLanguage.Published = true;
            return SaveAsync(primaryLanguage);
        }

        private async Task PopulateLocaleResources(Language language)
        {
            var appDataRoot = EngineContext.Current.Application.AppDataRoot;

            var locDir = appDataRoot.GetDirectory("Localization/App/" + language.LanguageCulture);
            if (!locDir.Exists)
            {
                // Fallback to neutral language folder (de, en etc.)
                locDir = appDataRoot.GetDirectory("Localization/App/" + language.UniqueSeoCode);
            }

            if (!locDir.Exists)
            {
                return;
            }

            var xmlResourceManager = XmlResourceManager;

            // Perf
            Context.DetachEntities<BaseEntity>();

            // Save resources
            foreach (var file in locDir.EnumerateFiles("*.smres.xml"))
            {
                var doc = new XmlDocument();
                doc.Load(file.PhysicalPath);

                doc = xmlResourceManager.FlattenResourceFile(doc);

                // Now we have a parsed XML file (the same structure as exported language packs)
                // Let's save resources
                await xmlResourceManager.ImportResourcesFromXmlAsync(language, doc);

                // No need to call SaveChanges() here, as the above call made it
                // already without AutoDetectChanges(), so it's fast.

                // Perf
                Context.DetachEntities<LocaleStringResource>();
            }

            await SeedPendingLocaleResources(locDir);
        }

        private async Task SeedPendingLocaleResources(IDirectory locDir)
        {
            var fs = locDir.FileSystem;
            var headFile = fs.GetFile(PathUtility.Join(locDir.SubPath, "head.txt"));

            if (!headFile.Exists)
            {
                return;
            }

            var resHead = headFile.ReadAllText().Trim();
            if (resHead.HasValue())
            {
                if (long.TryParse(resHead, out var version))
                {
                    await _migrator.SeedPendingLocaleResourcesAsync(version);
                }
                else
                {
                    throw new ArgumentException("Wrong head value (head.txt) for seeding pending locale resources."
                        + $" Must be a migration version number of type 'long'. See {nameof(MigrationHistory.Version)}.");
                }
            }
        }

        private async Task PopulateCurrencies()
        {
            await SaveRangeAsync(_data.Currencies().Where(x => x != null));
        }

        private async Task PopulateCountriesAndStates()
        {
            await SaveRangeAsync(_data.Countries().Where(x => x != null));
        }

        private Task PopulateShippingMethods()
        {
            return SaveRangeAsync(_data.ShippingMethods(_config.SeedSampleData).Where(x => x != null));
        }

        private async Task PopulateCustomersAndUsers(string defaultUserEmail, string defaultUserPassword)
        {
            var customerRoles = _data.CustomerRoles(_config.SeedSampleData);
            await SaveRangeAsync(customerRoles.Where(x => x != null));

            //admin user
            var adminUser = new Customer
            {
                CustomerGuid = Guid.NewGuid(),
                Email = defaultUserEmail,
                Username = defaultUserEmail,
                Password = defaultUserPassword,
                PasswordFormat = PasswordFormat.Clear,
                PasswordSalt = "",
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            var adminAddress = _data.AdminAddress();

            adminUser.Addresses.Add(adminAddress);
            adminUser.BillingAddress = adminAddress;
            adminUser.ShippingAddress = adminAddress;

            var adminRole = customerRoles.First(x => x.SystemName == SystemCustomerRoleNames.Administrators);
            var forumRole = customerRoles.First(x => x.SystemName == SystemCustomerRoleNames.ForumModerators);
            var registeredRole = customerRoles.First(x => x.SystemName == SystemCustomerRoleNames.Registered);

            adminUser.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = adminUser.Id, CustomerRoleId = adminRole.Id });
            adminUser.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = adminUser.Id, CustomerRoleId = forumRole.Id });
            adminUser.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = adminUser.Id, CustomerRoleId = registeredRole.Id });
            await SaveAsync(adminUser);

            // Set default customer name
            var firstAddress = adminUser.Addresses.FirstOrDefault();
            Context.GenericAttributes.AddRange(new[]
            {
                new GenericAttribute
                {
                    EntityId = adminUser.Id,
                    Key = "FirstName",
                    KeyGroup = "Customer",
                    Value = firstAddress.FirstName
                },
                new GenericAttribute
                {
                    EntityId = adminUser.Id,
                    Key = "LastName",
                    KeyGroup = "Customer",
                    Value = firstAddress.LastName
                }
            });

            await Context.SaveChangesAsync();

            // Built-in user for search engines (crawlers)
            var guestRole = customerRoles.FirstOrDefault(x => x.SystemName == SystemCustomerRoleNames.Guests);

            var customer = _data.SearchEngineUser();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
            await SaveAsync(customer);

            // Built-in user for background tasks
            customer = _data.BackgroundTaskUser();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
            await SaveAsync(customer);

            // Built-in user for the PDF converter
            customer = _data.PdfConverterUser();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
            await SaveAsync(customer);

            // Built-in user for the webhook client
            customer = _data.WebhookClientUser();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
            await SaveAsync(customer);
        }

        private async Task HashDefaultCustomerPassword(string defaultUserEmail, string defaultUserPassword)
        {
            var encryptor = new Encryptor(new SecuritySettings());
            var saltKey = encryptor.CreateSaltKey(5);
            var adminUser = await Context.Customers.FirstOrDefaultAsync(x => x.Email == _config.DefaultUserName);

            adminUser.PasswordSalt = saltKey;
            adminUser.PasswordFormat = PasswordFormat.Hashed;
            adminUser.Password = encryptor.CreatePasswordHash(defaultUserPassword, saltKey, new CustomerSettings().HashedPasswordFormat);

            await Context.SaveChangesAsync();
        }

        private async Task PopulateSettings()
        {
            var settings = _data.Settings();
            foreach (var setting in settings)
            {
                Type settingType = setting.GetType();
                int storeId = settingType.Equals(typeof(ThemeSettings)) ? _defaultStoreId : 0;
                await SettingFactory.SaveSettingsAsync(Context, setting, storeId: storeId);
            }
        }

        private async Task PopulateMessageTemplates()
        {
            await _messageTemplateService.ImportAllTemplatesAsync(_config.Language.LanguageCulture);
        }

        private async Task PopulateCategories()
        {
            var categoriesFirstLevel = _data.CategoriesFirstLevel();
            await SaveRangeAsync(categoriesFirstLevel);
            await PopulateUrlRecordsFor(categoriesFirstLevel);

            var categoriesSecondLevel = _data.CategoriesSecondLevel();
            await SaveRangeAsync(categoriesSecondLevel);
            await PopulateUrlRecordsFor(categoriesSecondLevel);
        }

        private async Task PopulateManufacturers()
        {
            var manufacturers = _data.Manufacturers();
            await SaveRangeAsync(manufacturers);
            await PopulateUrlRecordsFor(manufacturers);
        }

        private async Task PopulateProducts()
        {
            var products = _data.Products();
            await SaveRangeAsync(products);

            _data.AddDownloads(products);

            await PopulateUrlRecordsFor(products);

            _data.AssignGroupedProducts(products);

            // Fix MainPictureId
            await ProductPictureHelper.FixProductMainPictureIds(Context);
        }

        private async Task PopulateTopics()
        {
            var topics = _data.Topics();
            await SaveRangeAsync(topics);
            await PopulateUrlRecordsFor(topics);
        }

        private Task PopulateUrlRecordsFor<T>(IEnumerable<T> entities) where T : BaseEntity, ISlugSupported, new()
            => PopulateUrlRecordsFor(entities, entity => _data.CreateUrlRecordFor(entity));

        #endregion
    }
}
