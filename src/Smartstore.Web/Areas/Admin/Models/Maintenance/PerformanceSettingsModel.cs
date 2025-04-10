using FluentValidation;

namespace Smartstore.Admin.Models.Maintenance
{
    public partial class PerformanceSettingsModel : ModelBase
    {
        public PerformanceModel PerformanceSettings { get; set; } = new();
        public ResiliencyModel ResiliencySettings { get; set; } = new();

        [LocalizedDisplay("Admin.Configuration.Settings.Performance.")]
        public partial class PerformanceModel
        {
            [LocalizedDisplay("*CacheSegmentSize")]
            public int CacheSegmentSize { get; set; }

            [LocalizedDisplay("*AlwaysPrefetchTranslations")]
            public bool AlwaysPrefetchTranslations { get; set; }

            [LocalizedDisplay("*AlwaysPrefetchUrlSlugs")]
            public bool AlwaysPrefetchUrlSlugs { get; set; }

            [LocalizedDisplay("*MaxUnavailableAttributeCombinations")]
            public int MaxUnavailableAttributeCombinations { get; set; }

            [LocalizedDisplay("*UseResponseCompression")]
            public bool UseResponseCompression { get; set; }

            [LocalizedDisplay("*MediaDupeDetectorMaxCacheSize")]
            public int MediaDupeDetectorMaxCacheSize { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.Resiliency.")]
        public partial class ResiliencyModel
        {
            [LocalizedDisplay("*EnableOverloadProtection")]
            public bool EnableOverloadProtection { get; set; }

            [LocalizedDisplay("*ForbidNewGuestsIfSubRequest")]
            public bool ForbidNewGuestsIfSubRequest { get; set; }

            [LocalizedDisplay("*TrafficTimeWindow")]
            public TimeSpan LongTrafficWindow { get; set; }

            [LocalizedDisplay("*TrafficLimitGuest")]
            public int? LongTrafficLimitGuest { get; set; }

            [LocalizedDisplay("*TrafficLimitBot")]
            public int? LongTrafficLimitBot { get; set; }

            [LocalizedDisplay("*TrafficLimitGlobal")]
            public int? LongTrafficLimitGlobal { get; set; }

            [LocalizedDisplay("*TrafficTimeWindow")]
            public TimeSpan PeakTrafficWindow { get; set; }

            [LocalizedDisplay("*TrafficLimitGuest")]
            public int? PeakTrafficLimitGuest { get; set; }

            [LocalizedDisplay("*TrafficLimitBot")]
            public int? PeakTrafficLimitBot { get; set; }

            [LocalizedDisplay("*TrafficLimitGlobal")]
            public int? PeakTrafficLimitGlobal { get; set; }
        }
    }

    public class PerformanceSettingsModelValidator : AbstractValidator<PerformanceSettingsModel>
    {
        public PerformanceSettingsModelValidator()
        {
            RuleFor(x => x.PerformanceSettings).SetValidator(new PerformanceModelValidator());
            RuleFor(x => x.ResiliencySettings).SetValidator(new ResiliencyModelValidator());
        }

        class PerformanceModelValidator : AbstractValidator<PerformanceSettingsModel.PerformanceModel>
        {
            public PerformanceModelValidator()
            {
                RuleFor(x => x.CacheSegmentSize).GreaterThan(0);
                RuleFor(x => x.MaxUnavailableAttributeCombinations).GreaterThan(0);
                RuleFor(x => x.MediaDupeDetectorMaxCacheSize).GreaterThan(0);
            }
        }

        class ResiliencyModelValidator : AbstractValidator<PerformanceSettingsModel.ResiliencyModel>
        {
            public ResiliencyModelValidator()
            {
                RuleFor(x => x.LongTrafficWindow)
                    .GreaterThan(TimeSpan.FromSeconds(0))
                    .GreaterThan(x => x.PeakTrafficWindow);

                RuleFor(x => x.PeakTrafficWindow).GreaterThan(TimeSpan.FromSeconds(0));

                RuleFor(x => x.LongTrafficLimitBot).GreaterThan(0);
                RuleFor(x => x.LongTrafficLimitGuest).GreaterThan(0);
                RuleFor(x => x.LongTrafficLimitGlobal).GreaterThan(0);
                RuleFor(x => x.PeakTrafficLimitBot).GreaterThan(0);
                RuleFor(x => x.PeakTrafficLimitGuest).GreaterThan(0);
                RuleFor(x => x.PeakTrafficLimitGlobal).GreaterThan(0);
            }
        }
    }
}
