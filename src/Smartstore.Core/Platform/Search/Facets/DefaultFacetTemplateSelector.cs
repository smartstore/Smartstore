using Smartstore.Core.Widgets;

namespace Smartstore.Core.Search.Facets
{
    public class DefaultFacetTemplateSelector : IFacetTemplateSelector
    {
        public int Ordinal => -100;

        public Widget GetTemplateWidget(FacetGroup facetGroup)
        {
            var templateName = GetTemplateName(facetGroup);
            if (templateName.IsEmpty())
            {
                return null;
            }

            var widget = new ComponentWidget("FacetGroup", new { facetGroup, templateName })
            {
                Order = facetGroup.DisplayOrder
            };

            return widget;
        }

        private static string GetTemplateName(FacetGroup group)
        {
            switch (group?.Kind)
            {
                case FacetGroupKind.Category:
                case FacetGroupKind.DeliveryTime:
                case FacetGroupKind.Brand:
                case FacetGroupKind.Availability:
                case FacetGroupKind.NewArrivals:
                case FacetGroupKind.Forum:
                case FacetGroupKind.Customer:
                case FacetGroupKind.Date:
                    return group.IsMultiSelect ? "MultiSelect" : "SingleSelect";
                case FacetGroupKind.Price:
                    return "Price";
                case FacetGroupKind.Rating:
                    return "Rating";
                case FacetGroupKind.Unknown:
                case FacetGroupKind.Attribute:
                case FacetGroupKind.Variant:
                default:
                    return null;
            }
        }
    }
}
