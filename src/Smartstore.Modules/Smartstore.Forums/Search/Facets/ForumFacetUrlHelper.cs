using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Smartstore.Core;
using Smartstore.Core.Search.Facets;
using Smartstore.Forums.Search.Modelling;

namespace Smartstore.Forums.Search.Facets
{
    public partial class ForumFacetUrlHelper : FacetUrlHelperBase
    {
        private readonly static Dictionary<FacetGroupKind, string> _queryNames = new()
        {
            { FacetGroupKind.Forum, "f" },
            { FacetGroupKind.Customer, "c" },
            { FacetGroupKind.Date, "d" }
        };

        private readonly IWorkContext _workContext;
        private readonly IForumSearchQueryAliasMapper _forumAliasMapper;

        public ForumFacetUrlHelper(
            IHttpContextAccessor httpContextAccessor,
            IWorkContext workContext,
            IForumSearchQueryAliasMapper forumAliasMapper)
            : base(httpContextAccessor.HttpContext?.Request)
        {
            _workContext = workContext;
            _forumAliasMapper = forumAliasMapper;
        }

        public override int Order => 1;

        public override string Scope => ForumSearchService.Scope;

        protected override string GetUnmappedQueryName(Facet facet)
        {
            switch (facet.FacetGroup.Kind)
            {
                case FacetGroupKind.Forum:
                case FacetGroupKind.Customer:
                case FacetGroupKind.Date:
                    return _queryNames[facet.FacetGroup.Kind];
                default:
                    return null;
            }
        }

        protected override Dictionary<string, string> GetQueryParts(Facet facet)
        {
            var result = new Dictionary<string, string>();
            var languageId = _workContext.WorkingLanguage.Id;

            switch (facet.FacetGroup.Kind)
            {
                case FacetGroupKind.Forum:
                case FacetGroupKind.Customer:
                case FacetGroupKind.Date:
                    var value = facet.Value.ToString();
                    if (value.HasValue())
                    {
                        var name = _forumAliasMapper.GetCommonFacetAliasByGroupKind(facet.FacetGroup.Kind, languageId) ?? _queryNames[facet.FacetGroup.Kind];
                        result.Add(name, value);
                    }
                    break;
            }

            return result;
        }
    }
}
