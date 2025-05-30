using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Admin;
using System.Collections.Generic; // Required for List<>
using Smartstore.Core.Localization; // Required for LocalizedDisplay


namespace Smartstore.Klarna.Models
{
    public class KlarnaConfigurationModel : ModelBase, ILocalizedModel<KlarnaConfigurationLocalizedModel>
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [LocalizedDisplay("Plugins.Payment.Klarna.ApiKey")]
        public string ApiKey { get; set; }
        public bool ApiKey_OverrideForStore { get; set; }

        [LocalizedDisplay("Plugins.Payment.Klarna.ApiSecret")]
        public string ApiSecret { get; set; }
        public bool ApiSecret_OverrideForStore { get; set; }

        [LocalizedDisplay("Plugins.Payment.Klarna.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [LocalizedDisplay("Plugins.Payment.Klarna.Region")]
        public string Region { get; set; } // e.g., "EU", "NA", "OC"
        public bool Region_OverrideForStore { get; set; }

        public List<KlarnaConfigurationLocalizedModel> Locales { get; set; } = new();
    }

    public class KlarnaConfigurationLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }
        // Add localized properties if any are needed in the future
    }
}
