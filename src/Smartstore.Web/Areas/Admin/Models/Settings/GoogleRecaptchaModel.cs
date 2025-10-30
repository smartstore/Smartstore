using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Models.Security
{
    [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.GoogleRecaptcha.")]
    public class GoogleRecaptchaModel : ModelBase
    {
        [LocalizedDisplay("*SiteKey")]
        public string SiteKey { get; set; }

        [LocalizedDisplay("*SecretKey")]
        public string SecretKey { get; set; }

        [LocalizedDisplay("*Version")]
        public string Version { get; set; }

        [LocalizedDisplay("*UseDarkTheme")]
        public bool UseDarkTheme { get; set; }

        [LocalizedDisplay("*Size")]
        public string Size { get; set; }

        [LocalizedDisplay("*BadgePosition")]
        public string BadgePosition { get; set; }

        [LocalizedDisplay("*BadgePosition.Hide")]
        public bool HideBadgeV3 { get; set; }

        [LocalizedDisplay("*ScoreThreshold")]
        [UIHint("Range"), Range(0, 1)]
        [AdditionalMetadata("min", 0)]
        [AdditionalMetadata("max", 1)]
        [AdditionalMetadata("step", 0.1)]
        public float ScoreThreshold { get; set; }

        [LocalizedDisplay("*WidgetUrl")]
        public string WidgetUrl { get; set; }

        [LocalizedDisplay("*VerifyUrl")]
        public string VerifyUrl { get; set; }
    }

    public partial class GoogleRecaptchaSettingsValidator : SettingModelValidator<GoogleRecaptchaModel, GoogleRecaptchaSettings>
    {
        public GoogleRecaptchaSettingsValidator()
        {
            RuleFor(x => x.Version).NotEmpty();
            RuleFor(x => x.Size).NotEmpty();
            RuleFor(x => x.SiteKey).NotEmpty().Length(40);
            RuleFor(x => x.SecretKey).NotEmpty().Length(40);
        }
    }
}
