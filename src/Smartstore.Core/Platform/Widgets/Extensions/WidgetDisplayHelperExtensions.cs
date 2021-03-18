using System;
using Smartstore.Core.Widgets;

namespace Smartstore
{
    public static class WidgetDisplayHelperExtensions
    {
        /// <summary>
        /// Checks whether a given zone has content (contains at least one widget),
        /// </summary>
        /// <param name="zone">The zone name to check.</param>
        public static bool ZoneHasContent(this IDisplayHelper displayHelper, string zone)
        {
            return displayHelper.Resolve<IWidgetProvider>().HasContent(zone);
        }
    }
}