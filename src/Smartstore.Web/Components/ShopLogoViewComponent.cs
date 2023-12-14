using Smartstore.Core.Content.Media;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class ShopLogoViewComponent : SmartViewComponent
    {
        private readonly IMediaService _mediaService;

        public ShopLogoViewComponent(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var store = Services.StoreContext.CurrentStore;
            var cacheKey = ModelCacheInvalidator.STORE_LOGO_MODEL_KEY.FormatInvariant(store.Id, Request.Scheme, Request.Host);
            
            var model = await Services.Cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(4));

                var logo = await _mediaService.GetFileByIdAsync(store.LogoMediaFileId, MediaLoadFlags.AsNoTracking);

                var model = new ShopLogoModel
                {
                    LogoUploaded = logo != null,
                    LogoTitle = store.Name
                };

                if (logo != null)
                {
                    model.LogoUrl = _mediaService.GetUrl(logo, 0, null, false);
                    model.LogoWidth = logo.Size.Width;
                    model.LogoHeight = logo.Size.Height;
                }

                return model;
            });

            return View(model);
        }
    }
}
