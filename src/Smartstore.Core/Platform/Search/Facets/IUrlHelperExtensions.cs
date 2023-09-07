using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Search.Facets
{
    public static partial class IUrlHelperExtensions
    {
        public static string FacetToggle(this IUrlHelper urlHelper, Facet facet)
        {
            Guard.NotNull(facet);

            var facetUrlProvider = EngineContext.Current.ResolveService<IFacetUrlHelperProvider>();
            var facetUrlHelper = facetUrlProvider.GetUrlHelper(facet.FacetGroup.Scope);

            return facetUrlHelper.Toggle(facet);
        }

        public static string FacetAdd(this IUrlHelper urlHelper, params Facet[] facets)
        {
            Guard.NotEmpty(facets);

            var facetUrlProvider = EngineContext.Current.ResolveService<IFacetUrlHelperProvider>();
            var facetUrlHelper = facetUrlProvider.GetUrlHelper(facets.First().FacetGroup.Scope);

            return facetUrlHelper.Add(facets);
        }

        public static string FacetRemove(this IUrlHelper urlHelper, params Facet[] facets)
        {
            Guard.NotEmpty(facets);

            var facetUrlProvider = EngineContext.Current.ResolveService<IFacetUrlHelperProvider>();
            var facetUrlHelper = facetUrlProvider.GetUrlHelper(facets.First().FacetGroup.Scope);

            return facetUrlHelper.Remove(facets);
        }

        public static string GetFacetQueryName(this IUrlHelper urlHelper, Facet facet)
        {
            Guard.NotNull(facet);

            var facetUrlProvider = EngineContext.Current.ResolveService<IFacetUrlHelperProvider>();
            var facetUrlHelper = facetUrlProvider.GetUrlHelper(facet.FacetGroup.Scope);

            return facetUrlHelper.GetQueryName(facet);
        }
    }
}
