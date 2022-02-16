namespace Smartstore.Blog.Models
{
    [LocalizedDisplay("Admin.ContentManagement.Blog.Comments.Fields.")]
    public class BlogCommentModel : EntityModelBase
    {
        [LocalizedDisplay("*BlogPost")]
        public int BlogPostId { get; set; }

        [LocalizedDisplay("*BlogPost")]
        public string BlogPostTitle { get; set; }

        [LocalizedDisplay("*Customer")]
        public int CustomerId { get; set; }

        [LocalizedDisplay("*Customer")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("*IPAddress")]
        public string IpAddress { get; set; }

        [LocalizedDisplay("*Comment")]
        public string CommentText { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public string EditBlogPostUrl { get; set; }
        public string EditCustomerUrl { get; set; }
    }
}
