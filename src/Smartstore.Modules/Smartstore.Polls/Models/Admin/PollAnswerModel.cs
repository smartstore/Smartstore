using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Polls.Models
{
    public class PollAnswerModel : EntityModelBase
    {
        public int PollId { get; set; }

        [LocalizedDisplay("Admin.ContentManagement.Polls.Answers.Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.ContentManagement.Polls.Answers.Fields.NumberOfVotes")]
        public int NumberOfVotes { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        //we don't name it "DisplayOrder" because Telerik has a small bug 
        //"if we have one more editor with the same name on a page, it doesn't allow editing"
        //in our case it's pollAnswer.DisplayOrder
        public int DisplayOrder1 { get; set; }
    }

    public partial class PollAnswerValidator : AbstractValidator<PollAnswerModel>
    {
        public PollAnswerValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
