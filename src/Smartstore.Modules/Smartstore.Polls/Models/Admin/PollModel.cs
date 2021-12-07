namespace Smartstore.Polls.Models
{
    [LocalizedDisplay("Admin.ContentManagement.Polls.Fields.")]
    public class PollModel : EntityModelBase
    {
        [LocalizedDisplay("*Language")]
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Language")]
        public string LanguageName { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*SystemKeyword")]
        public string SystemKeyword { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("*ShowOnHomePage")]
        public bool ShowOnHomePage { get; set; }

        [LocalizedDisplay("*AllowGuestsToVote")]
        public bool AllowGuestsToVote { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        public string EditUrl { get; set; }
    }

    public partial class PollValidator : AbstractValidator<PollModel>
    {
        public PollValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
