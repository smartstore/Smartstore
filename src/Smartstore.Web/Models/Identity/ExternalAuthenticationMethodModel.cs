using Smartstore.Core.Widgets;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Identity
{
    //TODO: (mh) (core) Remove component model once auth modules are available, which can render directly into the zone via PublicInfo methods or something similiar.
    public partial class ExternalAuthenticationMethodModel : ModelBase
    {
        /// <summary>
        /// WidgetInvoker to invoke button rendering action from external authentication module.
        /// </summary>
        public WidgetInvoker Button { get; set; }

        /// <summary>
        /// Provider name as returned by external authentication methods.
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Display name as returned by external authentication methods.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
