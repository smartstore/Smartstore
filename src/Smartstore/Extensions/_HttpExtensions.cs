using Microsoft.AspNetCore.Http;
using SmartStore;

namespace Smartstore
{
    public static class HttpExtensions
    {
        const string CacheRegionName = "Smartstore:";

        public static string BuildScopedKey(this IMemoryCache cache, string key)
            => return key.HasValue()? CacheRegionName + key : null;

        public static PostedFileResult ToPostedFileResult(this HttpRequest httpRequest, int index = 0)
        {
            if (httpRequest != null && httpRequest.Form.Files.Count > index)
            {
                return httpRequest.Form.Files[index].ToPostedFileResult();
            }

            return null;
        }

        public static PostedFileResult ToPostedFileResult(this IFormFile httpFile)
        {
            if (httpFile != null && httpFile.Length > 0)
            {
                return new PostedFileResult(httpFile);
            }

            return null;
        }

        public static PostedFileResult ToPostedFileResult(this IFormCollection httpFileCollection)
        {
            if (httpFileCollection != null && httpFileCollection.Count > 0)
            {
                return new PostedFileResult(httpFileCollection.Files[0]);
            }

            return null;
        }
    }
}
//		const string CacheRegionName = "SmartStoreNET:";
//		const string RememberPathKey = "AppRelativeCurrentExecutionFilePath.Original";

//		private static readonly List<Tuple<string, string>> _sslHeaders = new List<Tuple<string, string>>
//		{
//			new Tuple<string, string>("HTTP_CLUSTER_HTTPS", "on"),
//            new Tuple<string, string>("HTTP_X_FORWARDED_PROTO", "https"),
//            new Tuple<string, string>("X-Forwarded-Proto", "https"),
//			new Tuple<string, string>("x-arr-ssl", null),
//			new Tuple<string, string>("X-Forwarded-Protocol", "https"),
//			new Tuple<string, string>("X-Forwarded-Ssl", "on"),
//			new Tuple<string, string>("X-Url-Scheme", "https")
//		};

//		/// <summary>
//		/// Returns wether the specified url is local to the host or not
//		/// </summary>
//		/// <param name="request"></param>
//		/// <param name="url"></param>
//		/// <returns></returns>
//		public static bool IsAppLocalUrl(this HttpRequestBase request, string url)
//		{
//			if (string.IsNullOrWhiteSpace(url))
//			{
//				return false;
//			}

//			url = url.Trim();

//			if (url.StartsWith("~/"))
//			{
//				return true;
//			}

//			if (url.StartsWith("//") || url.StartsWith("/\\"))
//			{
//				return false;
//			}

//			// At this point when the url starts with "/" it is local
//			if (url.StartsWith("/"))
//			{
//				return true;
//			}

//			// At this point, check for a fully qualified url
//			try
//			{
//				var uri = new Uri(url);

//				if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
//				{
//					return false;
//				}

//				if (uri.Authority.Equals(request.Headers["Host"], StringComparison.OrdinalIgnoreCase))
//				{
//					return true;
//				}

//				// Finally, check the base url from the settings
//				var storeContext = EngineContext.Current.Resolve<IStoreContext>();
//				if (storeContext != null)
//				{
//					var baseUrl = storeContext.CurrentStore.Url;
//					if (baseUrl.HasValue())
//					{
//						if (uri.Authority.Equals(new Uri(baseUrl).Authority, StringComparison.OrdinalIgnoreCase))
//						{
//							return true;
//						}
//					}
//				}

//				return false;
//			}
//			catch
//			{
//				// mall-formed url e.g, "abcdef"
//				return false;
//			}
//		}

//		[MethodImpl(MethodImplOptions.AggressiveInlining)]
//		public static void RememberAppRelativePath(this HttpContextBase httpContext)
//		{
//			httpContext.Items[RememberPathKey] = httpContext.Request.AppRelativeCurrentExecutionFilePath;
//		}

//		[MethodImpl(MethodImplOptions.AggressiveInlining)]
//		public static string GetOriginalAppRelativePath(this HttpContextBase httpContext)
//		{
//			return GetItem<string>(httpContext, RememberPathKey, forceCreation: false) ?? httpContext.Request.AppRelativeCurrentExecutionFilePath;
//		}

//		public static ControllerContext GetRootControllerContext(this ControllerContext controllerContext)
//        {
//            Guard.NotNull(controllerContext, nameof(controllerContext));

//            var ctx = controllerContext;

//            while (ctx.ParentActionViewContext != null)
//            {
//                ctx = ctx.ParentActionViewContext;
//            }

//            return ctx;
//        }

//        public static bool IsBareBonePage(this ControllerContext controllerContext)
//        {
//            var ctx = controllerContext.GetRootControllerContext();

//            if (ctx is ViewContext viewContext)
//            {
//                // IsPopUp or Framed
//                if (viewContext.ViewBag.IsPopup == true || viewContext.ViewBag.Framed == true)
//                {
//                    return true;
//                }
//            }

//            return false;
//        }
//	}
//}
