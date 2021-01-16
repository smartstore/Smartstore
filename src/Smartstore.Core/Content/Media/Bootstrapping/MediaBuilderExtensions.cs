using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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

            var pipeline = endpoints.CreateApplicationBuilder()
               .UseMiddleware<MediaMiddleware>()
               .Build();

            return endpoints.Map("media/{id:int}/{**path}", pipeline)
                .WithMetadata(new HttpMethodMetadata(new[] { "GET", "HEAD" }))
                .WithDisplayName("Media");
        }
    }
}
