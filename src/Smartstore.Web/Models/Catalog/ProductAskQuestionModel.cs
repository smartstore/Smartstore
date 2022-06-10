using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductAskQuestionModel : EntityModelBase
    {
        public LocalizedValue<string> ProductName { get; set; }

        public string ProductSeName { get; set; }

        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("Account.Fields.Email")]
        public string SenderEmail { get; set; }

        [LocalizedDisplay("Account.Fields.FullName")]
        public string SenderName { get; set; }
        public bool SenderNameRequired { get; set; }

        [LocalizedDisplay("Account.Fields.Phone")]
        public string SenderPhone { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("Common.Question")]
        public string Question { get; set; }

        public string SelectedAttributes { get; set; }
        public string ProductUrl { get; set; }
        public bool IsQuoteRequest { get; set; }

        public bool DisplayCaptcha { get; set; }
    }

    public class ProductAskQuestionValidator : SmartValidator<ProductAskQuestionModel>
    {
        public ProductAskQuestionValidator(PrivacySettings privacySettings)
        {
            RuleFor(x => x.SenderEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.Question).NotEmpty();

            if (privacySettings.FullNameOnProductRequestRequired)
            {
                RuleFor(x => x.SenderName).NotEmpty();
            }
        }
    }
}
