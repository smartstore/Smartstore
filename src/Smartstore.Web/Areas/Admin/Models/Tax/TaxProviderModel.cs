using Smartstore.Admin.Models.Modularity;

namespace Smartstore.Admin.Models.Tax
{
    public class TaxProviderModel : ProviderModel, IActivatable
    {
        [LocalizedDisplay("Admin.Configuration.Tax.Providers.Fields.IsPrimaryTaxProvider")]
        public bool IsPrimaryTaxProvider { get; set; }

        bool IActivatable.IsActive
        {
            get => IsPrimaryTaxProvider;
        }
    }
}
