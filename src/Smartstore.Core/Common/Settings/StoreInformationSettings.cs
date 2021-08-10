using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Settings
{
    public class StoreInformationSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether store is closed
        /// </summary>
        public bool StoreClosed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether administrators can visit a closed store
        /// </summary>
        public bool StoreClosedAllowForAdmins { get; set; } = true;
    }
}
