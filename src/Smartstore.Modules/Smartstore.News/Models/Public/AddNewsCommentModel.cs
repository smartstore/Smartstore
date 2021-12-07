namespace Smartstore.News.Models.Public
{
    public partial class AddNewsCommentModel : ModelBase
    {
        [LocalizedDisplay("News.Comments.CommentTitle")]
        public string CommentTitle { get; set; }

        [LocalizedDisplay("News.Comments.CommentText")]
        public string CommentText { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}
