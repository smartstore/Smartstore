using Smartstore.Core.Localization;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.Core.Seo
{
    public partial interface IXmlSitemapPublisher
    {
        XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context);
    }

    public abstract class XmlSitemapProvider
    {
        public virtual int Order { get; }

        public virtual Task<int> GetTotalCountAsync()
            => Task.FromResult(0);

        public virtual IAsyncEnumerable<NamedEntity> EnlistAsync(CancellationToken cancelToken = default)
            => AsyncEnumerable.Empty<NamedEntity>();

        /// <summary>
        /// Creates a sitemap node for <paramref name="entity"/>.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="entity"></param>
        /// <param name="slugs">Collection of all slugs.</param>
        /// <param name="language">Aktuelle Sprache, für die Sitemap nodes erstellt werden.</param>
        /// <param name="ctx">Contains extra metadata.</param>
        public virtual XmlSitemapNode CreateNode(
            string baseUrl, 
            NamedEntity entity, 
            UrlRecordCollection slugs,
            Language language, 
            XmlSitemapBuildNodeContext ctx)
        {
            var slug = slugs.GetSlug(language.Id, entity.Id, true);
            //var path = ctx.LinkGenerator.GetPathByRouteValues(entity.EntityName, new { SeName = slug }).EmptyNull().TrimStart('/');
            //var loc = baseUrl + path;

            if (slug == null)
            {
                return null;
            }

            return new()
            {
                LastMod = entity.LastMod,
                Loc = baseUrl + RouteHelper.NormalizePathComponent(slug.EmptyNull().TrimStart('/')),
                Links = CreateLinks(entity, slugs, ctx)
            };
        }

        /// <summary>
        /// Creates a list of alternative links for the given entity.
        /// </summary>
        /// <param name="entity">Entity ro create alternative links for.</param>
        /// <param name="slugs">Collection of all slugs.</param>
        /// <param name="ctx">Provides the languages for which the alternative links are to be created.</param>
        public virtual IEnumerable<XmlSitemapNode.LinkEntry> CreateLinks(NamedEntity entity, UrlRecordCollection slugs, XmlSitemapBuildNodeContext ctx)
        {
            if (ctx.LinkLanguages.IsNullOrEmpty())
            {
                return null;
            }

            return [.. ctx.LinkLanguages
                .Select(lang =>
                {
                    // TODO (?): Slug for default value fallback (languageId == 0) is never provided by sitemap generation.
                    var slug = slugs.GetSlug(lang.Language.Id, entity.Id, lang.Language.Id == ctx.DefaultLanguageId);
                    if (slug != null)
                    {
                        return new XmlSitemapNode.LinkEntry
                        {
                            Lang = lang.Language.LanguageCulture,
                            Href = lang.BaseUrl + RouteHelper.NormalizePathComponent(slug.EmptyNull().TrimStart('/'))
                        };
                    }

                    return null;
                })
                .Where(x => x != null)];
        }
    }
}
