namespace Smartstore.Google.MerchantCenter.Models
{
    [CustomModelPart]
    [Serializable]
    public class ProfileConfigurationModel
    {
        [LocalizedDisplay("Plugins.Feed.Froogle.DefaultGoogleCategory")]
        public string DefaultGoogleCategory { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.AdditionalImages")]
        public bool AdditionalImages { get; set; } = true;

        [LocalizedDisplay("Plugins.Feed.Froogle.Availability")]
        public string Availability { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.SpecialPrice")]
        public bool SpecialPrice { get; set; } = true;

        [UIHint("Gender")]
        [LocalizedDisplay("Plugins.Feed.Froogle.Gender")]
        public string Gender { get; set; }

        [UIHint("AgeGroup")]
        [LocalizedDisplay("Plugins.Feed.Froogle.AgeGroup")]
        public string AgeGroup { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.Color")]
        public string Color { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.Size")]
        public string Size { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.Material")]
        public string Material { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.Pattern")]
        public string Pattern { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.ExpirationDays")]
        public int ExpirationDays { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.ExportShipping")]
        public bool ExportShipping { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.ExportShippingTime")]
        public bool ExportShippingTime { get; set; }

        [LocalizedDisplay("Plugins.Feed.Froogle.ExportBasePrice")]
        public bool ExportBasePrice { get; set; }
    }

    public class ProfileConfigurationValidator : AbstractValidator<ProfileConfigurationModel>
    {
        public ProfileConfigurationValidator()
        {
            RuleFor(x => x.ExpirationDays).InclusiveBetween(0, 29);
        }
    }
}