using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Common.Mappers;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public static partial class CategoryMappingExtensions
    {
        public static async Task<MetaPropertiesModel> MapMetaPropertiesAsync(this CategoryModel model, dynamic parameters = null)
        {
            var mapper = MapperFactory.GetMapper<CategoryModel, MetaPropertiesModel>();
            var result = new MetaPropertiesModel();
            await mapper.MapAsync(model, result, parameters);
            return result;
        }
    }

    public class CategoryMetaPropertyMapper : MetaPropertiesMapperBase<CategoryModel>
    {
        private readonly IUrlHelper _urlHelper;
        private readonly IMediaService _mediaService;

        public CategoryMetaPropertyMapper(IHttpContextAccessor httpContextAccessor, IUrlHelper urlHelper, IMediaService mediaService)
            : base(httpContextAccessor)
        {
            _urlHelper = urlHelper;
            _mediaService = mediaService;
        }

        protected override MediaFileInfo GetMediaFile(CategoryModel from)
        {
            return from.Image?.File == null ? null : _mediaService.ConvertMediaFile(from.Image?.File);
        }

        protected override Task MapCoreAsync(CategoryModel source, MetaPropertiesModel model, dynamic parameters = null)
        {
            model.Url = _urlHelper.RouteUrl("Category", new { source.SeName }, HttpContext.Request.Scheme);
            model.Title = source.Name.Value;
            model.Description = source.MetaDescription?.Value;
            model.Type = "product";

            return Task.FromResult(model);
        }
    }
}
