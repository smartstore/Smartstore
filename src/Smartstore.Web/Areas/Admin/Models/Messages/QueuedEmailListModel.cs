namespace Smartstore.Admin.Models.Messages
{
    [LocalizedDisplay("Admin.System.QueuedEmails.List.")]
    public class QueuedEmailListModel : ModelBase
    {
        [LocalizedDisplay("*StartDate")]
        public DateTime? SearchStartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? SearchEndDate { get; set; }

        [LocalizedDisplay("*FromEmail")]
        public string SearchFromEmail { get; set; }

        [LocalizedDisplay("*ToEmail")]
        public string SearchToEmail { get; set; }

        [LocalizedDisplay("*LoadNotSent")]
        public bool SearchLoadNotSent { get; set; } = true;

        [LocalizedDisplay("*SendManually")]
        public bool? SearchSendManually { get; set; }

        [LocalizedDisplay("*MaxSentTries")]
        public int SearchMaxSentTries { get; set; } = 10;

        [LocalizedDisplay("*GoDirectlyToNumber")]
        public int? GoDirectlyToNumber { get; set; }
    }
}
