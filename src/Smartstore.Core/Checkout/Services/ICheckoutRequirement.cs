#nullable enable

using Smartstore.Http;

namespace Smartstore.Core.Checkout.Services
{
    public partial interface ICheckoutRequirement
    {
        Task<bool> IsFulfilledAsync();
        RouteInfo GetFulfillmentRoute();
    }
}
