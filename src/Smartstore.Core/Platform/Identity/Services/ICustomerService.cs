using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Web;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Customer service interface
    /// </summary>
    public partial interface ICustomerService
    {
        /// <summary>
        /// Creates and inserts a guest customer account.
        /// </summary>
        /// <param name="customerGuid">The customer GUID. Pass <c>null</c> to create a random one.</param>
        /// <returns>Customer</returns>
        Task<Customer> CreateGuestCustomerAsync(Guid? customerGuid = null);

        /// <summary>
        /// Tries to find a guest/anonymous customer record by client ident. This method should be called when an
        /// anonymous visitor rejects cookies and therefore cannot be identified automatically.
        /// </summary>
        /// <param name="clientIdent">
        /// The client ident string, which is a hashed combination of client IP address and user agent. 
        /// Call <see cref="IWebHelper.GetClientIdent()"/> to obtain an ident string, or pass <c>null</c> to let this method obtain it automatically.</param>
        /// <param name="maxAgeSeconds">The max age of the newly created guest customer record. The shorter, the better (default is 1 min.)</param>
        /// <returns>The identified customer or <c>null</c></returns>
        Task<Customer> FindGuestCustomerByClientIdentAsync(string clientIdent = null, int maxAgeSeconds = 60);

        /// <summary>
        /// Deletes guest customer records including generic attributes.
        /// </summary>
        /// <param name="registrationFrom">Customer registration from. <c>null</c> to ignore.</param>
        /// <param name="registrationTo">Customer registration to. <c>null</c> to ignore.</param>
        /// <param name="onlyWithoutShoppingCart">A value indicating whether to delete only customers without shopping cart.</param>
        /// <returns>Number of deleted guest customers.</returns>
        Task<int> DeleteGuestCustomersAsync(
            DateTime? registrationFrom,
            DateTime? registrationTo,
            bool onlyWithoutShoppingCart,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Gets customer by system name.
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <param name="tracked">Whether to load entity tracked. Non-tracking load will be cached.</param>
        /// <returns>Found customer</returns>
        Customer GetCustomerBySystemName(string systemName, bool tracked = true);

        /// <summary>
        /// Gets customer by system name.
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <param name="tracked">Whether to load entity tracked. Non-tracking load will be cached.</param>
        /// <returns>Found customer</returns>
        Task<Customer> GetCustomerBySystemNameAsync(string systemName, bool tracked = true);

        /// <summary>
        /// Gets the currently authenticated customer.
        /// </summary>
        /// <returns>Authenticated customer.</returns>
        Task<Customer> GetAuthenticatedCustomerAsync();

        /// <summary>
        /// Gets a customer role by system name.
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <param name="tracked">Whether to load entity tracked. Non-tracking load will be cached.</param>
        /// <returns>Found customer</returns>
        CustomerRole GetRoleBySystemName(string systemName, bool tracked = true);

        /// <summary>
        /// Gets a customer role by system name.
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <param name="tracked">Whether to load entity tracked. Non-tracking load will be cached.</param>
        /// <returns>Found customer</returns>
        Task<CustomerRole> GetRoleBySystemNameAsync(string systemName, bool tracked = true);

        /// <summary>
        /// Applies reward points for a product review. The caller is responsible for database commit.
        /// </summary>
        /// <param name="customer">Customer.</param>
        /// <param name="product">Product.</param>
        /// <param name="add"><c>True</c> to add reward points. <c>False</c> to remove reward points.</param>
        void ApplyRewardPointsForProductReview(Customer customer, Product product, bool add);
    }
}
