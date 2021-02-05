using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Allows request scoped registration of custom components, whose results get injected into widget zones.
    /// </summary>
    public interface IWidgetProvider
    {
        // TODO: (core) Implement IWidgetProvider.GetAllKnownWidgetZones()

        /// <summary>
        /// Registers a custom widget for widget zones
        /// </summary>
        /// <param name="zones">The names of the widget zones to inject the HTML content to</param>
        /// <param name="widget">Widget to register</param>
        void RegisterWidget(string[] zones, WidgetInvoker widget);

        /// <summary>
        /// Registers a custom widget for multiple widget zones by pattern
        /// </summary>
        /// <param name="zones">The widget zone pattern to inject the HTML content to</param>
        /// <param name="widget">Widget to register</param>
        void RegisterWidget(Regex zonePattern, WidgetInvoker widget);

        /// <summary>
        /// Enumerates all injected widgets for a given zone.
        /// </summary>
        /// <param name="zone">Zone name to retrieve widgets for.</param>
        /// <returns>List of <see cref="WidgetInvoker"/> instances.</returns>
        IEnumerable<WidgetInvoker> GetWidgets(string zone);

        /// <summary>
        /// Checks whether given <paramref name="zone"/> contains a widget
        /// with the same <see cref="WidgetInvoker.Key"/> as <paramref name="widgetKey"/>.
        /// </summary>
        /// <param name="zone">The zone name to check for existing widget.</param>
        /// <param name="widgetKey">The widget key to check.</param>
        bool ContainsWidget(string zone, string widgetKey);
    }

    public static class IWidgetProviderExtensions
    {
        // TODO: (core) Implement IWidgetProviderExtensions.RegisterViewComponent()

        /// <summary>
        /// Registers a custom widget for a single widget zone.
        /// </summary>
        /// <param name="zone">The name of the widget zone to inject the HTML content to</param>
        /// <param name="widget">Widget to register</param>
        public static void RegisterWidget(this IWidgetProvider provider, string zone, WidgetInvoker widget)
        {
            Guard.NotEmpty(zone, nameof(zone));
            provider.RegisterWidget(new[] { zone }, widget);
        }

        /// <summary>
        /// Registers custom HTML content for a single widget zone
        /// </summary>
        /// <param name="zone">The name of the widget zones to inject the HTML content to</param>
        /// <param name="html">HTML to inject</param>
        /// <param name="order">Sort order within the specified widget zone</param>
        public static void RegisterHtml(this IWidgetProvider provider, string zone, IHtmlContent html, int order = 0)
        {
            Guard.NotEmpty(zone, nameof(zone));
            provider.RegisterWidget(new[] { zone }, new HtmlWidgetInvoker(html) { Order = order });
        }

        /// <summary>
        /// Registers custom HTML content for widget zones
        /// </summary>
        /// <param name="zones">The names of the widget zones to inject the HTML content to</param>
        /// <param name="html">HTML to inject</param>
        /// <param name="order">Sort order within the specified widget zones</param>
        public static void RegisterHtml(this IWidgetProvider provider, string[] zones, IHtmlContent html, int order = 0)
        {
            provider.RegisterWidget(zones, new HtmlWidgetInvoker(html) { Order = order });
        }

        /// <summary>
        /// Registers custom HTML content for multiple widget zones by pattern
        /// </summary>
        /// <param name="zones">The widget zone pattern to inject the HTML content to</param>
        /// <param name="html">HTML to inject</param>
        /// <param name="order">Sort order within the specified widget zones</param>
        public static void RegisterHtml(this IWidgetProvider provider, Regex zonePattern, IHtmlContent html, int order = 0)
        {
            provider.RegisterWidget(zonePattern, new HtmlWidgetInvoker(html) { Order = order });
        }
    }
}