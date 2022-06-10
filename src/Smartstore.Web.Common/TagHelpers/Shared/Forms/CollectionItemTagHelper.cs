using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Utilities;

namespace Smartstore.Web.TagHelpers.Shared.Forms
{
    [HtmlTargetElement(CollectionItemTagName, Attributes = NameAttributeName)]
    public class CollectionItemTagHelper : SmartTagHelper
    {
        const string IdsToReuseKey = "__htmlPrefixScopeExtensions_IdsToReuse_";
        const string CollectionItemTagName = "collection-item";
        const string NameAttributeName = "name";

        /// <summary>
        /// The new prefix to use in ViewData.TemplateInfo.<see cref="TemplateInfo.HtmlFieldPrefix"/> for this scope.
        /// </summary>
        [HtmlAttributeName(NameAttributeName)]
        public string CollectionName { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            var idsToReuse = GetIdsToReuse(ViewContext.HttpContext, CollectionName);
            var itemIndex = idsToReuse.Count > 0 ? idsToReuse.Dequeue() : CommonHelper.GenerateRandomDigitCode(10);
            var originalPrefix = ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix;
            var htmlFieldPrefixForScope = $"{CollectionName}[{itemIndex}]";

            ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = htmlFieldPrefixForScope;

            output.SuppressOutput();
            var content = await output.GetChildContentAsync();

            // autocomplete="off" is needed to work around a very annoying Chrome behaviour
            // whereby it reuses old values after the user clicks "Back", which causes the
            // xyz.index and xyz[...] values to get out of sync.
            var input = new TagBuilder("input");
            input.Attributes["type"] = "hidden";
            input.Attributes["autocomplete"] = "off";
            input.Attributes["name"] = $"{CollectionName}.index";
            input.Attributes["value"] = HtmlHelper.Encode(itemIndex);
            output.PreContent.SetHtmlContent(input);

            output.Content.SetHtmlContent(content);

            ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = originalPrefix;
        }

        protected override string GenerateTagId(TagHelperContext context) => null;

        private static Queue<string> GetIdsToReuse(HttpContext httpContext, string collectionName)
        {
            // We need to use the same sequence of IDs following a server-side validation failure,
            // otherwise the framework won't render the validation error messages next to each item.
            var key = IdsToReuseKey + collectionName;
            var queue = (Queue<string>)httpContext.Items[key];
            if (queue == null)
            {
                httpContext.Items[key] = queue = new Queue<string>();
                string previouslyUsedIds = null;

                if (httpContext.Request.HasFormContentType)
                {
                    previouslyUsedIds = httpContext.Request.Form[collectionName + ".index"];
                }

                previouslyUsedIds ??= httpContext.Request.Query[collectionName + ".index"];

                if (!string.IsNullOrEmpty(previouslyUsedIds))
                {
                    foreach (var previouslyUsedId in previouslyUsedIds.Split(','))
                    {
                        queue.Enqueue(previouslyUsedId);
                    }
                }
            }
            return queue;
        }
    }
}
