using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.ComponentModel;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.Countries.Fields.")]
    public class CountryModel : TabbableModel, ILocalizedModel<CountryLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*AllowsBilling")]
        public bool AllowsBilling { get; set; } = true;

        [LocalizedDisplay("*AllowsShipping")]
        public bool AllowsShipping { get; set; } = true;

        [LocalizedDisplay("*TwoLetterIsoCode")]
        public string TwoLetterIsoCode { get; set; }

        [LocalizedDisplay("*ThreeLetterIsoCode")]
        public string ThreeLetterIsoCode { get; set; }

        [LocalizedDisplay("*NumericIsoCode")]
        public int NumericIsoCode { get; set; }

        [LocalizedDisplay("*SubjectToVat")]
        public bool SubjectToVat { get; set; }

        [LocalizedDisplay("*DisplayCookieManager")]
        public bool DisplayCookieManager { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; } = true;

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*NumberOfStates")]
        public int NumberOfStates { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 10)]
        [LocalizedDisplay("*AddressFormat")]
        public string AddressFormat { get; set; }

        [LocalizedDisplay("*DefaultCurrency")]
        public int? DefaultCurrencyId { get; set; }

        public List<CountryLocalizedModel> Locales { get; set; } = new();

        // Store mapping.
        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        public string EditUrl { get; set; }
    }

    public class CountryLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.Name")]
        public string Name { get; set; }
    }

    public partial class CountryValidator : AbstractValidator<CountryModel>
    {
        public CountryValidator()
        {
            RuleFor(x => x.Name).NotNull();
            RuleFor(x => x.TwoLetterIsoCode).NotEmpty().Length(2);
            RuleFor(x => x.ThreeLetterIsoCode).NotEmpty().Length(3);
        }
    }

    public class CountryMapper :
        IMapper<Country, CountryModel>
    {
        public Task MapAsync(Country from, CountryModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.NumberOfStates = from.StateProvinces?.Count ?? 0;
            return Task.CompletedTask;
        }
    }
}
