using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FluentValidation;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Localization
{
    [LocalizedDisplay("Admin.Configuration.Languages.Fields.")]
    public class LanguageModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*LanguageCulture")]
        public string LanguageCulture { get; set; }

        [LocalizedDisplay("*UniqueSeoCode")]
        public string UniqueSeoCode { get; set; }

        [LocalizedDisplay("*FlagImageFileName")]
        public string FlagImageFileName { get; set; }
        public List<string> FlagFileNames { get; set; } = new();

        [LocalizedDisplay("*Rtl")]
        public bool Rtl { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        // Store mapping.
        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        [LocalizedDisplay("*AvailableLanguageSetId")]
        public int AvailableLanguageSetId { get; set; }

        [LocalizedDisplay("*LastResourcesImportOn")]
        public DateTime? LastResourcesImportOn { get; set; }

        [LocalizedDisplay("*LastResourcesImportOn")]
        public string LastResourcesImportOnString { get; set; }
    }

    public partial class LanguageValidator : AbstractValidator<LanguageModel>
    {
        public LanguageValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.FlagImageFileName).NotEmpty();
            RuleFor(x => x.UniqueSeoCode).NotEmpty();
            // Annoying, never validating rule.
            //RuleFor(x => x.UniqueSeoCode).Length(2);

            RuleFor(x => x.LanguageCulture)
                .Must(x =>
                {
                    try
                    {
                        var culture = new CultureInfo(x);
                        return culture != null;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage(T("Admin.Configuration.Languages.Fields.LanguageCulture.Validation"));
        }
    }
}
