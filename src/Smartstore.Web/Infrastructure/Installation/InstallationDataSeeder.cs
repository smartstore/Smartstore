using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Smartstore.Caching;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Domain;

namespace Smartstore.Web.Infrastructure.Installation
{
    public partial class InstallationDataSeeder : IDataSeeder<SmartDbContext>
    {
        private readonly SeedDataConfiguration _config;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private SmartDbContext _ctx;
        private InvariantSeedData _data;
        //private ISettingService _settingService;
        //private IGenericAttributeService _gaService;
        //private ILocalizationService _locService;
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
                        _ctx,
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

        #region IDataSeeder

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context)
        {
            Guard.NotNull(context, nameof(context));

            _ctx = context;
            _data.Initialize(_ctx);

            _ctx.ChangeTracker.AutoDetectChangesEnabled = false;
            _ctx.MinHookImportance = HookImportance.Essential;

            _config.ProgressMessageCallback("Progress.CreatingRequiredData");

            // special mandatory (non-visible) settings
            await _ctx.MigrateSettingsAsync(x =>
            {
                x.Add("Media.Storage.Provider", _config.StoreMediaInDB ? DatabaseMediaStorageProvider.SystemName : FileSystemMediaStorageProvider.SystemName);
            });

            await Populate("PopulatePictures", _data.Pictures);
            await Populate("PopulateCurrencies", PopulateCurrencies);
            await Populate("PopulateStores", PopulateStores);
            await Populate("InstallLanguages", () => PopulateLanguage(_config.Language));
            await Populate("PopulateMeasureDimensions", _data.MeasureDimensions());
            await Populate("PopulateMeasureWeights", _data.MeasureWeights());
            await Populate("PopulateTaxCategories", PopulateTaxCategories);
            //await Populate("PopulateCountriesAndStates", PopulateCountriesAndStates);
            //await Populate("PopulateShippingMethods", PopulateShippingMethods);
            await Populate("PopulateDeliveryTimes", _data.DeliveryTimes());
            await Populate("PopulateQuantityUnits", _data.QuantityUnits());
            //await Populate("PopulateCustomersAndUsers", () => PopulateCustomersAndUsers(_config.DefaultUserName, _config.DefaultUserPassword));
            await Populate("PopulateEmailAccounts", _data.EmailAccounts());
            //await Populate("PopulateMessageTemplates", PopulateMessageTemplates);
            //await Populate("PopulateTopics", PopulateTopics);
            //await Populate("PopulateSettings", PopulateSettings);
            await Populate("PopulateActivityLogTypes", _data.ActivityLogTypes());
            //await Populate("PopulateCustomersAndUsers", () => HashDefaultCustomerPassword(_config.DefaultUserName, _config.DefaultUserPassword));
            await Populate("PopulateProductTemplates", _data.ProductTemplates());
            await Populate("PopulateCategoryTemplates", _data.CategoryTemplates());
            //await Populate("PopulateManufacturerTemplates", PopulateManufacturerTemplates);
            await Populate("PopulateScheduleTasks", _data.TaskDescriptors());
            await Populate("PopulateLocaleResources", PopulateLocaleResources);
            //await Populate("PopulateMenus", PopulateMenus);

            if (_config.SeedSampleData)
            {
                _logger.Info("Seeding sample data");

                _config.ProgressMessageCallback("Progress.CreatingSampleData");

                await Populate("PopulateSpecificationAttributes", _data.SpecificationAttributes());
                await Populate("PopulateProductAttributes", _data.ProductAttributes());
                await Populate("PopulateProductAttributeOptionsSets", _data.ProductAttributeOptionsSets);
                await Populate("PopulateProductAttributeOptions", _data.ProductAttributeOptions);
                await Populate("PopulateCampaigns", _data.Campaigns());
                await Populate("PopulateRuleSets", _data.RuleSets);
                await Populate("PopulateDiscounts", _data.Discounts);
                //await Populate("PopulateCategories", PopulateCategories);
                //await Populate("PopulateManufacturers", PopulateManufacturers);
                //await Populate("PopulateProducts", PopulateProducts);
                await Populate("PopulateProductBundleItems", _data.ProductBundleItems);
                await Populate("PopulateProductVariantAttributes", _data.ProductVariantAttributes);
                await Populate("ProductVariantAttributeCombinations", _data.ProductVariantAttributeCombinations);
                await Populate("PopulateProductTags", _data.ProductTags);
                //////await Populate("PopulateForumsGroups", _data.ForumGroups());
                //////await Populate("PopulateForums", _data.Forums());
                //////await Populate("PopulateBlogPosts", PopulateBlogPosts);
                //////await Populate("PopulateNews", PopulateNewsItems);
                //////await Populate("PopulatePolls", _data.Polls());
                await Populate("FinalizeSamples", _data.FinalizeSamples);
            }

            //Populate("MovePictures", MoveMedia);

            // Perf
            _ctx.DetachEntities<BaseEntity>();
        }

        #endregion

        #region Populate

        private async Task PopulateStores()
        {
            var stores = await _data.Stores();
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

                _ctx.Settings.Add(new Setting 
                {
                    Name = string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", id).ToLowerInvariant(),
                    Value = rate.Convert<string>(),
                });
            }

            await _ctx.SaveChangesAsync();
        }

        private Task PopulateLanguage(Language primaryLanguage)
        {
            primaryLanguage.Published = true;
            return Save(primaryLanguage);
        }

        private Task PopulateLocaleResources()
        {
            // TODO: (core) Implement PopulateLocaleResources
            return Task.CompletedTask;
        }

        private async Task PopulateCurrencies()
        {
            await SaveRange(_data.Currencies().Where(x => x != null));
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
                            FoundIsSelf = true
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

        private Task Save<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            _ctx.Set<TEntity>().Add(entity);
            return _ctx.SaveChangesAsync();
        }

        private Task SaveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : BaseEntity
        {
            _ctx.Set<TEntity>().AddRange(entities);
            return _ctx.SaveChangesAsync();
        }

        #endregion
    }
}
