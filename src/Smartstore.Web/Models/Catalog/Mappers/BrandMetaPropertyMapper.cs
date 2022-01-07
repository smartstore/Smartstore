using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Common.Mappers;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public static partial class BrandMappingExtensions
    {
        public static async Task<MetaPropertiesModel> MapMetaPropertiesAsync(this BrandModel model, dynamic parameters = null)
        {
            var mapper = MapperFactory.GetMapper<BrandModel, MetaPropertiesModel>();
            var result = new MetaPropertiesModel();
            await mapper.MapAsync(model, result, parameters);
            return result;
        }
    }

    public class BrandMetaPropertyMapper : MetaPropertiesMapperBase<BrandModel>
    {
        private readonly IUrlHelper _urlHelper;

        public BrandMetaPropertyMapper(IHttpContextAccessor httpContextAccessor, IUrlHelper urlHelper)
            : base(httpContextAccessor)
        {
            _urlHelper = urlHelper;
        }

        protected override MediaFileInfo GetMediaFile(BrandModel from)
        {
            return from.Image?.File;
        }

        protected override Task MapCoreAsync(BrandModel source, MetaPropertiesModel model, dynamic parameters = null)
        {
            model.Url = _urlHelper.RouteUrl("Manufacturer", new { source.SeName }, HttpContext.Request.Scheme);
            model.Title = source.Name.Value;
            model.Description = source.MetaDescription?.Value;
            model.Type = "product";

            return Task.FromResult(model);
        }
    }
}
