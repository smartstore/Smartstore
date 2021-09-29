using System;
using Smartstore.Web.Modelling;

namespace Smartstore.Polls.Models
{
    [LocalizedDisplay("Admin.Customers.Customers.Fields.")]
    public class PollVotingRecordModel : EntityModelBase
    {
        public int CustomerId { get; set; }
        public bool IsGuest { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Common.Answer")]
        public string AnswerName { get; set; }

        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        [LocalizedDisplay("*FullName")]
        public string FullName { get; set; }

    }
}
