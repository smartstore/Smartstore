using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Security;
using Smartstore.Forums.Search.Modelling;
using Smartstore.Web.Components;
using Smartstore.Web.Models.Search;

namespace Smartstore.Forums.Components
{
    public class ForumSearchBoxViewComponent : SmartViewComponent
    {
        private readonly IForumSearchQueryFactory _queryFactory;
        private readonly ForumSearchSettings _searchSettings;

        public ForumSearchBoxViewComponent(IForumSearchQueryFactory queryFactory, ForumSearchSettings searchSettings)
        {
            _queryFactory = queryFactory;
            _searchSettings = searchSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentTerm = _queryFactory.Current?.Term;

            var model = new SearchBoxModel
            {
                Origin = "Boards/Search",
                SearchUrl = Url.RouteUrl("ForumSearch"),
                InstantSearchUrl = Url.RouteUrl("ForumInstantSearch"),
                InputPlaceholder = T("Forum.SearchForumsTooltip"),
                InstantSearchEnabled = _searchSettings.InstantSearchEnabled && await Services.Permissions.AuthorizeAsync(Permissions.System.AccessShop),
                SearchTermMinimumLength = _searchSettings.InstantSearchTermMinLength,
                CurrentQuery = currentTerm
            };

            return View("~/Views/Shared/Components/SearchBox/Default.cshtml", model);
        }
    }
}
