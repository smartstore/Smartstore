using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Media.Imaging;

namespace Smartstore.Core.Content.Media
{
    public static class MediaUrlHelperExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Media(this IUrlHelper urlHelper, MediaFileInfo file, int thumbnailSize = 0, string host = null, bool doFallback = true)
        {
            var mediaService = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IMediaService>();
            return mediaService.GetUrl(file, thumbnailSize, host, doFallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Media(this IUrlHelper urlHelper, MediaFileInfo file, ProcessImageQuery query, string host = null, bool doFallback = true)
        {
            var mediaService = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IMediaService>();
            return mediaService.GetUrl(file, query, host, doFallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> MediaAsync(this IUrlHelper urlHelper, int? fileId, int thumbnailSize = 0, string host = null, bool doFallback = true)
        {
            var mediaService = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IMediaService>();
            return mediaService.GetUrlAsync(fileId, thumbnailSize, host, doFallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> MediaAsync(this IUrlHelper urlHelper, int? fileId, ProcessImageQuery query, string host = null, bool doFallback = true)
        {
            var mediaService = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IMediaService>();
            return mediaService.GetUrlAsync(fileId, query, host, doFallback);
        }
    }
}