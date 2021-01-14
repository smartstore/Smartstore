using System.Collections.Generic;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Customers
{
    public class ExternalAuthenticationSettings : ISettings
    {
        public bool AutoRegisterEnabled { get; set; }

        /// <summary>
        /// Gets or sets an system names of active payment methods
        /// </summary>
        public List<string> ActiveAuthenticationMethodSystemNames { get; set; } = new();
    }
}