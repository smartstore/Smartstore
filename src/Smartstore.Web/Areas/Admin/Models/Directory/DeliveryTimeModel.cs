using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FluentValidation;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.")]
    public class DeliveryTimeModel : EntityModelBase, ILocalizedModel<DeliveryTimeLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }
        public string DeliveryInfo { get; set; }

        public string DisplayLocale { get; set; }

        [UIHint("Color")]
        [LocalizedDisplay("*Color")]
        public string ColorHexValue { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*IsDefault")]
        public bool IsDefault { get; set; }

        [LocalizedDisplay("*MinDays")]
        public int? MinDays { get; set; }

        [LocalizedDisplay("*MaxDays")]
        public int? MaxDays { get; set; }

        public List<DeliveryTimeLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.")]
    public class DeliveryTimeLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }
    }

    public partial class DeliveryTimeValidator : AbstractValidator<DeliveryTimeModel>
    {
        public DeliveryTimeValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty().Length(1, 50);
            RuleFor(x => x.ColorHexValue).NotEmpty().Length(1, 50);

            RuleFor(x => x.DisplayLocale)
                .Must(x =>
                {
                    try
                    {
                        if (x.IsEmpty())
                        {
                            return true;
                        }

                        var culture = new CultureInfo(x);
                        return culture != null;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage(T("Admin.Configuration.DeliveryTimes.Fields.DisplayLocale.Validation"));

            RuleFor(x => x.MinDays)
                .GreaterThan(0)
                .When(x => x.MinDays.HasValue);

            RuleFor(x => x.MaxDays)
                .GreaterThan(0)
                .When(x => x.MaxDays.HasValue);

            When(x => x.MinDays.HasValue && x.MaxDays.HasValue, () =>
            {
                RuleFor(x => x.MaxDays).GreaterThanOrEqualTo(x => x.MinDays);
            });
        }
    }
}
