using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;

namespace Smartstore.Admin.Models.Catalog
{
    internal static partial class ProductOverviewMappingExtensions
    {
        public static async Task<IList<ProductOverviewModel>> MapAsync(this IEnumerable<Product> entities,
            IMediaService mediaService,
            bool includeFiles = true)
        {
            Guard.NotNull(entities, nameof(entities));

            dynamic parameters = new ExpandoObject();

            if (includeFiles)
            {
                var fileIds = entities
                    .Select(x => x.MainPictureId ?? 0)
                    .Where(x => x != 0)
                    .Distinct()
                    .ToArray();

                parameters.Files = (await mediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);
            }
            else
            {
                parameters.Files = null;
            }

            var models = await entities
                .SelectAsync(async x =>
                {
                    var model = new ProductOverviewModel();
                    await MapperFactory.MapAsync(x, model, parameters);
                    return model;
                })
                .AsyncToList();

            return models;
        }

        public static async Task<ProductOverviewModel> MapAsync(this Product entity,
            Dictionary<int, MediaFileInfo> files)
        {
            var model = new ProductOverviewModel();
            await entity.MapAsync(model, files);

            return model;
        }

        public static async Task MapAsync(this Product entity,
            ProductOverviewModel model,
            Dictionary<int, MediaFileInfo> files)
        {
            dynamic parameters = new ExpandoObject();
            parameters.Files = files;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    internal class ProductOverviewMapper : Mapper<Product, ProductOverviewModel>
    {
        private readonly ICommonServices _services;
        private readonly IUrlHelper _urlHelper;
        private readonly MediaSettings _mediaSettings;

        public ProductOverviewMapper(
            ICommonServices services, 
            IUrlHelper urlHelper,
            MediaSettings mediaSettings)
        {
            _services = services;
            _urlHelper = urlHelper;
            _mediaSettings = mediaSettings;
        }

        protected override void Map(Product from, ProductOverviewModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override Task MapAsync(Product from, ProductOverviewModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var files = parameters.Files as Dictionary<int, MediaFileInfo>;
            var file = files?.Get(from.MainPictureId ?? 0);

            MiniMapper.Map(from, to);

            to.ProductTypeName = from.GetProductTypeLabel(_services.Localization);
            to.EditUrl = _urlHelper.Action("Edit", "Product", new { id = from.Id, area = "Admin" });
            to.UpdatedOn = _services.DateTimeHelper.ConvertToUserTime(from.UpdatedOnUtc, DateTimeKind.Utc);
            to.CreatedOn = _services.DateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc);
            to.CopyProductModel.Name = _services.Localization.GetResource("Admin.Common.CopyOf").FormatInvariant(from.Name);
            to.NoThumb = file == null;

            // TODO: (core) Use IImageModel
            to.PictureThumbnailUrl = file != null
                ? _services.MediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize)
                : null;

            return Task.CompletedTask;
        }
    }
}
