using FluentValidation;

namespace Smartstore.Web.Models.Orders
{
    [LocalizedDisplay("ReturnRequests.")]
    public partial class AddReturnCaseModel : ModelBase
    {
        public int OrderId { get; set; }

        public ReturnCaseItemsModel Items { get; set; }

        [LocalizedDisplay("*ReturnReason")]
        public string ReturnReason { get; set; }

        [LocalizedDisplay("*ReturnAction")]
        public string ReturnAction { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("*Comments")]
        public string Comments { get; set; }
    }

    public class AddReturnCaseModelValidator : SmartValidator<AddReturnCaseModel>
    {
        public AddReturnCaseModelValidator()
        {
            RuleFor(x => x.ReturnReason).NotEmpty();
            RuleFor(x => x.ReturnAction).NotEmpty();
        }
    }
}
