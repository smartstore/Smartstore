using System.Collections.Generic;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Identity
{
    public class ExternalAuthenticationSettings : ISettings
    {
        /// <summary>
        /// Gets or sets an system names of active payment methods
        /// </summary>
        public List<string> ActiveAuthenticationMethodSystemNames { get; set; } = new();
    }
}