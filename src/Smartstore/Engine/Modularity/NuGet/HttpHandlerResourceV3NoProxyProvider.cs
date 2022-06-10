using System.Diagnostics;
using System.Net;
using System.Net.Http;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Smartstore.Engine.Modularity.NuGet
{
    /// <summary>
    /// Replaces <see cref="HttpHandlerResourceV3Provider"/> to fix access problems
    /// to global packages folder in IIS.
    /// </summary>
    internal class HttpHandlerResourceV3NoProxyProvider : ResourceProvider
    {
        public HttpHandlerResourceV3NoProxyProvider()
            : base(typeof(HttpHandlerResource),
                  nameof(HttpHandlerResourceV3NoProxyProvider),
                  nameof(HttpHandlerResourceV3Provider))
        {
            // Add this before original HttpHandlerResourceV3Provider
        }

        public override Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
        {
            Debug.Assert(source.PackageSource.IsHttp, "HTTP handler requested for a non-http source.");

            HttpHandlerResourceV3 curResource = null;

            if (source.PackageSource.IsHttp)
            {
                curResource = CreateResource(source.PackageSource);
            }

            return Task.FromResult(new Tuple<bool, INuGetResource>(curResource != null, curResource));
        }

        private static HttpHandlerResourceV3 CreateResource(PackageSource packageSource)
        {
            var sourceUri = packageSource.SourceUri;

            // Originally this was: ProxyCache.Instance.GetProxy(), but on IIS ProxyCache
            // tries to write a file in 'C:\WINDOWS\system32\config\systemprofile' folder, which fails of course,
            // because IIS_IUSRS has no write permission there and we simply won't require this to happen.
            var proxy = new ProxyCache(NullSettings.Instance, EnvironmentVariableWrapper.Instance).GetProxy(sourceUri);

            // replace the handler with the proxy aware handler
            var clientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
            };

            // Setup http client handler client certificates
            if (packageSource.ClientCertificates != null)
            {
                clientHandler.ClientCertificates.AddRange(packageSource.ClientCertificates.ToArray());
            }

            // HTTP handler pipeline can be injected here, around the client handler
            HttpMessageHandler messageHandler = new ServerWarningLogHandler(clientHandler);

            if (proxy != null)
            {
                messageHandler = new ProxyAuthenticationHandler(clientHandler, HttpHandlerResourceV3.CredentialService?.Value, ProxyCache.Instance);
            }

            var innerHandler = messageHandler;

            messageHandler = new HttpSourceAuthenticationHandler(packageSource, clientHandler, HttpHandlerResourceV3.CredentialService?.Value)
            {
                InnerHandler = innerHandler
            };

            var resource = new HttpHandlerResourceV3(clientHandler, messageHandler);

            return resource;
        }
    }
}
