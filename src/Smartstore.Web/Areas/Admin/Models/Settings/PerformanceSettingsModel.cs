using FluentValidation;

namespace Smartstore.Admin.Models
{
    public partial class PerformanceSettingsModel
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

            [LocalizedDisplay("*LongTrafficWindow")]
            public TimeSpan LongTrafficWindow { get; set; }

            [LocalizedDisplay("*PeakTrafficWindow")]
            public TimeSpan PeakTrafficWindow { get; set; }

            [LocalizedDisplay("*LongTrafficLimitGuest")]
            public int? LongTrafficLimitGuest { get; set; }

            [LocalizedDisplay("*LongTrafficLimitBot")]
            public int? LongTrafficLimitBot { get; set; }

            [LocalizedDisplay("*PeakTrafficLimitGuest")]
            public int? PeakTrafficLimitGuest { get; set; }

            [LocalizedDisplay("*PeakTrafficLimitBot")]
            public int? PeakTrafficLimitBot { get; set; }

            [LocalizedDisplay("*LongTrafficLimitGlobal")]
            public int? LongTrafficLimitGlobal { get; set; }

            [LocalizedDisplay("*PeakTrafficLimitGlobal")]
            public int? PeakTrafficLimitGlobal { get; set; }
        }
    }

    public partial class PerformanceModelValidator : AbstractValidator<PerformanceSettingsModel.PerformanceModel>
    {
        public PerformanceModelValidator()
        {
            RuleFor(x => x.CacheSegmentSize).GreaterThan(0);
            RuleFor(x => x.MaxUnavailableAttributeCombinations).GreaterThan(0);
            RuleFor(x => x.MediaDupeDetectorMaxCacheSize).GreaterThan(0);
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
