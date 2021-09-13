using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Seo;

namespace Smartstore.Forums.Services
{
    public partial class ForumService : IForumService, IXmlSitemapPublisher
    {
        public ForumService()
        {
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesForum || !context.LoadSettings<ForumSettings>().ForumsEnabled)
            {
                return null;
            }

            return null;
        }
    }
}
