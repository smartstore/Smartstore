using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Common.Mappers;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public static partial class ProductMappingExtensions
    {
        public static async Task<MetaPropertiesModel> MapMetaPropertiesAsync(this ProductDetailsModel model, dynamic parameters = null)
        {
            var mapper = MapperFactory.GetMapper<ProductDetailsModel, MetaPropertiesModel>();
            var result = new MetaPropertiesModel();
            await mapper.MapAsync(model, result, parameters);
            return result;
        }
    }

    public class ProductMetaPropertyMapper : MetaPropertiesMapperBase<ProductDetailsModel>
    {
        private readonly IUrlHelper _urlHelper;

        public ProductMetaPropertyMapper(IHttpContextAccessor httpContextAccessor, IUrlHelper urlHelper)
            : base(httpContextAccessor)
        {
            _urlHelper = urlHelper;
        }

        protected override MediaFileInfo GetMediaFile(ProductDetailsModel from)
        {
            return from.MediaGalleryModel.Files?.ElementAtOrDefault(from.MediaGalleryModel.GalleryStartIndex);
        }

        protected override Task MapCoreAsync(ProductDetailsModel source, MetaPropertiesModel model, dynamic parameters = null)
        {
            model.Url = _urlHelper.RouteUrl("Product", new { SeName = source.SeName }, HttpContext.Request.Scheme);
            model.Title = source.Name.Value;
            model.Type = "product";

            var shortDescription = source.ShortDescription.Value.HasValue() ? source.ShortDescription : source.MetaDescription;
            if (shortDescription.Value.HasValue())
            {
                model.Description = shortDescription.Value;
            }

            return Task.FromResult(model);
        }
    }
}
