using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products.Utilities;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messages.Utilities;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Theming;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore.Core.Installation
{
    public partial class InstallationDataSeeder : IDataSeeder<SmartDbContext>
    {
        private readonly SeedDataConfiguration _config;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private SmartDbContext _db;
        private InvariantSeedData _data;
        //private IGenericAttributeService _gaService;
        private ILocalizationService _locService;
        private IXmlResourceManager _xmlResourceManager;
        private IUrlService _urlService;
        private int _defaultStoreId;

        public InstallationDataSeeder(SeedDataConfiguration configuration, ILogger logger, IHttpContextAccessor httpContextAccessor)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(configuration.Language, "SeedDataConfiguration.Language");
            Guard.NotNull(configuration.Data, "SeedDataConfiguration.SeedData");
            Guard.NotNull(httpContextAccessor, nameof(httpContextAccessor));

            _config = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _data = configuration.Data;
        }

        protected IUrlService UrlService
        {
            get
            {
                if (_urlService == null)
                {
                    _urlService = new UrlService(
                        _db,
                        NullCache.Instance,
                        _httpContextAccessor,
                        null, // IWorkContext not accessed
                        null, // IStoreContext not accessed
                        null, // ILanguageService not accessed
                        new LocalizationSettings(),
                        new SeoSettings { LoadAllUrlAliasesOnStartup = false },
                        new PerformanceSettings(),
                        new SecuritySettings());
                }

                return _urlService;
            }
        }

        protected IXmlResourceManager XmlResourceManager
        {
            get
            {
                if (_xmlResourceManager == null)
                {
                    _xmlResourceManager = new XmlResourceManager(
                        _db,
                        NullRequestCache.Instance,
                        null /* ILanguageService: not needed during install */,
                        null /* ILocalizationService: not needed during install */);
                }

                return _xmlResourceManager;
            }
        }

        #region IDataSeeder

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context)
        {
            Guard.NotNull(context, nameof(context));

            _db = context;
            _data.Initialize(_db, _config.Language);

            _db.ChangeTracker.AutoDetectChangesEnabled = false;
            _db.MinHookImportance = HookImportance.Essential;

            _config.ProgressMessageCallback("Progress.CreatingRequiredData");

            // special mandatory (non-visible) settings
            await _db.MigrateSettingsAsync(x =>
            {
                x.Add("Media.Storage.Provider", _config.StoreMediaInDB ? DatabaseMediaStorageProvider.SystemName : FileSystemMediaStorageProvider.SystemName);
            });

            await Populate("PopulatePictures", _data.Pictures());
            await Populate("PopulateCurrencies", PopulateCurrencies);
            await Populate("PopulateStores", PopulateStores);
            await Populate("InstallLanguages", () => PopulateLanguage(_config.Language));
            await Populate("PopulateMeasureDimensions", _data.MeasureDimensions());
            await Populate("PopulateMeasureWeights", _data.MeasureWeights());
            await Populate("PopulateTaxCategories", PopulateTaxCategories);
            await Populate("PopulateCountriesAndStates", PopulateCountriesAndStates);
            await Populate("PopulateShippingMethods", PopulateShippingMethods);
            await Populate("PopulateDeliveryTimes", _data.DeliveryTimes());
            await Populate("PopulateQuantityUnits", _data.QuantityUnits());
            await Populate("PopulateCustomersAndUsers", async () => await PopulateCustomersAndUsers(_config.DefaultUserName, _config.DefaultUserPassword));
            await Populate("PopulateEmailAccounts", _data.EmailAccounts());
            await Populate("PopulateMessageTemplates", PopulateMessageTemplates);
            await Populate("PopulateTopics", PopulateTopics);
            await Populate("PopulateSettings", PopulateSettings);
            await Populate("PopulateActivityLogTypes", _data.ActivityLogTypes());
            await Populate("PopulateCustomersAndUsers", async () => await HashDefaultCustomerPassword(_config.DefaultUserName, _config.DefaultUserPassword));
            await Populate("PopulateProductTemplates", _data.ProductTemplates());
            await Populate("PopulateCategoryTemplates", _data.CategoryTemplates());
            await Populate("PopulateManufacturerTemplates", _data.ManufacturerTemplates());
            await Populate("PopulateScheduleTasks", _data.TaskDescriptors());
            await Populate("PopulateLocaleResources", async () => await PopulateLocaleResources(_config.Language));
            await Populate("PopulateMenus", _data.Menus());

            if (_config.SeedSampleData)
            {
                _logger.Info("Seeding sample data");

                _config.ProgressMessageCallback("Progress.CreatingSampleData");

                await Populate("PopulateSpecificationAttributes", _data.SpecificationAttributes());
                await Populate("PopulateProductAttributes", _data.ProductAttributes());
                await Populate("PopulateProductAttributeOptionsSets", _data.ProductAttributeOptionsSets());
                await Populate("PopulateProductAttributeOptions", _data.ProductAttributeOptions());
                await Populate("PopulateCampaigns", _data.Campaigns());
                await Populate("PopulateRuleSets", _data.RuleSets());
                await Populate("PopulateDiscounts", _data.Discounts());
                await Populate("PopulateCategories", PopulateCategories);
                await Populate("PopulateManufacturers", PopulateManufacturers);
                await Populate("PopulateProducts", PopulateProducts);
                await Populate("PopulateProductBundleItems", _data.ProductBundleItems());
                await Populate("PopulateProductVariantAttributes", _data.ProductVariantAttributes());
                await Populate("ProductVariantAttributeCombinations", _data.ProductVariantAttributeCombinations());
                await Populate("PopulateProductTags", _data.ProductTags());
                ////////await Populate("PopulateForumsGroups", _data.ForumGroups());
                ////////await Populate("PopulateForums", _data.Forums());
                ////////await Populate("PopulateBlogPosts", PopulateBlogPosts);
                ////////await Populate("PopulateNews", PopulateNewsItems);
                ////////await Populate("PopulatePolls", _data.Polls());
                Populate("FinalizeSamples", () => _data.FinalizeSamples());
            }

            await Populate("MoveMedia", MoveMedia);

            // Perf
            _db.DetachEntities<BaseEntity>();
        }

        #endregion

        #region Populate

        private async Task PopulateStores()
        {
            var stores = _data.Stores();
            await SaveRange(stores);
            _defaultStoreId = stores.First().Id;
        }

        private async Task PopulateTaxCategories()
        {
            var taxCategories = _data.TaxCategories();
            await SaveRange(taxCategories);

            // Add tax rates to fixed rate provider
            int i = 0;
            var taxIds = taxCategories.OrderBy(x => x.Id).Select(x => x.Id).ToList();
            foreach (var id in taxIds)
            {
                decimal rate = 0;
                if (_data.FixedTaxRates.Any() && _data.FixedTaxRates.Length > i)
                {
                    rate = _data.FixedTaxRates[i];
                }
                i++;

                _db.Settings.Add(new Setting 
                {
                    Name = string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", id).ToLowerInvariant(),
                    Value = rate.Convert<string>(),
                });
            }

            await _db.SaveChangesAsync();
        }

        private Task PopulateLanguage(Language primaryLanguage)
        {
            primaryLanguage.Published = true;
            return Save(primaryLanguage);
        }

        private async Task PopulateLocaleResources(Language language)
        {
            var appDataRoot = EngineContext.Current.Application.AppDataRoot;

            var locPath = appDataRoot.PathCombine("Localization/App/" + language.LanguageCulture);
            if (!appDataRoot.DirectoryExists(locPath))
            {
                // Fallback to neutral language folder (de, en etc.)
                locPath = appDataRoot.PathCombine("Localization/App/" + language.UniqueSeoCode);
            }

            var xmlResourceManager = XmlResourceManager;

            // Perf
            _db.DetachEntities<BaseEntity>();

            // Save resources
            foreach (var file in appDataRoot.EnumerateFiles(locPath, "*.smres.xml"))
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
                _db.DetachEntities<LocaleStringResource>();
            }

            // TODO: (core) Implement MigratorUtils.ExecutePendingResourceMigrations for installation
            //MigratorUtils.ExecutePendingResourceMigrations(locPath, _ctx);
        }

        private async Task PopulateCurrencies()
        {
            await SaveRange(_data.Currencies().Where(x => x != null));
        }

        private async Task PopulateCountriesAndStates()
        {
            await SaveRange(_data.Countries().Where(x => x != null));
        }

        private Task PopulateShippingMethods()
        {
            return SaveRange(_data.ShippingMethods(_config.SeedSampleData).Where(x => x != null));
        }

        private async Task PopulateCustomersAndUsers(string defaultUserEmail, string defaultUserPassword)
        {
            var customerRoles = _data.CustomerRoles(_config.SeedSampleData);
            await SaveRange(customerRoles.Where(x => x != null));

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
            await Save(adminUser);

            // Set default customer name
            var firstAddress = adminUser.Addresses.FirstOrDefault();
            _db.GenericAttributes.AddRange(new[] 
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

            await _db.SaveChangesAsync();

            // Built-in user for search engines (crawlers)
            var guestRole = customerRoles.FirstOrDefault(x => x.SystemName == SystemCustomerRoleNames.Guests);

            var customer = _data.SearchEngineUser();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
            await Save(customer);

            // Built-in user for background tasks
            customer = _data.BackgroundTaskUser();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
            await Save(customer);

            // Built-in user for the PDF converter
            customer = _data.PdfConverterUser();
            customer.CustomerRoleMappings.Add(new CustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = guestRole.Id });
            await Save(customer);
        }

        private async Task HashDefaultCustomerPassword(string defaultUserEmail, string defaultUserPassword)
        {
            var encryptor = new Encryptor(new SecuritySettings());
            var saltKey = encryptor.CreateSaltKey(5);
            var adminUser = await _db.Customers.FirstOrDefaultAsync(x => x.Email == _config.DefaultUserName);

            adminUser.PasswordSalt = saltKey;
            adminUser.PasswordFormat = PasswordFormat.Hashed;
            adminUser.Password = encryptor.CreatePasswordHash(defaultUserPassword, saltKey, new CustomerSettings().HashedPasswordFormat);

            await _db.SaveChangesAsync();
        }

        private async Task PopulateSettings()
        {
            var settings = _data.Settings();
            foreach (var setting in settings)
            {
                Type settingType = setting.GetType();
                int storeId = settingType.Equals(typeof(ThemeSettings)) ? _defaultStoreId : 0;
                await SettingFactory.SaveSettingsAsync(_db, setting, storeId);
            }
        }

        private async Task PopulateMessageTemplates()
        {
            var converter = new MessageTemplateConverter(_db, EngineContext.Current.Application);
            await converter.ImportAllAsync(_config.Language);
        }

        private async Task PopulateCategories()
        {
            var categoriesFirstLevel = _data.CategoriesFirstLevel();
            await SaveRange(categoriesFirstLevel);
            await PopulateUrlRecordsFor(categoriesFirstLevel);

            var categoriesSecondLevel = _data.CategoriesSecondLevel();
            await SaveRange(categoriesSecondLevel);
            await PopulateUrlRecordsFor(categoriesSecondLevel);
        }

        private async Task PopulateManufacturers()
        {
            var manufacturers = _data.Manufacturers();
            await SaveRange(manufacturers);
            await PopulateUrlRecordsFor(manufacturers);
        }

        private async Task PopulateProducts()
        {
            var products = _data.Products();
            await SaveRange(products);

            _data.AddDownloads(products);

            // Fix MainPictureId
            await ProductPictureHelper.FixProductMainPictureIds(_db);

            await PopulateUrlRecordsFor(products);

            _data.AssignGroupedProducts(products);
        }

        private async Task PopulateTopics()
        {
            var topics = _data.Topics();
            await SaveRange(topics);
            await PopulateUrlRecordsFor(topics);
        }

        private async Task MoveMedia()
        {
            if (_config.StoreMediaInDB)
            {
                return;
            }
            
            // All pictures have initially been stored in the DB. Move the binaries to disk as configured.
            var fileSystemStorageProvider = EngineContext.Current.ResolveService<Func<IMediaStorageProvider>>().Invoke();

            using (var scope = new DbContextScope(_db, autoDetectChanges: true))
            {
                var mediaFiles = await _db.MediaFiles
                    .Include(x => x.MediaStorage)
                    .Where(x => x.MediaStorageId != null)
                    .ToListAsync();

                foreach (var mediaFile in mediaFiles)
                {
                    if (mediaFile.MediaStorage?.Data?.LongLength > 0)
                    {
                        await fileSystemStorageProvider.SaveAsync(mediaFile, MediaStorageItem.FromStream(mediaFile.MediaStorage.Data.ToStream()));
                        mediaFile.MediaStorageId = null;
                        mediaFile.MediaStorage = null;
                    }
                }

                await scope.CommitAsync();
            }
        }

        #endregion

        #region Utils

        private async Task PopulateUrlRecordsFor<T>(IEnumerable<T> entities) 
            where T : BaseEntity, ISlugSupported, new()
        {
            using (var scope = UrlService.CreateBatchScope())
            {
                foreach (var entity in entities)
                {
                    var ur = _data.CreateUrlRecordFor(entity);
                    if (ur != null)
                    {
                        scope.ApplySlugs(new ValidateSlugResult 
                        {
                            Source = entity,
                            Found = ur,
                            Slug = ur.Slug,
                            LanguageId = 0,
                            FoundIsSelf = true,
                        });
                    }
                }

                await scope.CommitAsync();
            }
        }

        private async Task Populate<TEntity>(string stage, IEnumerable<TEntity> entities)
            where TEntity : BaseEntity
        {
            try
            {
                _logger.Debug("Populate: {0}", stage);
                await SaveRange(entities);
            }
            catch (Exception ex)
            {
                var ex2 = new SeedDataException(stage, ex);
                _logger.Error(ex2);
                throw ex2;
            }
        }

        private async Task Populate(string stage, Func<Task> populateAction)
        {
            try
            {
                _logger.Debug("Populate: {0}", stage);
                await populateAction();
            }
            catch (Exception ex)
            {
                var ex2 = new SeedDataException(stage, ex);
                _logger.Error(ex2);
                throw ex2;
            }
        }

        private void Populate(string stage, Action populateAction)
        {
            try
            {
                _logger.Debug("Populate: {0}", stage);
                populateAction();
            }
            catch (Exception ex)
            {
                var ex2 = new SeedDataException(stage, ex);
                _logger.Error(ex2);
                throw ex2;
            }
        }

        private Task Save<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            _db.Set<TEntity>().Add(entity);
            return _db.SaveChangesAsync();
        }

        private Task SaveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            _db.Set<TEntity>().AddRange(entities);
            return _db.SaveChangesAsync();
        }

        #endregion
    }
}
