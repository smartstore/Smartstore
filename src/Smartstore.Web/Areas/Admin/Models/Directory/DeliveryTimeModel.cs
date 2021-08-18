using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FluentValidation;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Directory
{
    public class DeliveryTimeModel : EntityModelBase, ILocalizedModel<DeliveryTimeLocalizedModel>
    {
        [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.Name")]
        public string Name { get; set; }
        public string DeliveryInfo { get; set; }

        [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.DisplayLocale")]
        public string DisplayLocale { get; set; }

        [UIHint("Color")]
        [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.Color")]
        public string ColorHexValue { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.IsDefault")]
        public bool IsDefault { get; set; }

        [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.MinDays")]
        public int? MinDays { get; set; }

        [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.MaxDays")]
        public int? MaxDays { get; set; }

        public List<DeliveryTimeLocalizedModel> Locales { get; set; } = new();
    }

    public class DeliveryTimeLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.DeliveryTimes.Fields.Name")]
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
                        if (string.IsNullOrEmpty(x))
                            return true;

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
