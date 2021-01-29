using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.UI
{
    public static class HtmlHelperUIExtensions
    {
        #region Hint

        /// <summary>
        /// Generates a question mark icon that pops a description tooltip on hover
        /// </summary>
        public static IHtmlContent HintTooltipFor<TModel, TResult>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpression = helper.ModelExpressionFor(expression);
            var hintText = modelExpression?.Metadata?.Description;

            return HintTooltip(helper, hintText);
        }

        /// <summary>
        /// Generates a question mark icon that pops a description tooltip on hover
        /// </summary>
        public static IHtmlContent HintTooltip(this IHtmlHelper _, string hintText)
        {
            if (string.IsNullOrEmpty(hintText))
            {
                return HtmlString.Empty;
            }

            // create a
            var a = new TagBuilder("a");
            a.Attributes.Add("href", "#");
            a.Attributes.Add("onclick", "return false;");
            //a.Attributes.Add("rel", "tooltip");
            a.Attributes.Add("title", hintText);
            a.Attributes.Add("tabindex", "-1");
            a.Attributes.Add("class", "hint");

            // Create symbol
            var img = new TagBuilder("i");
            img.Attributes.Add("class", "fa fa-question-circle");

            a.InnerHtml.SetHtmlContent(img);

            // Return <a> tag
            return a;
        }

        /// <summary>
        /// Generates control description text.
        /// </summary>
        public static IHtmlContent HintFor<TModel, TResult>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpression = helper.ModelExpressionFor(expression);
            var hintText = modelExpression?.Metadata?.Description;

            return Hint(helper, hintText);
        }

        /// <summary>
        /// Generates control description text.
        /// </summary>
        public static IHtmlContent Hint(this IHtmlHelper _, string hintText)
        {
            if (string.IsNullOrEmpty(hintText))
            {
                return HtmlString.Empty;
            }

            // create <small>
            var small = new TagBuilder("small");
            small.Attributes.Add("class", "form-text text-muted");

            small.InnerHtml.SetContent(hintText);

            return small;
        }

        #endregion
    }
}
