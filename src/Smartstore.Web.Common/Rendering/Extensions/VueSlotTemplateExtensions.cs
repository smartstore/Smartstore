using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Rendering
{
    public static class VueSlotTemplateExtensions
    {
        // TODO: (mg) (core) Port other helpers: LabeledCurrencyName

        public static IHtmlContent LabeledProductName(
            this IHtmlHelper _,
            string typeNameExpression = "item.row.ProductTypeName",
            string typeLabelHintExpression = "item.row.ProductTypeLabelHint",
            string urlExpression = "item.row.EditUrl",
            string linkTarget = null)
        {
            var builder = new HtmlContentBuilder();

            if (typeNameExpression.HasValue() && typeLabelHintExpression.HasValue())
            {
                var label = "<span class='mr-1 badge' :class=\"'badge-' + {0}\">{{{{ {1} }}}}</span>".FormatInvariant(typeLabelHintExpression, typeNameExpression);
                builder.AppendHtml(label);
            }

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

            builder.AppendHtml(name);

            return builder;
        }

        public static IHtmlContent LabeledOrderNumber(
            this IHtmlHelper helper,
            string hasNewPaymentNotificationExpression = "item.row.HasNewPaymentNotification",
            string urlExpression = "item.row.EditUrl",
            string linkTarget = null)
        {
            var builder = new HtmlContentBuilder();

            if (hasNewPaymentNotificationExpression.HasValue())
            {
                var localizationService = helper.ViewContext.HttpContext.RequestServices.GetService<ILocalizationService>();

                var label = new TagBuilder("span");
                label.Attributes.Add("v-if", hasNewPaymentNotificationExpression);
                label.Attributes.Add("class", "badge badge-warning mr-1");
                label.Attributes.Add("title", localizationService.GetResource("Admin.Orders.Payments.NewIpn.Hint"));
                label.InnerHtml.Append(localizationService.GetResource("Admin.Orders.Payments.NewIpn"));

                builder.AppendHtml(label);
            }

            var isLink = urlExpression.HasValue();
            var name = new TagBuilder(isLink ? "a" : "span");
            name.InnerHtml.AppendHtml("{{ item.value }}");

            if (isLink)
            {
                name.Attributes.Add("v-bind:href", urlExpression);
                if (linkTarget.HasValue())
                {
                    name.Attributes.Add("target", linkTarget);
                }
            }

            builder.AppendHtml(name);

            return builder;
        }
    }
}
