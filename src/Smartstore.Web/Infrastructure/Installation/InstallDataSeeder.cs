using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Smartstore.Caching;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Domain;

namespace Smartstore.Web.Infrastructure.Installation
{
    public partial class InstallDataSeeder : IDataSeeder<SmartDbContext>
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

        public InstallDataSeeder(SeedDataConfiguration configuration, ILogger logger, IHttpContextAccessor httpContextAccessor)
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

        public Task SeedAsync(SmartDbContext context)
        {
            throw new NotImplementedException();
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
