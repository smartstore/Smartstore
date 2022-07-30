using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Net.Http
{
    public static class HttpClientBuilderExtensions
    {
        private readonly static ProductInfoHeaderValue _userAgentHeader = new("Smartstore", SmartstoreVersion.CurrentFullVersion);

        public static IHttpClientBuilder AddSmartstoreUserAgent(this IHttpClientBuilder builder)
        {
            return builder.ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(_userAgentHeader);
            });
        }

        public static IHttpClientBuilder SkipCertificateValidation(this IHttpClientBuilder builder)
        {
            return builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
        }

        /// <summary>
        /// Adds a message handler for propagating cookies from current HTTP request to an outgoing request,
        /// explicitly specifying which cookies to propagate.
        /// </summary>
        /// <param name="cookieNames">A list of specific cookie names to propagate. If null or empty all request cookies will be propagated.</param>
        public static IHttpClientBuilder PropagateCookies(this IHttpClientBuilder builder, params string[] cookieNames)
        {
            return builder.AddHttpMessageHandler(services =>
            {
                return new CookiePropagationMessageHandler(services.GetRequiredService<IHttpContextAccessor>(), cookieNames ?? Array.Empty<string>());
            });
        }

        class CookiePropagationMessageHandler : DelegatingHandler
        {
            private readonly IHttpContextAccessor _httpContextAccessor;
            private readonly string[] _cookieNames;

            public CookiePropagationMessageHandler(IHttpContextAccessor httpContextAccessor, params string[] cookieNames)
            {
                _httpContextAccessor = httpContextAccessor;
                _cookieNames = cookieNames;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var httpRequest = httpContext.Request;

                    if (_cookieNames.Length == 0)
                    {
                        request.Headers.TryAddWithoutValidation("Cookie", httpRequest.Headers.Cookie.ToString());
                    }
                    else
                    {
                        var cookieContainer = new CookieContainer(_cookieNames.Length);

                        foreach (var cookieName in _cookieNames)
                        {
                            if (httpRequest.Cookies.TryGetValue(cookieName, out var cookieValue))
                            {
                                cookieContainer.Add(new Cookie(
                                    cookieName,
                                    cookieValue,
                                    httpRequest.PathBase.Value.NullEmpty(),
                                    httpRequest.Host.Host));
                            }
                        }

                        if (cookieContainer.Count > 0)
                        {
                            var cookieHeader = cookieContainer.GetCookieHeader(new Uri(httpRequest.GetDisplayUrl()));
                            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                        }
                    }
                }

                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
