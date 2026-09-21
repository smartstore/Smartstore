using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers;

public partial class ProductController : AdminController
{
    [Permission(Permissions.Configuration.Setting.Read)]
    [LoadSetting]
    public async Task<IActionResult> CatalogSettings(int storeScope, CatalogSettings catalogSettings, PriceSettings priceSettings)
    {
        var model = await MapperFactory.MapAsync<CatalogSettings, CatalogSettingsModel>(catalogSettings);
        await MapperFactory.MapAsync(catalogSettings, model.GroupedProductSettings);
        await MapperFactory.MapAsync(priceSettings, model.PriceSettings);

        await PrepareCatalogConfigurationModel(model, catalogSettings);

        AddLocales(model.Locales, (locale, languageId) =>
        {
            locale.OfferBadgeLabel = priceSettings.GetLocalizedSetting(x => x.OfferBadgeLabel, languageId, storeScope, false, false);
            locale.LimitedOfferBadgeLabel = priceSettings.GetLocalizedSetting(x => x.LimitedOfferBadgeLabel, languageId, storeScope, false, false);
        });

        AddLocales(model.GroupedProductSettings.Locales, (locale, languageId) =>
        {
            locale.AssociatedProductsTitle = catalogSettings.GetLocalizedSetting(x => x.AssociatedProductsTitle, languageId, storeScope, false, false);
        });

        return View(model);
    }

    [Permission(Permissions.Configuration.Setting.Update)]
    [HttpPost, SaveSetting]
    public async Task<IActionResult> CatalogSettings(
        int storeScope,
        CatalogSettingsModel model,
        CatalogSettings catalogSettings,
        PriceSettings priceSettings,
        GroupedProductSettingsModel groupedProductSettings)
    {
        if (!ModelState.IsValid)
        {
            return await CatalogSettings(storeScope, catalogSettings, priceSettings);
        }

        ModelState.Clear();

        // We need to clear the sitemap cache if MaxItemsToDisplayInCatalogMenu has changed.
        if (catalogSettings.MaxItemsToDisplayInCatalogMenu != model.MaxItemsToDisplayInCatalogMenu
            || catalogSettings.ShowCategoryProductNumberIncludingSubcategories != model.ShowCategoryProductNumberIncludingSubcategories)
        {
            // Clear cached navigation model.
            await _menuService.Value.ClearCacheAsync("Main");
        }

        await MapperFactory.MapAsync(model, catalogSettings);
        await MapperFactory.MapAsync(groupedProductSettings, catalogSettings);
        await MapperFactory.MapAsync(model.PriceSettings, priceSettings);

        catalogSettings.LegalInfoInProductDetail = ProductLegalInfo.None;
        model?.LegalInfoInProductDetail?.Each(x => catalogSettings.LegalInfoInProductDetail |= (ProductLegalInfo)x);

        catalogSettings.LegalInfoInLists = ProductLegalInfo.None;
        model?.LegalInfoInLists?.Each(x => catalogSettings.LegalInfoInLists |= (ProductLegalInfo)x);

        foreach (var localized in model.Locales)
        {
            await _localizedEntityService.ApplyLocalizedSettingAsync(priceSettings, x => x.OfferBadgeLabel, localized.OfferBadgeLabel, localized.LanguageId, storeScope);
            await _localizedEntityService.ApplyLocalizedSettingAsync(priceSettings, x => x.LimitedOfferBadgeLabel, localized.LimitedOfferBadgeLabel, localized.LanguageId, storeScope);
        }

        foreach (var localized in groupedProductSettings.Locales)
        {
            await _localizedEntityService.ApplyLocalizedSettingAsync(catalogSettings, x => x.AssociatedProductsTitle, localized.AssociatedProductsTitle, localized.LanguageId, storeScope);
        }

        NotifySuccess(T("Admin.Configuration.Updated"));
        return RedirectToAction(nameof(CatalogSettings));
    }

    private async Task PrepareCatalogConfigurationModel(CatalogSettingsModel model, CatalogSettings settings)
    {
        var localization = Services.Localization;

        ViewBag.AvailableDefaultViewModes = new List<SelectListItem>
        {
            new() { Value = "grid", Text = T("Common.Grid"), Selected = model.DefaultViewMode.EqualsNoCase("grid") },
            new() { Value = "list", Text = T("Common.List"), Selected = model.DefaultViewMode.EqualsNoCase("list") }
        };

        var priceLabels = await _db.PriceLabels
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

        ViewBag.AvailableDefaultComparePriceLabels = new List<SelectListItem>();
        ViewBag.AvailableDefaultRegularPriceLabels = new List<SelectListItem>();

        foreach (var label in priceLabels)
        {
            ViewBag.AvailableDefaultComparePriceLabels.Add(new SelectListItem
            {
                Value = label.Id.ToString(),
                Text = label.GetLocalized(x => x.ShortName),
                Selected = model.PriceSettings.DefaultComparePriceLabelId == label.Id
            });

            ViewBag.AvailableDefaultRegularPriceLabels.Add(new SelectListItem
            {
                Value = label.Id.ToString(),
                Text = label.GetLocalized(x => x.ShortName),
                Selected = model.PriceSettings.DefaultRegularPriceLabelId == label.Id
            });
        }

        ViewBag.LimitedOfferBadgeStyles = AddBadgeStyles(model.PriceSettings.LimitedOfferBadgeStyle);
        ViewBag.OfferBadgeStyles = AddBadgeStyles(model.PriceSettings.OfferBadgeStyle);
        ViewBag.AssociatedProductsHeaderFields = CreateAssociatedProductsHeaderFieldsList(settings.CollapsibleAssociatedProductsHeaders, T);
        ViewBag.AvailableProductSortings = CreateProductSortingsList(model.DefaultSortOrder, Services);

        var legalInfos = Enum.GetValues<ProductLegalInfo>()
            .Where(x => x != ProductLegalInfo.None && x != ProductLegalInfo.All)
            .ToArray();

        model.LegalInfoInProductDetail = legalInfos
            .Where(x => settings.LegalInfoInProductDetail.HasFlag(x))
            .Select(x => (int)x)
            .ToArray();

        model.LegalInfoInLists = legalInfos
            .Where(x => settings.LegalInfoInLists.HasFlag(x))
            .Select(x => (int)x)
            .ToArray();

        ViewBag.ProductLegalInfos = legalInfos
            .Select(x => new SelectListItem
            {
                Value = ((int)x).ToString(),
                Text = localization.GetLocalizedEnum(x)
            })
            .ToList();

        ViewData[nameof(CatalogSettingsModel.DefaultSwatchSize) + "RangeTicks"] = Enum.GetValues<SwatchSize>()
            .Select(x => localization.GetLocalizedEnum(x))
            .ToList();

        static List<SelectListItem> AddBadgeStyles(string value)
        {
            return [.. Enum.GetNames<BadgeStyle>().Select(x => new SelectListItem { Text = x, Value = x.ToLower(), Selected = value.EqualsNoCase(x) })];
        }
    }

    internal static List<SelectListItem> CreateProductSortingsList(ProductSortingEnum selectedSorting, ICommonServices services)
    {
        var language = services.WorkContext.WorkingLanguage;

        return [.. Enum.GetValues<ProductSortingEnum>()
            .Where(x => x != ProductSortingEnum.CreatedOnAsc && x != ProductSortingEnum.Initial)
            .Select(x => new SelectListItem
            {
                Text = services.Localization.GetLocalizedEnum(x, language.Id),
                Value = ((int)x).ToString(),
                Selected = x == selectedSorting
            })];
    }
}