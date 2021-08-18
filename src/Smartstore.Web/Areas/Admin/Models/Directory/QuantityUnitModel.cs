using System.Collections.Generic;
using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Directory
{
    public class QuantityUnitModel : EntityModelBase, ILocalizedModel<QuantityUnitLocalizedModel>
    {
        [LocalizedDisplay("Admin.Configuration.QuantityUnit.Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Configuration.QuantityUnit.Fields.NamePlural")]
        public string NamePlural { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Configuration.QuantityUnit.Fields.IsDefault")]
        public bool IsDefault { get; set; }

        public List<QuantityUnitLocalizedModel> Locales { get; set; } = new();
    }

    public class QuantityUnitLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.QuantityUnit.Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Configuration.QuantityUnit.Fields.NamePlural")]
        public string NamePlural { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }
    }

    public partial class QuantityUnitValidator : AbstractValidator<QuantityUnitModel>
    {
        public QuantityUnitValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.NamePlural).NotEmpty();
        }
    }
}
