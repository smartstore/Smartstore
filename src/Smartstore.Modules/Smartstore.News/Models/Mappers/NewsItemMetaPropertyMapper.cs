using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.News.Models.Public;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Common.Mappers;

namespace Smartstore.News.Models.Mappers
{
    public static partial class NewsItemMappingExtensions
    {
        public static async Task<MetaPropertiesModel> MapMetaPropertiesAsync(this PublicNewsItemModel model, dynamic parameters = null)
        {
            var mapper = MapperFactory.GetMapper<PublicNewsItemModel, MetaPropertiesModel>();
            var result = new MetaPropertiesModel();
            await mapper.MapAsync(model, result, parameters);
            return result;
        }
    }

    public class NewsItemMetaPropertyMapper : MetaPropertiesMapperBase<PublicNewsItemModel>
    {
        private readonly IUrlHelper _urlHelper;

        public NewsItemMetaPropertyMapper(IHttpContextAccessor httpContextAccessor, IUrlHelper urlHelper)
            : base(httpContextAccessor)
        {
            _urlHelper = urlHelper;
        }

        protected override MediaFileInfo GetMediaFile(PublicNewsItemModel from)
        {
            return from.PictureModel?.File ?? from.PreviewPictureModel?.File;
        }

        protected override Task MapCoreAsync(PublicNewsItemModel source, MetaPropertiesModel model, dynamic parameters = null)
        {
            model.Url = _urlHelper.RouteUrl("NewsItem", new { source.SeName }, HttpContext.Request.Scheme);
            model.Title = source.MetaTitle;
            model.Description = source.MetaDescription;
            model.PublishedTime = source.CreatedOnUTC;
            model.Type = "article";

            return Task.FromResult(model);
        }
    }
}
