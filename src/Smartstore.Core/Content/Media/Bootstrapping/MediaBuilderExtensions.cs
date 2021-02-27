using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Bootstrapping
{
    public static class MediaBuilderExtensions
    {
        /// <summary>
        /// Maps media endpoints
        /// </summary>
        public static IEndpointConventionBuilder MapMedia(this IEndpointRouteBuilder endpoints)
        {
            Guard.NotNull(endpoints, nameof(endpoints));

            var mediaStorageConfiguration = endpoints.ServiceProvider.GetRequiredService<IMediaStorageConfiguration>();
            var mediaPublicPath = mediaStorageConfiguration.PublicPath;

            var mediaMiddleware = endpoints.CreateApplicationBuilder()
               .UseMiddleware<MediaMiddleware>()
               .Build();

            var mediaLegacyMiddleware = endpoints.CreateApplicationBuilder()
               .UseMiddleware<MediaLegacyMiddleware>()
               .Build();

            return new CompositeEndpointConventionBuilder(new[]
            {
                // Match main URL pattern /{pub}/{id}/{path}[?{query}], e.g. '/media/234/{album}/myproduct.png?size=250'
                endpoints.Map(mediaPublicPath + "{id:int}/{**path}", mediaMiddleware)
                    .WithMetadata(new HttpMethodMetadata(new[] { "GET", "HEAD" }))
                    .WithDisplayName("Media"),

                // ----------------------------
                // Legacy routes
                // ----------------------------

                // Match legacy URL pattern /{pub}/uploaded/{path}[?{query}], e.g. '/media/uploaded/subfolder/image.png' 
                endpoints.Map(mediaPublicPath + "uploaded/{**path}", mediaLegacyMiddleware)
                    .WithMetadata(new HttpMethodMetadata(new[] { "GET", "HEAD" }))
                    .WithDisplayName("Media Legacy 1 (uploaded/...)"),

                // Match legacy URL pattern /{pub}/{tenant=default}/uploaded/{path}[?{query}], e.g. '/media/default/uploaded/subfolder/image.png' 
                endpoints.Map(mediaPublicPath + "{tenant:regex(^default$)}/uploaded/{**path}", mediaLegacyMiddleware)
                    .WithMetadata(new HttpMethodMetadata(new[] { "GET", "HEAD" }))
                    .WithDisplayName("Media Legacy 2 ({tenant}/uploaded/...)"),

                // Match legacy URL pattern /{pub}/image/{id}/{path}[?{query}], e.g. '/media/image/234/myproduct.png?size=250' 
                endpoints.Map(mediaPublicPath + "image/{id:int}/{**path}", mediaLegacyMiddleware)
                    .WithMetadata(new HttpMethodMetadata(new[] { "GET", "HEAD" }))
                    .WithDisplayName("Media Legacy 3 (image/{id}/...)"),
            });
        }
    }
}
