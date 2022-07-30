
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Seo
{
    public partial interface IXmlSitemapPublisher
    {
        XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context);
    }

    public abstract class XmlSitemapProvider
    {
        public virtual Task<int> GetTotalCountAsync()
            => Task.FromResult(0);

        public virtual IAsyncEnumerable<NamedEntity> EnlistAsync(CancellationToken cancelToken = default)
            => AsyncEnumerable.Empty<NamedEntity>();

        public virtual XmlSitemapNode CreateNode(LinkGenerator linkGenerator, string baseUrl, NamedEntity entity, UrlRecordCollection slugs, Language language)
        {
            var slug = slugs.GetSlug(language.Id, entity.Id, true);
            //var path = linkGenerator.GetPathByRouteValues(entity.EntityName, new { SeName = slug }).EmptyNull().TrimStart('/');
            //var loc = baseUrl + path;

            if (slug == null)
            {
                return null;
            }

            return new XmlSitemapNode
            {
                LastMod = entity.LastMod,
                Loc = baseUrl + slug.EmptyNull().TrimStart('/')
            };
        }

        public virtual int Order { get; }
    }
}
