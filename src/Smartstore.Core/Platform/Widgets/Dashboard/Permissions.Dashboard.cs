namespace Smartstore.Core.Security;

public static partial class Permissions
{
    /// <summary>
    /// Contains dashboard permissions.
    /// </summary>
    public static partial class Dashboard
    {
        /// <summary>
        /// Grants all dashboard capabilities.
        /// </summary>
        public const string Self = "dashboard";

        /// <summary>
        /// Controls access to widget data independently of administration pages.
        /// </summary>
        public static class Widgets
        {
            /// <summary>
            /// Grants access to all dashboard widgets.
            /// </summary>
            public const string Self = "dashboard.widgets";

            /// <summary>
            /// Groups sales dashboard widgets.
            /// </summary>
            public static class Sales
            {
                /// <summary>
                /// Grants access to all widgets in this group.
                /// </summary>
                public const string Self = "dashboard.widgets.sales";

                /// <summary>
                /// Allows displaying the IncompleteOrders widget and its data.
                /// </summary>
                public const string IncompleteOrders = Self + ".incompleteorders";

                /// <summary>
                /// Allows displaying the Orders widget and its data.
                /// </summary>
                public const string Orders = Self + ".orders";

                /// <summary>
                /// Allows displaying the Bestsellers widget and its data.
                /// </summary>
                public const string Bestsellers = Self + ".bestsellers";

                /// <summary>
                /// Allows displaying the LatestOrders widget and its data.
                /// </summary>
                public const string LatestOrders = Self + ".latestorders";
            }

            /// <summary>
            /// Groups customers dashboard widgets.
            /// </summary>
            public static class Customers
            {
                /// <summary>
                /// Grants access to all widgets in this group.
                /// </summary>
                public const string Self = "dashboard.widgets.customers";

                /// <summary>
                /// Allows displaying the RegisteredCustomers widget and its data.
                /// </summary>
                public const string RegisteredCustomers = Self + ".registeredcustomers";

                /// <summary>
                /// Allows displaying the TopCustomers widget and its data.
                /// </summary>
                public const string TopCustomers = Self + ".topcustomers";
            }

            /// <summary>
            /// Groups system dashboard widgets.
            /// </summary>
            public static class System
            {
                /// <summary>
                /// Grants access to all widgets in this group.
                /// </summary>
                public const string Self = "dashboard.widgets.system";

                /// <summary>
                /// Allows displaying the StoreReport widget and its data.
                /// </summary>
                public const string StoreReport = Self + ".storereport";

                /// <summary>
                /// Allows displaying the NewsFeed widget and its data.
                /// </summary>
                public const string NewsFeed = Self + ".newsfeed";
            }
        }
    }
}
