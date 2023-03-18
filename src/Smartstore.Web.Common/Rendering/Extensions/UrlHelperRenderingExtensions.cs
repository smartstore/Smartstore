using System.Runtime.CompilerServices;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Rendering
{
    public static class UrlHelperRenderingExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Media(this IUrlHelper urlHelper, IImageModel model)
        {
            Guard.NotNull(urlHelper);
            Guard.NotNull(model);

            var mediaService = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IMediaService>();

            return mediaService.GetUrl(model.File, model.ThumbSize ?? 0, model.Host, !model.NoFallback);
        }
    }
}
