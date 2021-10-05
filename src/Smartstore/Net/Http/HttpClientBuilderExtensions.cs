using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Smartstore.Engine;
using Smartstore.Threading;

namespace Smartstore.Net.Http
{
    //public static class HttpWebRequestExtensions
    //{
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public static void SetAuthenticationCookie(this HttpWebRequest webRequest, HttpRequest httpRequest)
    //    {
    //        CopyCookie(webRequest, httpRequest, CookieNames.Identity);
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public static void SetVisitorCookie(this HttpWebRequest webRequest, HttpRequest httpRequest)
    //    {
    //        CopyCookie(webRequest, httpRequest, CookieNames.Visitor);
    //    }

    //    private static void CopyCookie(HttpWebRequest webRequest, HttpRequest sourceHttpRequest, string cookieName)
    //    {
    //        Guard.NotNull(webRequest, nameof(webRequest));
    //        Guard.NotNull(sourceHttpRequest, nameof(sourceHttpRequest));
    //        Guard.NotEmpty(cookieName, nameof(cookieName));

    //        var sourceCookie = sourceHttpRequest.Cookies[cookieName];
    //        if (sourceCookie == null)
    //            return;

    //        var sendCookie = new Cookie(
    //            cookieName,
    //            sourceCookie,
    //            sourceHttpRequest.PathBase.Value.NullEmpty(),
    //            sourceHttpRequest.Host.Host);

    //        if (webRequest.CookieContainer == null)
    //        {
    //            webRequest.CookieContainer = new CookieContainer();
    //        }

    //        webRequest.CookieContainer.Add(sendCookie);
    //    }
    //}

    public static class HttpClientBuilderExtensions
    {
        private static readonly AsyncLock _asyncLock = new();
        private static readonly ConcurrentDictionary<int, string> _safeLocalHostNames = new();

        public static IHttpClientBuilder ForSafeLocalCall(this IHttpClientBuilder builder)
        {
            builder
                .SkipCertificateValidation()
                .ConfigureHttpClient(client => 
                {
                    //client.b
                });


            return builder;
        }

        public static IHttpClientBuilder SkipCertificateValidation(this IHttpClientBuilder builder)
        {
            return builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler 
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
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
