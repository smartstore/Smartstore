using Smartstore.Web.Modelling;

namespace Smartstore.WebApi.Models
{
    [LocalizedDisplay("Plugins.Api.WebApi.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*ApiOdataUrl")]
        public string ApiOdataUrl { get; set; }

        [LocalizedDisplay("*ApiOdataMetadataUrl")]
        public string ApiOdataMetadataUrl { get; set; }

        [LocalizedDisplay("*SwaggerUrl")]
        public string SwaggerUrl { get; set; }

        [LocalizedDisplay("*IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*MaxTop")]
        public int MaxTop { get; set; }

        [LocalizedDisplay("*MaxExpansionDepth")]
        public int MaxExpansionDepth { get; set; }
    }
}
