using Microsoft.AspNetCore.Mvc;
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

        public IViewComponentResult Invoke()
        {
            var model = new SearchBoxModel
            {
                Origin = "Search/Search",
                InstantSearchUrl = Url.RouteUrl("InstantSearch"),
                InputPlaceholder = T("Search.SearchBox.Tooltip"),
                InstantSearchEnabled = _searchSettings.InstantSearchEnabled && Services.Permissions.Authorize(Permissions.System.AccessShop),
                ShowThumbsInInstantSearch = _searchSettings.ShowProductImagesInInstantSearch,
                SearchTermMinimumLength = _searchSettings.InstantSearchTermMinLength,
                CurrentQuery = _queryFactory.Current?.Term
            };

            return View(model);
        }
    }
}
