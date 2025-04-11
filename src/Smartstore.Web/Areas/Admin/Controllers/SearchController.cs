using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Search;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering;
using CatalogSearch = Smartstore.Core.Catalog.Search;

namespace Smartstore.Admin.Controllers
{
    public class SearchController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly MultiStoreSettingHelper _multiStoreSettingHelper;
        private readonly Lazy<ICatalogSearchQueryAliasMapper> _catalogSearchQueryAliasMapper;

        public SearchController(
            SmartDbContext db,
            ILanguageService languageService,
            MultiStoreSettingHelper multiStoreSettingHelper,
            Lazy<ICatalogSearchQueryAliasMapper> catalogSearchQueryAliasMapper)
        {
            _db = db;
            _languageService = languageService;
            _multiStoreSettingHelper = multiStoreSettingHelper;
            _catalogSearchQueryAliasMapper = catalogSearchQueryAliasMapper;
        }

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> SearchSettings(SearchSettings settings, int storeScope)
        {
            var megaSearchDescriptor = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.MegaSearch");
            var megaSearchPlusDescriptor = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.MegaSearchPlus");

            var model = new SearchSettingsModel();
            MiniMapper.Map(settings, model);

            model.IsMegaSearchInstalled = megaSearchDescriptor != null;

            PrepareSearchConfigModel(model, settings, megaSearchPlusDescriptor);

            // Common facets.
            model.CategoryFacet.Sorting = settings.CategorySorting;
            model.BrandFacet.Disabled = settings.BrandDisabled;
            model.BrandFacet.DisplayOrder = settings.BrandDisplayOrder;
            model.BrandFacet.Sorting = settings.BrandSorting;
            model.PriceFacet.Disabled = settings.PriceDisabled;
            model.PriceFacet.DisplayOrder = settings.PriceDisplayOrder;
            model.RatingFacet.Disabled = settings.RatingDisabled;
            model.RatingFacet.DisplayOrder = settings.RatingDisplayOrder;
            model.DeliveryTimeFacet.Disabled = settings.DeliveryTimeDisabled;
            model.DeliveryTimeFacet.DisplayOrder = settings.DeliveryTimeDisplayOrder;
            model.DeliveryTimeFacet.Sorting = settings.DeliveryTimeSorting;
            model.AvailabilityFacet.Disabled = settings.AvailabilityDisabled;
            model.AvailabilityFacet.DisplayOrder = settings.AvailabilityDisplayOrder;
            model.AvailabilityFacet.IncludeNotAvailable = settings.IncludeNotAvailable;
            model.NewArrivalsFacet.Disabled = settings.NewArrivalsDisabled;
            model.NewArrivalsFacet.DisplayOrder = settings.NewArrivalsDisplayOrder;

            await _multiStoreSettingHelper.DetectOverrideKeysAsync(settings, model);

            // Localized facet settings (CommonFacetSettingsLocalizedModel).
            var i = 0;
            var languages = await _languageService.GetAllLanguagesAsync(true);
            foreach (var language in languages)
            {
                var categoryFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Category, language.Id);
                var brandFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Brand, language.Id);
                var priceFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Price, language.Id);
                var ratingFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Rating, language.Id);
                var deliveryTimeFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.DeliveryTime, language.Id);
                var availabilityFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Availability, language.Id);
                var newArrivalsFacetAliasSettingsKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.NewArrivals, language.Id);

                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"CategoryFacet.Locales[{i}].Alias", categoryFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"BrandFacet.Locales[{i}].Alias", brandFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"PriceFacet.Locales[{i}].Alias", priceFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"RatingFacet.Locales[{i}].Alias", ratingFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"DeliveryTimeFacet.Locales[{i}].Alias", deliveryTimeFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"AvailabilityFacet.Locales[{i}].Alias", availabilityFacetAliasSettingsKey);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync($"NewArrivalsFacet.Locales[{i}].Alias", newArrivalsFacetAliasSettingsKey);

                model.CategoryFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(categoryFacetAliasSettingsKey, storeId: storeScope)
                });
                model.BrandFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(brandFacetAliasSettingsKey, storeId: storeScope)
                });
                model.PriceFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(priceFacetAliasSettingsKey, storeId: storeScope)
                });
                model.RatingFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(ratingFacetAliasSettingsKey, storeId: storeScope)
                });
                model.DeliveryTimeFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(deliveryTimeFacetAliasSettingsKey, storeId: storeScope)
                });
                model.AvailabilityFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(availabilityFacetAliasSettingsKey, storeId: storeScope)
                });
                model.NewArrivalsFacet.Locales.Add(new CommonFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(newArrivalsFacetAliasSettingsKey, storeId: storeScope)
                });

                i++;
            }

            // Facet settings (CommonFacetSettingsModel).
            foreach (var prefix in new[] { "Brand", "Price", "Rating", "DeliveryTime", "Availability", "NewArrivals" })
            {
                await _multiStoreSettingHelper.DetectOverrideKeyAsync(prefix + "Facet.Disabled", prefix + "Disabled", settings);
                await _multiStoreSettingHelper.DetectOverrideKeyAsync(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", settings);
            }

            await _multiStoreSettingHelper.DetectOverrideKeyAsync("CategoryFacet.Sorting", nameof(CatalogSearch.SearchSettings.CategorySorting), settings);
            await _multiStoreSettingHelper.DetectOverrideKeyAsync("BrandFacet.Sorting", nameof(CatalogSearch.SearchSettings.BrandSorting), settings);
            await _multiStoreSettingHelper.DetectOverrideKeyAsync("DeliveryTimeFacet.Sorting", nameof(CatalogSearch.SearchSettings.DeliveryTimeSorting), settings);

            // Facet settings with a non-prefixed name.
            await _multiStoreSettingHelper.DetectOverrideKeyAsync("AvailabilityFacet.IncludeNotAvailable", nameof(CatalogSearch.SearchSettings.IncludeNotAvailable), settings);

            return View(model);
        }

        // INFO: do not use SaveSetting attribute here because it would delete all previously added facet settings if storeScope > 0.
        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, LoadSetting]
        public async Task<IActionResult> SearchSettings(SearchSettingsModel model, SearchSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return await SearchSettings(settings, storeScope);
            }

            var form = Request.Form;
            CategoryTreeChangeReason? categoriesChange = model.AvailabilityFacet.IncludeNotAvailable != settings.IncludeNotAvailable
                ? CategoryTreeChangeReason.ElementCounts
                : null;

            ModelState.Clear();

            settings = ((ISettings)settings).Clone() as SearchSettings;
            MiniMapper.Map(model, settings);

            // Common facets.
            settings.CategorySorting = model.CategoryFacet.Sorting;
            settings.BrandDisabled = model.BrandFacet.Disabled;
            settings.BrandDisplayOrder = model.BrandFacet.DisplayOrder;
            settings.BrandSorting = model.BrandFacet.Sorting;
            settings.PriceDisabled = model.PriceFacet.Disabled;
            settings.PriceDisplayOrder = model.PriceFacet.DisplayOrder;
            settings.RatingDisabled = model.RatingFacet.Disabled;
            settings.RatingDisplayOrder = model.RatingFacet.DisplayOrder;
            settings.DeliveryTimeDisabled = model.DeliveryTimeFacet.Disabled;
            settings.DeliveryTimeDisplayOrder = model.DeliveryTimeFacet.DisplayOrder;
            settings.DeliveryTimeSorting = model.DeliveryTimeFacet.Sorting;
            settings.AvailabilityDisabled = model.AvailabilityFacet.Disabled;
            settings.AvailabilityDisplayOrder = model.AvailabilityFacet.DisplayOrder;
            settings.IncludeNotAvailable = model.AvailabilityFacet.IncludeNotAvailable;
            settings.NewArrivalsDisabled = model.NewArrivalsFacet.Disabled;
            settings.NewArrivalsDisplayOrder = model.NewArrivalsFacet.DisplayOrder;

            await _multiStoreSettingHelper.UpdateSettingsAsync(settings, form);

            // Facet settings (CommonFacetSettingsModel).
            if (storeScope != 0)
            {
                foreach (var prefix in new[] { "Brand", "Price", "Rating", "DeliveryTime", "Availability", "NewArrivals" })
                {
                    await _multiStoreSettingHelper.ApplySettingAsync(prefix + "Facet.Disabled", prefix + "Disabled", settings, form);
                    await _multiStoreSettingHelper.ApplySettingAsync(prefix + "Facet.DisplayOrder", prefix + "DisplayOrder", settings, form);
                }

                await _multiStoreSettingHelper.ApplySettingAsync("CategoryFacet.Sorting", nameof(CatalogSearch.SearchSettings.CategorySorting), settings, form);
                await _multiStoreSettingHelper.ApplySettingAsync("BrandFacet.Sorting", nameof(CatalogSearch.SearchSettings.BrandSorting), settings, form);
                await _multiStoreSettingHelper.ApplySettingAsync("DeliveryTimeFacet.Sorting", nameof(CatalogSearch.SearchSettings.DeliveryTimeSorting), settings, form);
            }

            // Facet settings with a non-prefixed name.
            await _multiStoreSettingHelper.ApplySettingAsync("AvailabilityFacet.IncludeNotAvailable", nameof(CatalogSearch.SearchSettings.IncludeNotAvailable), settings, form);

            // Localized facet settings (CommonFacetSettingsLocalizedModel).
            var num = 0;
            num += await ApplyLocalizedFacetSettings(model.CategoryFacet, FacetGroupKind.Category, storeScope);
            num += await ApplyLocalizedFacetSettings(model.BrandFacet, FacetGroupKind.Brand, storeScope);
            num += await ApplyLocalizedFacetSettings(model.PriceFacet, FacetGroupKind.Price, storeScope);
            num += await ApplyLocalizedFacetSettings(model.RatingFacet, FacetGroupKind.Rating, storeScope);
            num += await ApplyLocalizedFacetSettings(model.DeliveryTimeFacet, FacetGroupKind.DeliveryTime, storeScope);
            num += await ApplyLocalizedFacetSettings(model.AvailabilityFacet, FacetGroupKind.Availability, storeScope);
            num += await ApplyLocalizedFacetSettings(model.NewArrivalsFacet, FacetGroupKind.NewArrivals, storeScope);

            await _db.SaveChangesAsync();

            if (num > 0)
            {
                await _catalogSearchQueryAliasMapper.Value.ClearCommonFacetCacheAsync();
            }

            if (categoriesChange.HasValue)
            {
                await Services.EventPublisher.PublishAsync(new CategoryTreeChangedEvent(categoriesChange.Value));
            }

            await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, settings, form));

            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(nameof(SearchSettings));
        }

        private async Task<int> ApplyLocalizedFacetSettings(CommonFacetSettingsModel model, FacetGroupKind kind, int storeId = 0)
        {
            var num = 0;

            foreach (var localized in model.Locales)
            {
                var key = FacetUtility.GetFacetAliasSettingKey(kind, localized.LanguageId);
                var existingAlias = Services.Settings.GetSettingByKey<string>(key, storeId: storeId);

                if (existingAlias.EqualsNoCase(localized.Alias))
                {
                    continue;
                }

                if (localized.Alias.HasValue())
                {
                    await Services.Settings.ApplySettingAsync(key, localized.Alias, storeId);
                }
                else
                {
                    await Services.Settings.RemoveSettingAsync(key, storeId);
                }

                num++;
            }

            return num;
        }

        private void PrepareSearchConfigModel(SearchSettingsModel model, SearchSettings searchSettings, IModuleDescriptor megaSearchPlusDescriptor)
        {
            var availableSearchFields = new List<SelectListItem>();
            var availableSearchModes = new List<SelectListItem>();

            if (!model.IsMegaSearchInstalled)
            {
                model.SearchFieldsNote = T("Admin.Configuration.Settings.Search.SearchFieldsNote");

                availableSearchFields.AddRange(
                [
                    new() { Text = T("Admin.Catalog.Products.Fields.ShortDescription"), Value = "shortdescription" },
                    new() { Text = T("Admin.Catalog.Products.Fields.Sku"), Value = "sku" },
                ]);

                availableSearchModes = searchSettings.SearchMode.ToSelectList().Where(x => x.Value.ToInt() != (int)SearchMode.ExactMatch).ToList();
            }
            else
            {
                availableSearchFields.AddRange(
                [
                    new() { Text = T("Admin.Catalog.Products.Fields.ShortDescription"), Value = "shortdescription" },
                    new() { Text = T("Admin.Catalog.Products.Fields.FullDescription"), Value = "fulldescription" },
                    new() { Text = T("Admin.Catalog.Products.Fields.ProductTags"), Value = "tagname" },
                    new() { Text = T("Common.Keywords"), Value = "keyword" },
                    new() { Text = T("Admin.Catalog.Manufacturers"), Value = "manufacturer" },
                    new() { Text = T("Admin.Catalog.Categories"), Value = "category" },
                    new() { Text = T("Admin.Catalog.Products.Fields.Sku"), Value = "sku" },
                    new() { Text = T("Admin.Catalog.Products.Fields.GTIN"), Value = "gtin" },
                    new() { Text = T("Admin.Catalog.Products.Fields.ManufacturerPartNumber"), Value = "mpn" }
                ]);

                if (megaSearchPlusDescriptor != null)
                {
                    availableSearchFields.AddRange(
                    [
                        new() { Text = T("Search.Fields.SpecificationAttributeOptionName"), Value = "attrname" },
                        new() { Text = T("Search.Fields.ProductAttributeOptionName"), Value = "variantname" }
                    ]);
                }

                availableSearchModes = [.. searchSettings.SearchMode.ToSelectList()];
            }

            ViewBag.AvailableSearchFields = availableSearchFields;
            ViewBag.AvailableSearchModes = availableSearchModes;
            ViewBag.AvailableProductSortings = ProductController.CreateProductSortingsList(model.DefaultSortOrder, Services);

            var facetSortings = Enum.GetValues(typeof(FacetSorting)).Cast<FacetSorting>();
            if (!model.IsMegaSearchInstalled)
            {
                facetSortings = facetSortings.Where(x => x != FacetSorting.HitsDesc);
            }

            ViewBag.FacetSortings = facetSortings
                .Select(x => new SelectListItem { Text = Services.Localization.GetLocalizedEnum(x), Value = ((int)x).ToString() })
                .ToList();
        }
    }
}
