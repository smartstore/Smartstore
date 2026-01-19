namespace Smartstore.Google.MerchantCenter.Models
{
    [CustomModelPart]
    [Serializable]
    [LocalizedDisplay("Plugins.Feed.Froogle.")]
    public class ProfileConfigurationModel
    {
        [LocalizedDisplay("*DefaultGoogleCategory")]
        public string DefaultGoogleCategory { get; set; }

        [LocalizedDisplay("*ExportAllProducts")]
        public bool ExportAllProducts { get; set; } = true;

        [LocalizedDisplay("*AdditionalImages")]
        public bool AdditionalImages { get; set; } = true;

        [LocalizedDisplay("*Availability")]
        public string Availability { get; set; }

        [LocalizedDisplay("*SpecialPrice")]
        public bool SpecialPrice { get; set; } = true;

        [UIHint("Gender")]
        [LocalizedDisplay("*Gender")]
        public string Gender { get; set; }

        [UIHint("AgeGroup")]
        [LocalizedDisplay("*AgeGroup")]
        public string AgeGroup { get; set; }

        [LocalizedDisplay("*Color")]
        public string Color { get; set; }

        [LocalizedDisplay("*Size")]
        public string Size { get; set; }

        [LocalizedDisplay("*Material")]
        public string Material { get; set; }

        [LocalizedDisplay("*Pattern")]
        public string Pattern { get; set; }

        [LocalizedDisplay("*ExpirationDays")]
        public int ExpirationDays { get; set; }

        [LocalizedDisplay("*ExportShipping")]
        public bool ExportShipping { get; set; }

        [LocalizedDisplay("*ExportShippingTime")]
        public bool ExportShippingTime { get; set; }

        [LocalizedDisplay("*ExportBasePrice")]
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