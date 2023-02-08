namespace Smartstore.Core.Identity
{
    /// <summary>
    /// An event message that will be published before guest customers are deleted.
    /// </summary>
    public class GuestCustomerDeletingEvent
    {
        public GuestCustomerDeletingEvent(
            DateTime? registrationFrom,
            DateTime? registrationTo,
            bool onlyWithoutShoppingCart)
        {
            RegistrationFrom = registrationFrom;
            RegistrationTo = registrationTo;
            OnlyWithoutShoppingCart = onlyWithoutShoppingCart;
        }

        /// <summary>
        /// Customer registration from. <c>null</c> to ignore.
        /// Already included in <see cref="Query"/>.
        /// </summary>
        public DateTime? RegistrationFrom { get; }

        /// <summary>
        /// Customer registration to. <c>null</c> to ignore.
        /// Already included in <see cref="Query"/>.
        /// </summary>
        public DateTime? RegistrationTo { get; }

        /// <summary>
        /// A value indicating whether to delete only customers without shopping cart.
        /// Already included in <see cref="Query"/>.
        /// </summary>
        public bool OnlyWithoutShoppingCart { get; }

        /// <summary>
        /// Gets or sets the query used for deleting guest customers.
        /// </summary>
        public IQueryable<Customer> Query { get; set; }
    }
}
