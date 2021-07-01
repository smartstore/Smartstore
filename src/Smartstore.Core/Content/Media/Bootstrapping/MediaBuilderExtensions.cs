using System;
using Microsoft.AspNetCore.Builder;
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

            app.UseMiddleware<MediaMiddleware>();
            app.UseMiddleware<MediaLegacyMiddleware>();

            return app;
        }
    }
}
