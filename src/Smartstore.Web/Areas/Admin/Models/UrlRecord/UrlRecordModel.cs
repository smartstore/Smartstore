using FluentValidation;

namespace Smartstore.Admin.Models.UrlRecord
{
    [LocalizedDisplay("Admin.System.SeNames.")]
    public partial class UrlRecordModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Slug { get; set; }

        [LocalizedDisplay("*EntityName")]
        public string LocalizedEntityName { get; set; }

        [LocalizedDisplay("*EntityName")]
        public string EntityName { get; set; }

        [LocalizedDisplay("*EntityId")]
        public int EntityId { get; set; }

        [LocalizedDisplay("Admin.Common.Entity")]
        public string EntityUrl { get; set; }

        [LocalizedDisplay("*IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*Language")]
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Language")]
        public string Language { get; set; }
        public string FlagImageUrl { get; set; }

        [LocalizedDisplay("*SlugsPerEntity")]
        public int SlugsPerEntity { get; set; }
        public string EditUrl { get; set; }
    }

    public partial class UrlRecordValidator : AbstractValidator<UrlRecordModel>
    {
        public UrlRecordValidator()
        {
            RuleFor(x => x.Slug).NotEmpty();
            RuleFor(x => x.EntityName).NotEmpty();
            RuleFor(x => x.EntityId).GreaterThan(0);
        }
    }
}
