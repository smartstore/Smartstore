using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Web.Modelling;

namespace Smartstore.Blog.Models.Public
{
    public partial class BlogPostListModel : ModelBase
    {
        public bool RenderHeading { get; set; }
        public string BlogHeading { get; set; }
        public bool RssToLinkButton { get; set; }
        public bool DisableCommentCount { get; set; }
        public BlogPagingFilteringModel PagingFilteringContext { get; set; } = new();
        public List<BlogPostModel> BlogPosts { get; set; } = new();
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
    }
}
