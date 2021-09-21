using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Forums.Models.Public
{
    public partial class PublicForumModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public LocalizedValue<string> Description { get; set; }
        public string Slug { get; set; }
        public int NumTopics { get; set; }
        public int NumPosts { get; set; }
        public int LastPostId { get; set; }

        public PublicForumPostModel LastPost { get; set; }
    }
}
