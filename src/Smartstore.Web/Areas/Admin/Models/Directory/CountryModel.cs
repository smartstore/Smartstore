using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Directory
{
    public class CountryModel : TabbableModel, ILocalizedModel<CountryLocalizedModel>
    {
        [LocalizedDisplay("Admin.Configuration.Countries.Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.AllowsBilling")]
        public bool AllowsBilling { get; set; } = true;

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.AllowsShipping")]
        public bool AllowsShipping { get; set; } = true;

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.TwoLetterIsoCode")]
        public string TwoLetterIsoCode { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.ThreeLetterIsoCode")]
        public string ThreeLetterIsoCode { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.NumericIsoCode")]
        public int NumericIsoCode { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.SubjectToVat")]
        public bool SubjectToVat { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.DisplayCookieManager")]
        public bool DisplayCookieManager { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.Published")]
        public bool Published { get; set; } = true;

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.NumberOfStates")]
        public int NumberOfStates { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 10)]
        [LocalizedDisplay("Admin.Configuration.Countries.Fields.AddressFormat")]
        public string AddressFormat { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.Fields.DefaultCurrency")]
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
            RuleFor(x => x.TwoLetterIsoCode).NotEmpty();
            RuleFor(x => x.TwoLetterIsoCode).Length(2);
            RuleFor(x => x.ThreeLetterIsoCode).NotEmpty();
            RuleFor(x => x.ThreeLetterIsoCode).Length(3);
        }
    }

    // TODO: (mh) (core) Find out what to do here
    //public class CountryMapper :
    //    IMapper<Country, CountryModel>
    //{
    //    public void Map(Country from, CountryModel to)
    //    {
    //        MiniMapper.Map(from, to);
    //        to.NumberOfStates = from.StateProvinces?.Count ?? 0;
    //    }
    //}
}
