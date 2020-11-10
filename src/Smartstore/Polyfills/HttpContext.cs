using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace System.Web
{
    // TODO: (core) Remove PolyfillHttpContext later
    public static class HttpContext
    {
        private static IHttpContextAccessor _contextAccessor;

        internal static void Configure(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public static Microsoft.AspNetCore.Http.HttpContext Current
            => _contextAccessor.HttpContext;
    }

    public static class PolyfillHttpContextExtensions
    {
        //public static void AddHttpContextAccessor(this IServiceCollection services)
        //{
        //    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        //}

        public static IApplicationBuilder UsePolyfillHttpContext(this IApplicationBuilder app)
        {
            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            System.Web.HttpContext.Configure(httpContextAccessor);
            return app;
        }
    }
}
