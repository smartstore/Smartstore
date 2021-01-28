using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Platform.Search.Facets;

namespace Smartstore.Core.Search.Facets
{
    public partial class CatalogFacetUrlHelper : FacetUrlHelperBase
    {
        private readonly static IDictionary<FacetGroupKind, string> _queryNames = new Dictionary<FacetGroupKind, string>
        {
			{ FacetGroupKind.Brand, "m" },
            { FacetGroupKind.Category, "c" },
            { FacetGroupKind.Price, "p" },
            { FacetGroupKind.Rating, "r" },
            { FacetGroupKind.DeliveryTime, "d" },
            { FacetGroupKind.Availability, "a" },
            { FacetGroupKind.NewArrivals, "n" },
        };

        private readonly IWorkContext _workContext;
        private readonly ICatalogSearchQueryAliasMapper _catalogAliasMapper;

        public CatalogFacetUrlHelper(
            IHttpContextAccessor httpContextAccessor,
            IWorkContext workContext,
            ICatalogSearchQueryAliasMapper catalogAliasMapper)
            : base(httpContextAccessor.HttpContext?.Request)
        {
            _workContext = workContext;
            _catalogAliasMapper = catalogAliasMapper;
        }

        public override int Order => 0;

        public override string Scope => CatalogSearchService.Scope;

        protected override string GetUnmappedQueryName(Facet facet)
        {
            switch (facet.FacetGroup.Kind)
            {
                case FacetGroupKind.Attribute:
                    return "attr" + facet.Value.ParentId;
                case FacetGroupKind.Variant:
                    return "vari" + facet.Value.ParentId;
                case FacetGroupKind.Category:
                case FacetGroupKind.Brand:
                case FacetGroupKind.Price:
                case FacetGroupKind.Rating:
                case FacetGroupKind.DeliveryTime:
                case FacetGroupKind.Availability:
                case FacetGroupKind.NewArrivals:
                    return _queryNames[facet.FacetGroup.Kind];
                default:
                    return null;
            }
        }

        protected override async Task<NameValueCollection> GetQueryPartsAsync(Facet facet)
        {
            var result = new NameValueCollection(2);
            string name;
            string value;
            int entityId;
            var val = facet.Value;
            var languageId = _workContext.WorkingLanguage.Id;

            switch (facet.FacetGroup.Kind)
            {
                case FacetGroupKind.Attribute:
                    if (facet.Value.TypeCode == IndexTypeCode.Double)
                    {
                        value = "{0}~{1}".FormatInvariant(
                            val.Value != null ? ((double)val.Value).ToString(CultureInfo.InvariantCulture) : string.Empty,
                            val.UpperValue != null ? ((double)val.UpperValue).ToString(CultureInfo.InvariantCulture) : string.Empty);
                    }
                    else
                    {
                        entityId = val.Value.Convert<int>();
                        value = await _catalogAliasMapper.GetAttributeOptionAliasByIdAsync(entityId, languageId) ?? "opt" + entityId;
                    }
                    name = await _catalogAliasMapper.GetAttributeAliasByIdAsync(val.ParentId, languageId) ?? "attr" + val.ParentId;
                    result.Add(name, value);
                    break;

                case FacetGroupKind.Variant:
                    entityId = val.Value.Convert<int>();
                    name = await _catalogAliasMapper.GetVariantAliasByIdAsync(val.ParentId, languageId) ?? "vari" + val.ParentId;
                    value = await _catalogAliasMapper.GetVariantOptionAliasByIdAsync(entityId, languageId) ?? "opt" + entityId;
                    result.Add(name, value);
                    break;

                case FacetGroupKind.Category:
                case FacetGroupKind.Brand:
                case FacetGroupKind.Price:
                case FacetGroupKind.Rating:
                case FacetGroupKind.DeliveryTime:
                case FacetGroupKind.Availability:
                case FacetGroupKind.NewArrivals:
                    value = val.ToString();
                    if (value.HasValue())
                    {
                        name = _catalogAliasMapper.GetCommonFacetAliasByGroupKind(facet.FacetGroup.Kind, languageId) ?? _queryNames[facet.FacetGroup.Kind];
                        result.Add(name, value);
                    }
                    break;
            }

            return result;
        }
    }
}
