#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Widgets
{
    public static class IWidgetSelectorExtensions
    {
        /// <summary>
        /// Checks whether the given <paramref name="zone"/> contains at least one widget
        /// that produces non-whitespace content.
        /// </summary>
        /// <remarks>
        /// This method must actually INVOKE widgets in order to scan for content.
        /// It will break iteration on first found real content though.
        /// But to check for the mere existence of widgets in a zone it is better to call 
        /// <see cref="IWidgetSelector.GetWidgetsAsync(string, object)"/>.Any() instead.
        /// </remarks>
        /// <param name="zone">The zone name to check.</param>
        /// <param name="viewContext">The current view context.</param>
        public static async Task<bool> HasContentAsync(this IWidgetSelector selector, string zone, ViewContext viewContext)
        {
            Guard.NotNull(selector, nameof(selector));
            Guard.NotNull(viewContext, nameof(viewContext));

            var widgets = await selector.GetWidgetsAsync(zone);

            foreach (var widget in widgets)
            {
                try
                {
                    var htmlContent = await widget.InvokeAsync(viewContext);
                    if (htmlContent.HasContent())
                    {
                        return true;
                    }
                }
                catch
                {
                    // An exception indicates that most probably something went wrong
                    // with view rendering (wrong model type etc.). Although not really
                    // 100% bulletproof, the fact that we came so far should indicate that
                    // there is something to render.
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves all widgets for the given zone and invokes them.
        /// </summary>
        /// <param name="zone">Zone name to resolve widgets for</param>
        /// <param name="viewContext">The current view context</param>
        /// <param name="model">Optional view model</param>
        /// <returns>
        /// A <see cref="ZoneHtmlContent"/> instance containing the generated content.
        /// </returns>
        public static async Task<ZoneHtmlContent> GetContentAsync(this IWidgetSelector selector, string zone, ViewContext viewContext, object? model = null)
        {
            Guard.NotNull(selector);
            Guard.NotNull(viewContext);

            var result = new ZoneHtmlContent();
            var widgets = await selector.GetWidgetsAsync(zone, model ?? viewContext.ViewData.Model);

            if (widgets.Any())
            {
                var widgetContext = new WidgetContext(viewContext)
                {
                    Model = model,
                    Zone = zone,
                    // Create ViewData that is scoped to the current zone
                    ViewData = new ViewDataDictionary(viewContext.ViewData)
                    {
                        ["widgetzone"] = zone
                    }
                };

                foreach (var widget in widgets)
                {
                    var content = await widget.InvokeAsync(widgetContext);
                    var target = widget.Prepend ? result.PreContent : result.PostContent;
                    target.AppendHtml(content);
                }
            }

            return result;
        }
    }
}