using Smartstore.Admin.Models.Modularity;

namespace Smartstore.Admin.Models.Tax
{
    public class TaxProviderModel : ProviderModel
    {
        [LocalizedDisplay("Admin.Configuration.Tax.Providers.Fields.IsPrimaryTaxProvider")]
        public bool IsPrimaryTaxProvider { get; set; }
    }
}
