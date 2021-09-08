using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Web.Modelling;

namespace Smartstore.Blog.Models.Public
{
    public partial class BlogPostTagListModel : ModelBase
    {
        // TODO: (mh) (core) Lets eliminate this model and use ViewBag instead
        public List<BlogPostTagModel> Tags { get; set; } = new();
    }
}
