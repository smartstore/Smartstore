using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Web.Models.Common;

namespace Smartstore.Admin.Models
{
    public partial class FinanceSettingsModel : ModelBase
    {
        public TaxSettingsModel TaxSettings { get; set; } = new();
        public CurrencySettingsModel CurrencySettings { get; set; } = new();

        [LocalizedDisplay("Admin.Configuration.Settings.Tax.")]
        public partial class TaxSettingsModel
        {
            [LocalizedDisplay("*PricesIncludeTax")]
            public bool PricesIncludeTax { get; set; }

            [LocalizedDisplay("*AllowCustomersToSelectTaxDisplayType")]
            public bool AllowCustomersToSelectTaxDisplayType { get; set; }

            [LocalizedDisplay("*TaxDisplayType")]
            public TaxDisplayType TaxDisplayType { get; set; }

            [LocalizedDisplay("*DisplayTaxSuffix")]
            public bool DisplayTaxSuffix { get; set; }

            [LocalizedDisplay("*DisplayTaxRates")]
            public bool DisplayTaxRates { get; set; }

            [LocalizedDisplay("*HideZeroTax")]
            public bool HideZeroTax { get; set; }

            [LocalizedDisplay("*HideTaxInOrderSummary")]
            public bool HideTaxInOrderSummary { get; set; }

            [LocalizedDisplay("*ShowLegalHintsInProductList")]
            public bool ShowLegalHintsInProductList { get; set; }

            [LocalizedDisplay("*ShowLegalHintsInProductDetails")]
            public bool ShowLegalHintsInProductDetails { get; set; }

            [LocalizedDisplay("*ShowLegalHintsInFooter")]
            public bool ShowLegalHintsInFooter { get; set; }

            [LocalizedDisplay("*TaxBasedOn")]
            public TaxBasedOn TaxBasedOn { get; set; }

            [ValidateNever]
            [UIHint("Address")]
            [LocalizedDisplay("*DefaultTaxAddress")]
            public AddressModel DefaultTaxAddress { get; set; } = new();

            [LocalizedDisplay("*ShippingIsTaxable")]
            public bool ShippingIsTaxable { get; set; }

            [LocalizedDisplay("*ShippingPriceIncludesTax")]
            public bool ShippingPriceIncludesTax { get; set; }

            [LocalizedDisplay("*ShippingTaxClass")]
            public int ShippingTaxClassId { get; set; }

            [LocalizedDisplay("*PaymentMethodAdditionalFeeIsTaxable")]
            public bool PaymentMethodAdditionalFeeIsTaxable { get; set; }

            [LocalizedDisplay("*PaymentMethodAdditionalFeeIncludesTax")]
            public bool PaymentMethodAdditionalFeeIncludesTax { get; set; }

            [LocalizedDisplay("*PaymentMethodAdditionalFeeTaxClass")]
            public int PaymentMethodAdditionalFeeTaxClassId { get; set; }

            [LocalizedDisplay("*AuxiliaryServicesTaxingType")]
            public AuxiliaryServicesTaxType AuxiliaryServicesTaxingType { get; set; }

            [LocalizedDisplay("*EuVatEnabled")]
            public bool EuVatEnabled { get; set; }

            [LocalizedDisplay("*EuVatShopCountry")]
            public int EuVatShopCountryId { get; set; }

            [LocalizedDisplay("*EuVatAllowVatExemption")]
            public bool EuVatAllowVatExemption { get; set; }

            [LocalizedDisplay("*EuVatUseWebService")]
            public bool EuVatUseWebService { get; set; }

            [LocalizedDisplay("*EuVatEmailAdminWhenNewVatSubmitted")]
            public bool EuVatEmailAdminWhenNewVatSubmitted { get; set; }

            [LocalizedDisplay("*VatRequired")]
            public bool VatRequired { get; set; } = false;
        }

        [LocalizedDisplay("Admin.Configuration.Settings.Currency.")]
        public partial class CurrencySettingsModel
        {
            [LocalizedDisplay("*ExchangeRateProvider")]
            public string ActiveExchangeRateProviderSystemName { get; set; }

            [LocalizedDisplay("*AutoUpdateEnabled")]
            public bool AutoUpdateEnabled { get; set; }

            #region Rounding

            [LocalizedDisplay("*RoundOrderItemsEnabled")]
            public bool RoundOrderItemsEnabled { get; set; }

            [LocalizedDisplay("Admin.Configuration.Currencies.Fields.RoundNetPrices")]
            public bool RoundNetPrices { get; set; }

            [LocalizedDisplay("Admin.Configuration.Currencies.Fields.RoundUnitPrices")]
            public bool RoundUnitPrices { get; set; }

            #endregion
        }
    }
}
