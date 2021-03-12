using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class ShopLogoViewComponent : SmartViewComponent
    {
        private readonly Lazy<IMediaService> _mediaService;

        public ShopLogoViewComponent(Lazy<IMediaService> mediaService)
        {
            _mediaService = mediaService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // TODO: (mh) (core) caching.

            var store = Services.StoreContext.CurrentStore;
            var logo = await _mediaService.Value.GetFileByIdAsync(store.LogoMediaFileId, MediaLoadFlags.AsNoTracking);

            var model = new ShopLogoModel
            {
                LogoUploaded = logo != null,
                LogoTitle = store.Name
            };

            if (logo != null)
            {
                model.LogoUrl = await _mediaService.Value.GetUrlAsync(logo.Id, 0, null, false);
                model.LogoWidth = logo.Size.Width;
                model.LogoHeight = logo.Size.Height;
            }

            return View(model);
        }
    }
}
