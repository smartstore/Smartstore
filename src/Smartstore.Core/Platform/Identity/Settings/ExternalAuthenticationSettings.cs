using System.Collections.Generic;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Identity
{
    public class ExternalAuthenticationSettings : ISettings
    {
        // INFO: (mh) (core) This seting is obsolete. Don't add when porting Admin settings.
        //public bool AutoRegisterEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets an system names of active payment methods
        /// </summary>
        public List<string> ActiveAuthenticationMethodSystemNames { get; set; } = new();
    }
}