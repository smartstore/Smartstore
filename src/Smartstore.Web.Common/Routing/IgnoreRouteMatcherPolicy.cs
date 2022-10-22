using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace Smartstore.Web.Routing
{
    /// <summary>
    /// Metadata for endpoints that should only be matched for link generation.
    /// </summary>
    public sealed class IgnoreRouteAttribute : Attribute
    {
    }
    
    public class IgnoreRouteMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy, IEndpointComparerPolicy
    {
        public override int Order => int.MinValue + 100;

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            for (var i = 0; i < endpoints.Count; i++)
            {
                var attr = endpoints[i].Metadata.GetMetadata<IgnoreRouteAttribute>();
                if (attr != null)
                {
                    return true;
                }
            }

            return false;
        }

        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!candidates.IsValidCandidate(i))
                {
                    continue;
                }

                var attr = candidates[i].Endpoint.Metadata.GetMetadata<IgnoreRouteAttribute>();
                if (attr == null)
                {
                    continue;
                }

                // The attribute is set. This route should not be matched.
                candidates.SetValidity(i, false);
            }

            return Task.CompletedTask;
        }

        public IComparer<Endpoint> Comparer => new MyTestEndpointComparer();

        private class MyTestEndpointComparer : EndpointMetadataComparer<IgnoreRouteAttribute>
        {
            protected override int CompareMetadata(IgnoreRouteAttribute x, IgnoreRouteAttribute y)
            {
                return base.CompareMetadata(x, y);
            }
        }
    }
}
