using Smartstore.Web.Modelling;

namespace Smartstore.Blog.Models.Public
{
    public partial class BlogPostTagModel : ModelBase
    {
        public string Name { get; set; }

        public string SeName { get; set; }

        public int BlogPostCount { get; set; }
    }
}
