using Smartstore.Core.Configuration;

namespace Smartstore.Clickatell.Settings
{
    public class ClickatellSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the value indicting whether this SMS provider is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the Clickatell phone number.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the Clickatell API ID.
        /// </summary>
        public string ApiId { get; set; }
    }
}