using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.AllSettings.Fields.")]
    public class SettingModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Value")]
        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 1)]
        public string Value { get; set; }

        [LocalizedDisplay("*StoreName")]
        [UIHint("Stores")]
        public int? StoreId { get; set; }

        public string Store { get; set; }
    }

    public partial class SettingValidator : AbstractValidator<SettingModel>
    {
        public SettingValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
