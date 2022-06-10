using Smartstore.Core.Catalog;

namespace Smartstore.Web.Components
{
    public class HomeBrandsViewComponent : SmartViewComponent
    {
        private readonly CatalogHelper _catalogHelper;
        private readonly CatalogSettings _catalogSettings;

        public HomeBrandsViewComponent(CatalogHelper catalogHelper, CatalogSettings catalogSettings)
        {
            _catalogHelper = catalogHelper;
            _catalogSettings = catalogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (_catalogSettings.ManufacturerItemsToDisplayOnHomepage > 0 && _catalogSettings.ShowManufacturersOnHomepage)
            {
                var model = await _catalogHelper.PrepareBrandNavigationModelAsync(_catalogSettings.ManufacturerItemsToDisplayOnHomepage);
                if (model.Brands.Any())
                {
                    return View(model);
                }
            }

            return Empty();
        }
    }
}
