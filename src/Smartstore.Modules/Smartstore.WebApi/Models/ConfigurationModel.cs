using Smartstore.Web.Modelling;

namespace Smartstore.Web.Api.Models
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

        [LocalizedDisplay("Admin.Customers.Customers.List.SearchTerm")]
        public string SearchTerm { get; set; }

        [LocalizedDisplay("Admin.Customers.Customers.List.SearchEmail")]
        public string SearchEmail { get; set; }

        [LocalizedDisplay("Admin.Customers.Customers.List.SearchUsername")]
        public string SearchUsername { get; set; }
        public bool UsernamesEnabled { get; set; }

        [LocalizedDisplay("Admin.Customers.Customers.List.SearchActiveOnly")]
        public bool? SearchActiveOnly { get; set; }
    }
}
