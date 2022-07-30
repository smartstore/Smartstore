using System.Runtime.CompilerServices;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;

namespace Smartstore.Web.Rendering
{
    public static class UrlHelperRenderingExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Media(this IUrlHelper urlHelper, IImageModel model)
        {
            Guard.NotNull(urlHelper, nameof(urlHelper));
            Guard.NotNull(model, nameof(model));

            var urlGenerator = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IMediaUrlGenerator>();

            ProcessImageQuery query = model.ThumbSize > 0
                ? new ProcessImageQuery { MaxSize = model.ThumbSize.Value }
                : null;

            return urlGenerator.GenerateUrl(model.File, query, model.Host, !model.NoFallback);
        }
    }
}
