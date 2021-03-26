using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Catalog
{
    // TODO: (mh) (core) uncomment when validation is available.
    //[Validator(typeof(ProductAskQuestionValidator))]
    public partial class ProductAskQuestionModel : EntityModelBase
    {
        public LocalizedValue<string> ProductName { get; set; }

        public string ProductSeName { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("Account.Fields.Email")]
        public string SenderEmail { get; set; }

        [LocalizedDisplay("Account.Fields.FullName")]
        public string SenderName { get; set; }
        public bool SenderNameRequired { get; set; }

        [LocalizedDisplay("Account.Fields.Phone")]
        public string SenderPhone { get; set; }

        [Required]
        [SanitizeHtml]
        [LocalizedDisplay("Common.Question")]
        public string Question { get; set; }

        public string SelectedAttributes { get; set; }
        public string ProductUrl { get; set; }
        public bool IsQuoteRequest { get; set; }

        public bool DisplayCaptcha { get; set; }
    }

    // TODO: (mh) (core) uncomment when validation is available.
    //public class ProductAskQuestionValidator : AbstractValidator<ProductAskQuestionModel>
    //{
    //    public ProductAskQuestionValidator(PrivacySettings privacySettings)
    //    {
    //        if (privacySettings.FullNameOnProductRequestRequired)
    //        {
    //            RuleFor(x => x.SenderName).NotEmpty();
    //        }
    //    }
    //}
}
