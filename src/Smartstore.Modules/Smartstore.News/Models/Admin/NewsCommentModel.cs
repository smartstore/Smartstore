using System;
using Smartstore.Web.Modelling;

namespace Smartstore.News.Models
{
    [LocalizedDisplay("Admin.ContentManagement.News.Comments.Fields.")]
    public class NewsCommentModel : EntityModelBase
    {
        [LocalizedDisplay("*NewsItem")]
        public int NewsItemId { get; set; }

        [LocalizedDisplay("*NewsItem")]
        public string NewsItemTitle { get; set; }

        [LocalizedDisplay("*Customer")]
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        [LocalizedDisplay("*IPAddress")]
        public string IpAddress { get; set; }

        [LocalizedDisplay("*CommentTitle")]
        public string CommentTitle { get; set; }

        [LocalizedDisplay("*CommentText")]
        public string CommentText { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public string EditNewsItemUrl { get; set; }
        public string EditCustomerUrl { get; set; }
    }
}
