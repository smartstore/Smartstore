using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.UI
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlContent Hint(this IHtmlHelper _, string hintText)
        {
            if (string.IsNullOrEmpty(hintText))
            {
                return HtmlString.Empty;
            }

            // create a
            var a = new TagBuilder("a");
            a.MergeAttribute("href", "#");
            a.MergeAttribute("onclick", "return false;");
            //a.MergeAttribute("rel", "tooltip");
            a.MergeAttribute("title", hintText);
            a.MergeAttribute("tabindex", "-1");
            a.AddCssClass("hint");

            // Create symbol
            var img = new TagBuilder("i");
            img.AddCssClass("fa fa-question-circle");

            a.InnerHtml.SetHtmlContent(img);

            // Return <a> tag
            return a;
        }
    }
}
