using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Admin.Models.Localization
{
    [LocalizedDisplay("Admin.Configuration.Languages.Resources.List.")]
    public class LanguageResourceListModel : ModelBase
    {
        public int LanguageId { get; set; }
        public string LanguageName { get; set; }

        [LocalizedDisplay("*Name")]
        public string ResourceName { get; set; }

        [LocalizedDisplay("*Value")]
        public string ResourceValue { get; set; }
    }

    [LocalizedDisplay("Admin.Configuration.Languages.Resources.Fields.")]
    public class LanguageResourceModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string ResourceName { get; set; }

        [LocalizedDisplay("*Value")]
        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 1)]
        public string ResourceValue { get; set; }

        [LocalizedDisplay("*LanguageName")]
        public string LanguageName { get; set; }

        public int LanguageId { get; set; }
    }

    public partial class LanguageResourceValidator : AbstractValidator<LanguageResourceModel>
    {
        public LanguageResourceValidator()
        {
            RuleFor(x => x.ResourceName).NotEmpty();
            RuleFor(x => x.ResourceValue).NotEmpty();
        }
    }
}
