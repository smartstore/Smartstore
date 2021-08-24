using System.Collections.Generic;
using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Directory
{
    [LocalizedDisplay("Admin.Configuration.Countries.States.Fields.")]
    public class StateProvinceModel : EntityModelBase, ILocalizedModel<StateProvinceLocalizedModel>
    {
        public int CountryId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Abbreviation")]
        public string Abbreviation { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public List<StateProvinceLocalizedModel> Locales { get; set; } = new();
    }

    public class StateProvinceLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.States.Fields.Name")]
        public string Name { get; set; }
    }

    public partial class StateProvinceValidator : AbstractValidator<StateProvinceModel>
    {
        public StateProvinceValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
