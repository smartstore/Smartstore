using Microsoft.AspNetCore.Builder;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Bootstrapping
{
    public static class MediaBuilderExtensions
    {
        /// <summary>
        /// Maps media middleware as a new branch
        /// </summary>
        public static IApplicationBuilder MapMedia(this IApplicationBuilder app)
        {
            Guard.NotNull(app);

            var publicPath = (app.ApplicationServices.GetService<IMediaStorageConfiguration>()?.PublicPath ?? "media")
                .EnsureStartsWith('/')
                .TrimEnd('/');

            app.Map(publicPath, preserveMatchedPathSegment: true, branch =>
            {
                branch.UseCookiePolicy();
                branch.UseAuthentication();
                branch.UseWorkContext();
                branch.UseRequestLogging();
                
                branch.UseMiddleware<MediaMiddleware>();
                branch.UseMiddleware<MediaLegacyMiddleware>();
            });

            return app;
        }
    }
}
