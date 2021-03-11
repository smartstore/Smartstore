using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class FaviconViewComponent : SmartViewComponent
    {
        private readonly IMediaService _mediaService;

        public FaviconViewComponent(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // TODO: (core) Find a way to cache FaviconModel.
            var store = Services.StoreContext.CurrentStore;

            var model = new FaviconModel
            {
                FavIcon = await _mediaService.GetFileByIdAsync(Convert.ToInt32(store.FavIconMediaFileId), MediaLoadFlags.AsNoTracking),
                PngIcon = await _mediaService.GetFileByIdAsync(Convert.ToInt32(store.PngIconMediaFileId), MediaLoadFlags.AsNoTracking),
                AppleTouchIcon = await _mediaService.GetFileByIdAsync(Convert.ToInt32(store.AppleTouchIconMediaFileId), MediaLoadFlags.AsNoTracking),
                MsTileIcon = await _mediaService.GetFileByIdAsync(Convert.ToInt32(store.MsTileImageMediaFileId), MediaLoadFlags.AsNoTracking),
                MsTileColor = store.MsTileColor
            };

            return View(model);
        }
    }
}
