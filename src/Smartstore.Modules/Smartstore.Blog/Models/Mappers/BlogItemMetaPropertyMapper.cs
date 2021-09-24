using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Blog.Models.Public;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Common.Mappers;

namespace Smartstore.Blog.Models.Mappers
{
    public static partial class BlogPostMappingExtensions
    {
        public static async Task<MetaPropertiesModel> MapMetaPropertiesAsync(this PublicBlogPostModel model, dynamic parameters = null)
        {
            var mapper = MapperFactory.GetMapper<PublicBlogPostModel, MetaPropertiesModel>();
            var result = new MetaPropertiesModel();
            await mapper.MapAsync(model, result, parameters);
            return result;
        }
    }

    public class BlogPostModelMetaPropertyMapper : MetaPropertiesMapperBase<PublicBlogPostModel>
    {
        private readonly IUrlHelper _urlHelper;

        public BlogPostModelMetaPropertyMapper(IHttpContextAccessor httpContextAccessor, IUrlHelper urlHelper)
            : base(httpContextAccessor)
        {
            _urlHelper = urlHelper;
        }

        protected override MediaFileInfo GetMediaFile(PublicBlogPostModel from)
        {
            return from.Image?.File ?? from.Preview?.File;
        }

        protected override Task MapCoreAsync(PublicBlogPostModel source, MetaPropertiesModel model, dynamic parameters = null)
        {
            model.Url = _urlHelper.RouteUrl("BlogPost", new { source.SeName }, HttpContext.Request.Scheme);
            model.Title = source.MetaTitle;
            model.Description = source.MetaDescription;
            model.PublishedTime = source.CreatedOnUTC;
            model.Type = "article";

            if (source.Tags.Count > 0)
            {
                model.ArticleTags = source.Tags.Select(x => x.Name);
                model.ArticleSection = model.ArticleTags.First();
            }

            return Task.FromResult(model);
        }
    }
}
