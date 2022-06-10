using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Search;

namespace Smartstore.Web.Components
{
    public class SearchBoxViewComponent : SmartViewComponent
    {
        private readonly ICatalogSearchQueryFactory _queryFactory;
        private readonly SearchSettings _searchSettings;

        public SearchBoxViewComponent(ICatalogSearchQueryFactory queryFactory, SearchSettings searchSettings)
        {
            _queryFactory = queryFactory;
            _searchSettings = searchSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new SearchBoxModel
            {
                Origin = "Search/Search",
                SearchUrl = Url.RouteUrl("Search"),
                InstantSearchUrl = Url.RouteUrl("InstantSearch"),
                InputPlaceholder = T("Search.SearchBox.Tooltip"),
                InstantSearchEnabled = _searchSettings.InstantSearchEnabled && await Services.Permissions.AuthorizeAsync(Permissions.System.AccessShop),
                ShowThumbsInInstantSearch = _searchSettings.ShowProductImagesInInstantSearch,
                SearchTermMinimumLength = _searchSettings.InstantSearchTermMinLength,
                CurrentQuery = _queryFactory.Current?.Term
            };

            return View(model);
        }
    }
}
