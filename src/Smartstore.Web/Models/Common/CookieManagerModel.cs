using System.Collections.Generic;
using Smartstore.Core.Identity;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Common
{
    public partial class CookieManagerModel : ModelBase
    {
        public List<CookieInfo> CookiesInfos { get; set; } = new();
        public bool ModalCookieConsent { get; set; }

        public bool AcceptAll { get; set; }

        public bool RequiredConsent { get; set; } = true;

        public bool AnalyticsConsent { get; set; }

        public bool ThirdPartyConsent { get; set; }
    }
}
