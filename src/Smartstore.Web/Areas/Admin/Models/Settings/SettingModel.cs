using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Settings
{
    [LocalizedDisplay("Admin.Configuration.Settings.AllSettings.Fields.")]
    public class SettingModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Value")]
        public string Value { get; set; }

        [LocalizedDisplay("*StoreName")]
        public string Store { get; set; }
        public int StoreId { get; set; }
    }

    public partial class SettingValidator : AbstractValidator<SettingModel>
    {
        public SettingValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
