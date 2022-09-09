using Smartstore.Web.Modelling;

namespace Smartstore.WebApi.Models
{
    [LocalizedDisplay("Plugins.Api.WebApi.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*MaxTop")]
        public int MaxTop { get; set; }

        [LocalizedDisplay("*MaxExpansionDepth")]
        public int MaxExpansionDepth { get; set; }

        [LocalizedDisplay("*LogUnauthorized")]
        public bool LogUnauthorized { get; set; }
    }
}
