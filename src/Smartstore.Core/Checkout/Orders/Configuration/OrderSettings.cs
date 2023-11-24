using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Checkout.Orders
{
    public class OrderSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether customer can make a re-order.
        /// </summary>
        public bool IsReOrderAllowed { get; set; } = true;

        /// <summary>
        /// Gets or sets a minimum order total amount.
        /// </summary>
        public decimal? OrderTotalMinimum { get; set; }

        /// <summary>
        /// Gets or sets a maximum order total amount
        /// </summary>
        public decimal? OrderTotalMaximum { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how customer group restrictions are applied with each other.
        /// <c>true</c>: lowest possible total amount span gets applied. <c>false</c>: highest possible total amount span gets applied.
        /// </summary>
        public bool MultipleOrderTotalRestrictionsExpandRange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to allow anonymous checkout.
        /// </summary>
        public bool AnonymousCheckoutAllowed { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable "Terms of service".
        /// </summary>
        public bool TermsOfServiceEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether "Order completed" page should be skipped-
        /// </summary>
        public bool DisableOrderCompletedPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable return requests.
        /// </summary>
        public bool ReturnRequestsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a list of return request reasons.
        /// </summary>
        [LocalizedProperty]
        public string ReturnRequestReasons { get; set; } = "Received Wrong Product,Wrong Product Ordered,There Was A Problem With The Product";

        /// <summary>
        /// Gets or sets a list of return request actions.
        /// </summary>
        [LocalizedProperty]
        public string ReturnRequestActions { get; set; } = "Repair,Replacement,Store Credit";

        /// <summary>
        /// Gets or sets a number of days that the return request link will be available for customers after placing an order.
        /// </summary>
        public int NumberOfDaysReturnRequestAvailable { get; set; } = 365;

        /// <summary>
        /// Gets or sets the order status when gift cards are activated.
        /// </summary>
        public int GiftCards_Activated_OrderStatusId { get; set; }

        /// <summary>
        /// Gets or sets the order status when gift cards are deactivated.
        /// </summary>
        public int GiftCards_Deactivated_OrderStatusId { get; set; }

        /// <summary>
        /// Gets or sets an order placement interval in seconds (prevent 2 orders being placed within an X seconds time frame).
        /// </summary>
        public int MinimumOrderPlacementInterval { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether to display all orders of all stores to a customer.
        /// </summary>
        public bool DisplayOrdersOfAllStores { get; set; }

        /// <summary>
        /// Gets or sets the page size of the order list.
        /// </summary>
        public int OrderListPageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the page size of the recurring payment list.
        /// </summary>
        public int RecurringPaymentListPageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum order age in days up to which to create and send messages.
        /// Set to 0 to always send messages.
        /// </summary>
        public int MaxMessageOrderAgeInDays { get; set; } = 180;
    }
}
