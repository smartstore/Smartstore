using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Net.Http
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder ForSafeLocalCall(this IHttpClientBuilder builder)
        {
            return builder;
        }

        public static IHttpClientBuilder SendAuthenticationCookie(this IHttpClientBuilder builder)
        {
            return builder;
        }

        public static IHttpClientBuilder SendVisitorCookie(this IHttpClientBuilder builder)
        {
            return builder;
        }
    }
}
