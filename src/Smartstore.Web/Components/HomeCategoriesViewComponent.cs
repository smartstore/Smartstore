using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Web.Components
{
    public class HomeCategoriesViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly CatalogHelper _catalogHelper;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly MediaSettings _mediaSettings;

        public HomeCategoriesViewComponent(
            SmartDbContext db,
            CatalogHelper catalogHelper,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            MediaSettings mediaSettings)
        {
            _db = db;
            _catalogHelper = catalogHelper;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _mediaSettings = mediaSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .ApplyStandardFilter(false)
                .Where(x => x.ShowOnHomePage)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            // ACL and store mapping
            categories = await categories
                .WhereAwait(async c => (await _aclService.AuthorizeAsync(c)) && (await _storeMappingService.AuthorizeAsync(c)))
                .AsyncToList();

            var model = await _catalogHelper.MapCategorySummaryModelAsync(categories, _mediaSettings.CategoryThumbPictureSize);
            if (model.Count == 0)
            {
                return Empty();
            }

            return View(model);
        }
    }
}
