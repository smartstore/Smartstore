using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Localization
{
    [LocalizedDisplay("Admin.Configuration.Languages.Resources.Fields.")]
    public class LanguageResourceModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string ResourceName { get; set; }

        [LocalizedDisplay("*Value")]
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
