using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Configuration
{
    public class SocialSettings : ISettings
    {
        /// <summary>
        /// Gets or sets facebook app id.
        /// </summary>
        public string FacebookAppId { get; set; }

        /// <summary>
        /// Gets or sets twitter account site name.
        /// </summary>
        public string TwitterSite { get; set; }

        /// <summary>
        /// Gets or sets the value that determines whether social links should be show in the footer.
        /// </summary>
        public bool ShowSocialLinksInFooter { get; set; } = true;

        /// <summary>
        /// Gets or sets the Facebook link.
        /// </summary>
        public string FacebookLink { get; set; } = "#";

        /// <summary>
        /// Gets or sets the Twitter link.
        /// </summary>
        public string TwitterLink { get; set; } = "#";

        /// <summary>
        /// Gets or sets the Pinterest link.
        /// </summary>
        public string PinterestLink { get; set; }

        /// <summary>
        /// Gets or sets the Youtube link.
        /// </summary>
        public string YoutubeLink { get; set; } = "#";

        /// <summary>
        /// Gets or sets the Instagram link.
        /// </summary>
        public string InstagramLink { get; set; } = "#";

        /// <summary>
        /// Gets or sets the Flickr link.
        /// </summary>
        public string FlickrLink { get; set; }

        /// <summary>
        /// Gets or sets the LinkedIn link.
        /// </summary>
        public string LinkedInLink { get; set; }

        /// <summary>
        /// Gets or sets the Xing link.
        /// </summary>
        public string XingLink { get; set; }

        /// <summary>
        /// Gets or sets the TikTok link.
        /// </summary>
        public string TikTokLink { get; set; } = "#";

        /// <summary>
        /// Gets or sets the Snapchat link.
        /// </summary>
        public string SnapchatLink { get; set; }

        /// <summary>
        /// Gets or sets the Vimeo link.
        /// </summary>
        public string VimeoLink { get; set; }

        /// <summary>
        /// Gets or sets the Tumblr link.
        /// </summary>
        public string TumblrLink { get; set; }

        /// <summary>
        /// Gets or sets the Ello link.
        /// </summary>
        public string ElloLink { get; set; }

        /// <summary>
        /// Gets or sets the Behance link.
        /// </summary>
        public string BehanceLink { get; set; }
    }
}
