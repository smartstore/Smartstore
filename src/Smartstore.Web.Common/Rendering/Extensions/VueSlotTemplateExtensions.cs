using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.Rendering
{
    public static class VueSlotTemplateExtensions
    {
        public static IHtmlContent LabeledProductName(
            this IHtmlHelper helper,
            string typeNameExpression = "item.row.ProductTypeName",
            string typeLabelHintExpression = "item.row.ProductTypeLabelHint",
            string urlExpression = "item.row.EditUrl",
            string linkTarget = null)
        {
            var label = "<span class='mr-1 badge' :class=\"'badge-' + {0}\">{{{{ {1} }}}}</span>".FormatInvariant(typeLabelHintExpression, typeNameExpression);

            var isLink = urlExpression.HasValue();
            var name = new TagBuilder(isLink ? "a" : "span");
            name.Attributes.Add("class", "text-truncate");
            name.InnerHtml.AppendHtml("{{ item.value }}");

            if (isLink)
            {
                name.Attributes.Add("v-bind:href", urlExpression);
                if (linkTarget.HasValue())
                {
                    name.Attributes.Add("target", linkTarget);
                }
            }

            if (typeNameExpression.IsEmpty() && typeLabelHintExpression.IsEmpty())
            {
                return name;
            }

            var builder = new HtmlContentBuilder();
            builder.AppendHtml(label);
            builder.AppendHtml(name);
            return builder;
        }
    }
}
