using FluentValidation;

namespace Smartstore.Admin.Models
{
    public partial class PerformanceSettingsModel : ModelBase
    {
        public PerformanceModel PerformanceSettings { get; set; } = new();
        public ResiliencyModel ResiliencySettings { get; set; } = new();

        [LocalizedDisplay("Admin.Configuration.Settings.Performance.")]
        public class PerformanceModel
        {
            [LocalizedDisplay("*CacheSegmentSize")]
            public int CacheSegmentSize { get; set; }

            public string Yodele { get; set; }

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
        public class ResiliencyModel
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

    public partial class PerformanceModelValidator : AbstractValidator<PerformanceSettingsModel.PerformanceModel>
    {
        public PerformanceModelValidator()
        {
            RuleFor(x => x.Yodele).Length(2, 4);
            RuleFor(x => x.CacheSegmentSize).GreaterThan(100).LessThan(1000);
            RuleFor(x => x.MaxUnavailableAttributeCombinations).GreaterThan(100);
            RuleFor(x => x.MediaDupeDetectorMaxCacheSize).GreaterThan(100);
        }
    }

    public partial class ResiliencyModelValidator : AbstractValidator<PerformanceSettingsModel.ResiliencyModel>
    {
        public ResiliencyModelValidator()
        {
            // TODO: Implement validation rules for ResiliencyModel
        }
    }
}
