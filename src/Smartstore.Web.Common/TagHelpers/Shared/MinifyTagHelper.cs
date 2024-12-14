using DouglasCrockford.JsMin;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Caching;
using Smartstore.Utilities;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Minifies an inline script in debug mode, but only if the corresponding theming option is activated.
    /// Inline script minification is an opt-in feature and is disabled by default, 
    /// because inline scripts may contain dynamic content.
    /// </summary>
    [HtmlTargetElement("script", Attributes = MinifyAttributeName)]
    public class MinifyTagHelper : TagHelper
    {
        const string MinifyAttributeName = "sm-minify";

        private readonly ICacheFactory _cacheFactory;

        public MinifyTagHelper(ICacheFactory cacheFactory)
        {
            _cacheFactory = cacheFactory;
        }

        /// <summary>
        /// Minifies the inline script in debug mode, but only if the corresponding theming option is activated. Default = false.
        /// </summary>
        [HtmlAttributeName(MinifyAttributeName)]
        public virtual bool Minify { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!Minify /*|| CommonHelper.IsDevEnvironment*/)
            {
                // Proceed only if enabled or in release mode
                return;
            }

            if (output.Attributes.ContainsName("src"))
            {
                // Proceed only if the script is inline
                return;
            }

            if (output.Attributes.TryGetAttribute("type", out var typeAttribute) && typeAttribute.ValueAsString() != "text/javascript")
            {
                // Proceed only if the script is text/javascript
                return;
            }

            // Get the content of the script tag
            var childContent = await output.GetChildContentAsync();
            var originalContent = childContent.GetContent();

            // Generate a cache key using XxHash
            var contentHash = originalContent.XxHash64();
            var cacheKey = $"InlineScript:{contentHash}";

            var minifiedResult = _cacheFactory.GetMemoryCache().Get(cacheKey, o => {
                o.SetSlidingExpiration(TimeSpan.FromHours(1));

                try
                {
                    // Return the minified the JavaScript code
                    return new JsMinifier().Minify(originalContent);
                }
                catch
                {
                    return null;
                }
            }, independent: true);

            if (minifiedResult is null)
            {
                // Set the original content on failure
                output.Content.SetHtmlContent(originalContent);
            }
            else
            {
                // Set the minified content
                output.Content.SetHtmlContent(minifiedResult);
            }
        }
    }
}
