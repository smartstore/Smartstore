using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Stores
{
    [LocalizedDisplay("Admin.Configuration.Stores.Fields.")]
    public partial class StoreModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Url")]
        public string Url { get; set; }

        [LocalizedDisplay("*SslEnabled")]
        public bool SslEnabled { get; set; }

        [LocalizedDisplay("*SslPort")]
        [AdditionalMetadata("invariant", true)]
        public int? SslPort { get; set; }

        [LocalizedDisplay("*SecureUrl")]
        public string SecureUrl { get; set; }

        [LocalizedDisplay("*ForceSslForAllPages")]
        public bool ForceSslForAllPages { get; set; }

        [LocalizedDisplay("*Hosts")]
        public string Hosts { get; set; }
        public string[] HostList { get; set; }

        [LocalizedDisplay("*StoreLogo")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("transientUpload", true)]
        [AdditionalMetadata("entityType", "Store")]
        public int LogoMediaFileId { get; set; }

        [LocalizedDisplay("*FavIconMediaFileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("typeFilter", ".ico")]
        [AdditionalMetadata("transientUpload", true)]
        [AdditionalMetadata("entityType", "Store")]
        public int? FavIconMediaFileId { get; set; }

        [LocalizedDisplay("*PngIconMediaFileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("typeFilter", ".png")]
        [AdditionalMetadata("transientUpload", true)]
        [AdditionalMetadata("entityType", "Store")]
        public int? PngIconMediaFileId { get; set; }

        [LocalizedDisplay("*AppleTouchIconMediaFileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("typeFilter", "image")]
        [AdditionalMetadata("transientUpload", true)]
        [AdditionalMetadata("entityType", "Store")]
        public int? AppleTouchIconMediaFileId { get; set; }

        [LocalizedDisplay("*MsTileImageMediaFileId")]
        [UIHint("Media")]
        [AdditionalMetadata("album", "content")]
        [AdditionalMetadata("typeFilter", "image")]
        [AdditionalMetadata("transientUpload", true)]
        [AdditionalMetadata("entityType", "Store")]
        public int? MsTileImageMediaFileId { get; set; }

        [LocalizedDisplay("*MsTileColor")]
        [UIHint("Color")]
        [AdditionalMetadata("swatches", false)]
        public string MsTileColor { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*HtmlBodyId")]
        public string HtmlBodyId { get; set; }

        [LocalizedDisplay("*ContentDeliveryNetwork")]
        public string ContentDeliveryNetwork { get; set; }

        [LocalizedDisplay("*DefaultCurrencyId")]
        public int DefaultCurrencyId { get; set; }

        public string EditUrl { get; set; }
    }

    public partial class StoreValidator : AbstractValidator<StoreModel>
    {
        public StoreValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty();

            RuleFor(x => x.Url)
                .Must(x => x.HasValue() && x.IsWebUrl())
                .WithMessage(T("Admin.Validation.Url"));

            RuleFor(x => x.SslPort)
                .InclusiveBetween(0, ushort.MaxValue)
                .When(x => x.SslPort.HasValue);

            RuleFor(x => x.HtmlBodyId).Matches(@"^([A-Za-z])(\w|\-)*$")
                .WithMessage(T("Admin.Configuration.Stores.Fields.HtmlBodyId.Validation"));
        }
    }
}