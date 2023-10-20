using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Shipping.")]
    public partial class ShippingSettingsModel : ModelBase
    {
        public string PrimaryStoreCurrencyCode { get; set; }

        [LocalizedDisplay("*FreeShippingOverXEnabled")]
        public bool FreeShippingOverXEnabled { get; set; }

        [LocalizedDisplay("*FreeShippingOverXValue")]
        public decimal FreeShippingOverXValue { get; set; }

        [LocalizedDisplay("*FreeShippingOverXIncludingTax")]
        public bool FreeShippingOverXIncludingTax { get; set; }

        [LocalizedDisplay("*EstimateShippingEnabled")]
        public bool EstimateShippingEnabled { get; set; }

        [LocalizedDisplay("*DisplayShipmentEventsToCustomers")]
        public bool DisplayShipmentEventsToCustomers { get; set; }

        [ValidateNever]
        [UIHint("Address")]
        [LocalizedDisplay("*ShippingOriginAddress")]
        public AddressModel ShippingOriginAddress { get; set; } = new();

        [LocalizedDisplay("*SkipShippingIfSingleOption")]
        public bool SkipShippingIfSingleOption { get; set; }

        [LocalizedDisplay("*ChargeOnlyHighestProductShippingSurcharge")]
        public bool ChargeOnlyHighestProductShippingSurcharge { get; set; }

        [LocalizedDisplay("*DeliveryOnWorkweekDaysOnly")]
        public bool DeliveryOnWorkweekDaysOnly { get; set; }

        [LocalizedDisplay("*TodayShipmentHour")]
        public int? TodayShipmentHour { get; set; }
    }

    public partial class ShippingSettingsValidator : SettingModelValidator<ShippingSettingsModel, ShippingSettings>
    {
        public ShippingSettingsValidator()
        {
            RuleFor(x => x.FreeShippingOverXValue).GreaterThanOrEqualTo(0);
        }
    }
}
