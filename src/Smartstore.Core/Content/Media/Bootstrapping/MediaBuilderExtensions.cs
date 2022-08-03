using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Bootstrapping
{
    public static class MediaBuilderExtensions
    {
        /// <summary>
        /// Uses media middleware
        /// </summary>
        public static IApplicationBuilder UseMedia(this IApplicationBuilder app)
        {
            Guard.NotNull(app, nameof(app));

            app.UseWhen(IsGetOrHead, x =>
            {
                x.UseMiddleware<MediaMiddleware>();
                x.UseMiddleware<MediaLegacyMiddleware>();
            });

            return app;
        }

        static bool IsGetOrHead(HttpContext ctx) => ctx.Request.Method == HttpMethods.Get || ctx.Request.Method == HttpMethods.Head;
    }
}
