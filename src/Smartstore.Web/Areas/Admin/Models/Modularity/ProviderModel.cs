using Smartstore.Core.Checkout.Payment;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Admin.Models.Modularity
{
    public class ProviderModel : ModelBase, ILocalizedModel<ProviderLocalizedModel>
    {
        private List<ProviderLocalizedModel> _locales;

        public Type ProviderType { get; set; }

        [LocalizedDisplay("Common.SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("Common.FriendlyName")]
        public string FriendlyName { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public bool IsEditable { get; set; }

        public bool IsConfigurable { get; set; }

        public RouteInfo ConfigurationRoute { get; set; }

        public IModuleDescriptor ModuleDescriptor { get; set; }

        [LocalizedDisplay("Admin.Providers.ProvidingPlugin")]
        public string ProvidingModuleFriendlyName { get; set; }

        /// <summary>
        /// Returns the absolute path of the provider's icon url. 
        /// </summary>
        /// <remarks>
        /// The parent plugin's icon url is returned as a fallback if provider icon cannot be resolved.
        /// </remarks>
        public string IconUrl { get; set; }

        public List<ProviderLocalizedModel> Locales
        {
            get => _locales ??= new();
            set => _locales = value;
        }

        public bool IsPaymentMethod => ProviderType == typeof(IPaymentMethod);
    }

    public class ProviderLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Common.FriendlyName")]
        public string FriendlyName { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }
    }
}
