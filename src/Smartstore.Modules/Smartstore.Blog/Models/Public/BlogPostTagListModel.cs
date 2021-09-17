using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.Blog.Models.Public
{
    public partial class BlogPostTagListModel : ModelBase
    {
        public List<BlogPostTagModel> Tags { get; set; } = new();
    }
}
