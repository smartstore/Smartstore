#nullable enable
using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Seo.Routing
{
    public interface IUrlPolicyFeature
    {
        UrlPolicy? UrlPolicy { get; set; }
    }

    public static class UrlPolicyHttpContextExtensions
    {
        private class UrlPolicyFeature : IUrlPolicyFeature
        {
            public UrlPolicy? UrlPolicy { get; set; }
        }

        /// <summary>
        /// Extension method for getting the <see cref="UrlPolicy"/> for the current request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <returns>The <see cref="UrlPolicy"/>.</returns>
        public static UrlPolicy? GetUrlPolicy(this HttpContext context)
        {
            Guard.NotNull(context, nameof(context));

            return context.Features.Get<IUrlPolicyFeature>()?.UrlPolicy;
        }

        /// <summary>
        /// Extension method for setting the <see cref="UrlPolicy"/> for the current request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> context.</param>
        /// <param name="policy">The <see cref="UrlPolicy"/>.</param>
        public static void SetUrlPolicy(this HttpContext context, UrlPolicy? policy)
        {
            Guard.NotNull(context, nameof(context));

            var feature = context.Features.Get<IUrlPolicyFeature>();

            if (policy != null)
            {
                if (feature == null)
                {
                    feature = new UrlPolicyFeature();
                    context.Features.Set(feature);
                }

                feature.UrlPolicy = policy;
            }
            else
            {
                if (feature == null)
                {
                    // No policy to set and no feature on context. Do nothing
                    return;
                }

                feature.UrlPolicy = null;
            }
        }
    }
}
