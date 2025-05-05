using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Common
{
    public class MetaPropertiesModel : ModelBase
    {
        public string Site { get; set; }
        public string SiteName { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public string ImageUrl { get; set; }
        public string ImageAlt { get; set; }
        public string ImageType { get; set; }
        public int ImageHeight { get; set; }
        public int ImageWidth { get; set; }

        public OpenGraphArticle Article { get; set; }

        public string TwitterSite { get; set; }
        public string FacebookAppId { get; set; }

        /// <summary>
        /// Represents Open Graph article meta properties.
        /// <see cref="https://ogp.me/#no_vertical"/>
        /// </summary>
        public class OpenGraphArticle
        {
            public DateTime? PublishedTime { get; set; }
            public string Author { get; set; }
            public string Section { get; set; }
            public string[] Tags { get; set; }
        }
    }
}
