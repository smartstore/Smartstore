using System.Collections.Generic;
using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Directory
{
    public class StateProvinceModel : EntityModelBase, ILocalizedModel<StateProvinceLocalizedModel>
    {
        public int CountryId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.States.Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.States.Fields.Abbreviation")]
        public string Abbreviation { get; set; }

        [LocalizedDisplay("Admin.Configuration.Countries.States.Fields.Published")]
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
