namespace Smartstore.Core.Customers
{
    /// <summary>
    /// Represents the customer name fortatting enumeration.
    /// </summary>
    public enum CustomerNameFormat
    {
        /// <summary>
        /// Show emails
        /// </summary>
        ShowEmail = 1,
        /// <summary>
        /// Show usernames
        /// </summary>
        ShowUsername = 2,
        /// <summary>
        /// Show full names
        /// </summary>
        ShowFullName = 3,
        /// <summary>
        /// Show first name
        /// </summary>
        ShowFirstName = 4,
        /// <summary>
        /// Show shorted name and city
        /// </summary>
        ShowNameAndCity = 5
    }
}
