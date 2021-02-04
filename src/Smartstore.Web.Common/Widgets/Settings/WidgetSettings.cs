using System.Collections.Generic;
using Smartstore.Core.Configuration;

namespace Smartstore.Web.Widgets
{
    public class WidgetSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a system names of active widgets.
        /// </summary>
        public List<string> ActiveWidgetSystemNames { get; set; } = new();
    }
}
