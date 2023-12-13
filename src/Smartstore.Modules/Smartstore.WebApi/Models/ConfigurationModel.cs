using FluentValidation;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Web.Api.Models
{
    [LocalizedDisplay("Plugins.Api.WebApi.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*ApiOdataUrl")]
        public string ApiOdataUrl { get; set; }

        [LocalizedDisplay("*ApiOdataEndpointsUrl")]
        public string ApiOdataEndpointsUrl { get; set; }

        [LocalizedDisplay("*ApiOdataMetadataUrl")]
        public string ApiOdataMetadataUrl { get; set; }

        [LocalizedDisplay("*SwaggerUrl")]
        public string ApiDocsUrl { get; set; }

        [LocalizedDisplay("*IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*MaxTop")]
        public int MaxTop { get; set; }

        [LocalizedDisplay("*MaxExpansionDepth")]
        public int MaxExpansionDepth { get; set; }

        #region Batch

        [LocalizedDisplay("*MaxBatchNestingDepth")]
        public int MaxBatchNestingDepth { get; set; }

        [LocalizedDisplay("*MaxBatchOperationsPerChangeset")]
        public int MaxBatchOperationsPerChangeset { get; set; }

        [LocalizedDisplay("*MaxBatchReceivedMessageSize")]
        public long MaxBatchReceivedMessageSize { get; set; }

        #endregion

        public CustomerSearchModel CustomerSearch { get; set; } = new();
    }

    [LocalizedDisplay("Plugins.Api.WebApi.")]
    public class ApiKeysModel : ModelBase
    {
        [LocalizedDisplay("Common.Entity.Customer")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("*PublicKey")]
        public string PublicKey { get; set; }

        [LocalizedDisplay("*SecretKey")]
        public string SecretKey { get; set; }

        public bool Enabled { get; set; }
    }


    public partial class WebApiConfigurationValidator : SettingModelValidator<ConfigurationModel, WebApiSettings>
    {
        public WebApiConfigurationValidator()
        {
            RuleFor(x => x.MaxTop).GreaterThan(0);
            RuleFor(x => x.MaxExpansionDepth).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxBatchNestingDepth).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxBatchOperationsPerChangeset).GreaterThan(0);
            RuleFor(x => x.MaxBatchReceivedMessageSize).GreaterThan(0);
        }
    }
}
