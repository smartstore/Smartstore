using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Content.Topics
{
    /// <summary>
    /// Represents a topic.
    /// </summary>
    [CacheableEntity]
    [LocalizedEntity("IsPublished and !IsSystemTopic")]
    public partial class Topic : EntityWithAttributes, ILocalizedEntity, ISlugSupported, IStoreRestricted, IAclRestricted
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this topic is deleteable by a user.
        /// </summary>
        public bool IsSystemTopic { get; set; }

        /// <summary>
        /// Gets or sets the html id.
        /// </summary>
        public string HtmlId { get; set; }

        /// <summary>
        /// Gets or sets the body css class.
        /// </summary>
        public string BodyCssClass { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this topic should be included in sitemap.
        /// </summary>
        public bool IncludeInSitemap { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this topic is password protected.
        /// </summary>
        public bool IsPasswordProtected { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [LocalizedProperty]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the short title (for links).
        /// </summary>
        [StringLength(50)]
        [LocalizedProperty]
        public string ShortTitle { get; set; }

        /// <summary>
        /// Gets or sets the intro.
        /// </summary>
        [StringLength(255)]
        [LocalizedProperty]
        public string Intro { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        [MaxLength]
        [LocalizedProperty]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the meta keywords.
        /// </summary>
        [LocalizedProperty]
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Gets or sets the meta description.
        /// </summary>
        [LocalizedProperty]
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the meta title.
        /// </summary>
        [LocalizedProperty]
        public string MetaTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the topic should also be rendered as a generic html widget.
        /// </summary>
        public bool RenderAsWidget { get; set; }

        /// <summary>
        /// Gets or sets the widget zone name.
        /// </summary>
        public string WidgetZone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content should be surrounded by a topic block wrapper.
        /// </summary>
        public bool? WidgetWrapContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the title should be displayed in the widget block.
        /// </summary>
        public bool WidgetShowTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the widget block should have borders.
        /// </summary>
        public bool WidgetBordered { get; set; }

        /// <summary>
        /// Gets or sets the sort order (relevant for widgets).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the title tag.
        /// </summary>
        public string TitleTag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is subject to ACL.
        /// </summary>
        public bool SubjectToAcl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the topic page is published.
        /// </summary>
        public bool IsPublished { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the topic set a cookie and the cookie type.
        /// </summary>
        public CookieType? CookieType { get; set; }

        /// <summary>
        /// Helper function which gets the comma-separated <c>WidgetZone</c> property as list of strings.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetWidgetZones()
        {
            if (this.WidgetZone.IsEmpty())
            {
                return Enumerable.Empty<string>();
            }

            return this.WidgetZone.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
        }

        /// <inheritdoc/>
        public string GetDisplayName()
        {
            return Title.NullEmpty() ?? ShortTitle.NullEmpty() ?? SystemName;
        }

        /// <inheritdoc/>
        public string[] GetDisplayNameMemberNames()
        {
            return new[] { nameof(Title), nameof(ShortTitle), nameof(SystemName) };
        }
    }
}
