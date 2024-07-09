#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Widgets
{
    public static class IWidgetSelectorExtensions
    {
        /// <summary>
        /// Resolves all widgets for the given zone, sorted by <see cref="Widget.Prepend"/>, 
        /// then by <see cref="Widget.Order"/>.
        /// </summary>
        /// <param name="zone">Zone to enumerate widgets for.</param>
        /// <param name="model">Optional view model.</param>
        /// <returns>A list of <see cref="Widget"/> instances that should be injected into the zone.</returns>
        public static async Task<IEnumerable<Widget>> GetWidgetsAsync(this IWidgetSelector selector, IWidgetZone zone, object? model = null)
        {
            Guard.NotNull(selector);
            Guard.NotNull(zone);

            var widgets = await selector.EnumerateWidgetsAsync(zone, model)
                .Distinct()
                .OrderBy(x => x.Prepend)
                .ThenBy(x => x.Order)
                .ToListAsync();

            return widgets;
        }

        /// <summary>
        /// Checks whether the given <paramref name="zone"/> contains at least one widget
        /// that produces non-whitespace content.
        /// </summary>
        /// <remarks>
        /// This method must actually INVOKE widgets in order to scan for content.
        /// It will break iteration on first found real content though.
        /// But to check for the mere existence of widgets in a zone it is better to call 
        /// <see cref="IWidgetSelector.EnumerateWidgetsAsync(IWidgetZone, object)"/>.AnyAsync() instead.
        /// </remarks>
        /// <param name="zone">The zone to check.</param>
        /// <param name="viewContext">The current view context.</param>
        public static async Task<bool> HasContentAsync(this IWidgetSelector selector, IWidgetZone zone, ViewContext viewContext)
        {
            Guard.NotNull(selector);
            Guard.NotNull(zone);
            Guard.NotNull(viewContext);

            var widgets = selector.EnumerateWidgetsAsync(zone);

            await foreach (var widget in widgets)
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
        /// <param name="zone">Zone to resolve widgets for</param>
        /// <param name="viewContext">The current view context</param>
        /// <param name="model">Optional view model</param>
        /// <returns>
        /// A <see cref="ZoneHtmlContent"/> instance containing the generated content.
        /// </returns>
        public static async Task<ZoneHtmlContent> GetContentAsync(this IWidgetSelector selector, IWidgetZone zone, ViewContext viewContext, object? model = null)
        {
            Guard.NotNull(selector);
            Guard.NotNull(zone);
            Guard.NotNull(viewContext);

            var result = new ZoneHtmlContent();
            var widgets = await selector.GetWidgetsAsync(zone, model);

            if ((widgets.TryGetNonEnumeratedCount(out var count) && count > 0) || widgets.Any())
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

        /// <inheritdoc cref="IWidgetSelector.EnumerateWidgetsAsync(IWidgetZone, object)" />
        /// <param name="zoneName">Zone name to enumerate widgets for.</param>
        /// <param name="model">Optional view model.</param>
        public static IAsyncEnumerable<Widget> EnumerateWidgetsAsync(this IWidgetSelector selector, string zoneName, object? model = null)
            => selector.EnumerateWidgetsAsync(new PlainWidgetZone(zoneName), model);

        /// <inheritdoc cref="GetWidgetsAsync(IWidgetSelector, IWidgetZone, object)" />
        /// <param name="zoneName">Zone name to resolve widgets for.</param>
        /// <param name="model">Optional view model.</param>
        public static Task<IEnumerable<Widget>> GetWidgetsAsync(this IWidgetSelector selector, string zoneName, object? model = null)
            => selector.GetWidgetsAsync(new PlainWidgetZone(zoneName), model);

        /// <inheritdoc cref="HasContentAsync(IWidgetSelector, IWidgetZone, ViewContext)" />
        /// <param name="zoneName">The zone name to check.</param>
        public static Task<bool> HasContentAsync(this IWidgetSelector selector, string zoneName, ViewContext viewContext)
            => HasContentAsync(selector, new PlainWidgetZone(zoneName), viewContext);

        /// <inheritdoc cref="GetContentAsync(IWidgetSelector, IWidgetZone, ViewContext, object)" />
        /// <param name="zoneName">The zone name to check.</param>
        /// <param name="model">Optional view model.</param>
        public static Task<ZoneHtmlContent> GetContentAsync(this IWidgetSelector selector, string zoneName, ViewContext viewContext, object? model = null)
            => GetContentAsync(selector, new PlainWidgetZone(zoneName), viewContext, model);
    }
}