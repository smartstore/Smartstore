using System.Threading.Tasks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    public partial interface IPaymentService
    {
        /// <summary>
        /// Loads a payment provider by system name.
        /// </summary>
        /// <param name="systemName">System name of the payment provider.</param>
        /// <param name="onlyWhenActive"><c>true</c> to only load an active provider.</param>
        /// <param name="storeId">Filter payment provider by store identifier. 0 to load all.</param>
        /// <returns>Payment provider.</returns>
        Task<Provider<IPaymentMethod>> LoadPaymentMethodBySystemNameAsync(string systemName, bool onlyWhenActive = false, int storeId = 0);
    }
}
