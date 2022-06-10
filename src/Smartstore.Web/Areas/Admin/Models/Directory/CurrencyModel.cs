using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.Currencies.Fields.")]
    public class CurrencyModel : EntityModelBase, ILocalizedModel<CurrencyLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*CurrencyCode")]
        public string CurrencyCode { get; set; }

        [LocalizedDisplay("*DisplayLocale")]
        public string DisplayLocale { get; set; }

        [LocalizedDisplay("*Rate")]
        public decimal Rate { get; set; } = 1;

        [LocalizedDisplay("*CustomFormatting")]
        public string CustomFormatting { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; } = true;

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*DomainEndings")]
        public string DomainEndings { get; set; }
        public string[] DomainEndingsArray { get; set; }
        public List<SelectListItem> AvailableDomainEndings { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Text = ".com", Value = ".com" },
            new SelectListItem { Text = ".uk", Value = ".uk" },
            new SelectListItem { Text = ".de", Value = ".de" },
            new SelectListItem { Text = ".ch", Value = ".ch" }
        };

        public List<CurrencyLocalizedModel> Locales { get; set; } = new();

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        [LocalizedDisplay("*IsPrimaryStoreCurrency")]
        public bool IsPrimaryCurrency { get; set; }

        [LocalizedDisplay("*IsPrimaryExchangeRateCurrency")]
        public bool IsPrimaryExchangeCurrency { get; set; }

        public string EditUrl { get; set; }

        #region Rounding

        [LocalizedDisplay("*RoundOrderItemsEnabled")]
        public bool RoundOrderItemsEnabled { get; set; }

        [LocalizedDisplay("*RoundNumDecimals")]
        public int RoundNumDecimals { get; set; } = 2;

        [LocalizedDisplay("*RoundOrderTotalEnabled")]
        public bool RoundOrderTotalEnabled { get; set; }

        [LocalizedDisplay("*RoundOrderTotalDenominator")]
        public decimal RoundOrderTotalDenominator { get; set; }

        [LocalizedDisplay("*RoundOrderTotalRule")]
        public CurrencyRoundingRule RoundOrderTotalRule { get; set; }

        public Dictionary<string, string> RoundOrderTotalPaymentMethods { get; set; } = new();

        #endregion Rounding
    }

    public class CurrencyLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Currencies.Fields.Name")]
        public string Name { get; set; }
    }

    public partial class CurrencyValidator : AbstractValidator<CurrencyModel>
    {
        public CurrencyValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty().Length(1, 50);
            RuleFor(x => x.CurrencyCode).NotEmpty().Length(1, 5);
            RuleFor(x => x.Rate).GreaterThan(0);
            RuleFor(x => x.CustomFormatting).Length(0, 50);
            RuleFor(x => x.DisplayLocale)
                .Must(x =>
                {
                    try
                    {
                        if (!x.HasValue())
                        {
                            return true;
                        }

                        var culture = new CultureInfo(x);
                        return culture != null;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage(T("Admin.Configuration.Currencies.Fields.DisplayLocale.Validation"));

            RuleFor(x => x.RoundNumDecimals)
                .InclusiveBetween(0, 8)
                .When(x => x.RoundOrderItemsEnabled)
                .WithMessage(T("Admin.Configuration.Currencies.Fields.RoundOrderItemsEnabled.Validation"));
        }
    }

    [LocalizedDisplay("Admin.Configuration.Currencies.Fields.")]
    public partial class CurrencyListModel
    {
        public bool DisplayLiveRates { get; set; }

        [LocalizedDisplay("*CurrencyRateAutoUpdateEnabled")]
        public bool AutoUpdateEnabled { get; set; }

        [LocalizedDisplay("*ExchangeRateProvider")]
        public string ExchangeRateProvider { get; set; }
    }
}