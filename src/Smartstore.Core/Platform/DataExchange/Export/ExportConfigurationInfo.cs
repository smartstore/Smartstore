using Smartstore.Core.Widgets;

namespace Smartstore.Core.DataExchange.Export
{
    /// <summary>
    /// Serves information about export provider specific configuration.
    /// </summary>
    public class ExportConfigurationInfo
    {
        ///// <summary>
        ///// The partial view name for the configuration.
        ///// </summary>
        //public string PartialViewName { get; set; }

        /// <summary>
        /// Gets or sets the widget for the export provider specific configuration.
        /// </summary>
        public Widget ConfigurationWidget { get; set; }

        /// <summary>
        /// Type of the view model.
        /// </summary>
        public Type ModelType { get; set; }
    }
}
