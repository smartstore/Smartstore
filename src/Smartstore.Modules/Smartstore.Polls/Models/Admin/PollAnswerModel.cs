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
        public int DisplayOrder { get; set; }
    }

    public partial class PollAnswerValidator : AbstractValidator<PollAnswerModel>
    {
        public PollAnswerValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
