namespace Smartstore.Core.Identity
{
    public enum PasswordFormat
    {
        Clear = 0,
        Hashed = 1,
        Encrypted = 2
    }

    /// <summary>
    /// Represents the customer login type.
    /// </summary>
    public enum CustomerLoginType
    {
        /// <summary>
        /// The username will be used to login.
        /// </summary>
        Username = 10,

        /// <summary>
        /// The email will be used to login.
        /// </summary>
        Email = 20,

        /// <summary>
        /// The username or the email address can be used to login.
        /// </summary>
        UsernameOrEmail = 30
    }

    /// <summary>
    /// Represents the customer name fortatting enumeration.
    /// </summary>
    /// <remarks>
    /// Backward compat: don't singularize enum values.
    /// </remarks>
    public enum CustomerNameFormat
    {
        /// <summary>
        /// Show emails
        /// </summary>
        ShowEmails = 1,

        /// <summary>
        /// Show usernames
        /// </summary>
        ShowUsernames = 2,

        /// <summary>
        /// Show full names
        /// </summary>
        ShowFullNames = 3,

        /// <summary>
        /// Show first name
        /// </summary>
        ShowFirstName = 4,

        /// <summary>
        /// Show shorted name and city
        /// </summary>
        ShowNameAndCity = 5
    }

    /// <summary>
    /// Represents the customer number method.
    /// </summary>
    public enum CustomerNumberMethod
    {
        /// <summary>
        /// No customer number will be saved.
        /// </summary>
        Disabled = 10,

        /// <summary>
        /// Customer numbers can be saved.
        /// </summary>
        Enabled = 20,

        /// <summary>
        /// Customer numbers will automatically be set when new customers are created.
        /// </summary>
        AutomaticallySet = 30
    }

    /// <summary>
    /// Represents the customer visibility in the frontend.
    /// </summary>
    public enum CustomerNumberVisibility
    {
        /// <summary>
        /// Customer number won't be displayed in the frontend.
        /// </summary>
        None = 10,

        /// <summary>
        /// Customer number will be displayed in the frontend.
        /// </summary>
        Display = 20,

        /// <summary>
        /// A customer can enter his own number if customer number wasn't saved yet.
        /// </summary>
        EditableIfEmpty = 30,

        /// <summary>
        /// A customer can enter his own number and alter it.
        /// </summary>
        Editable = 40
    }

    /// <summary>
    /// Represents the customer registration type fortatting enumeration.
    /// </summary>
    public enum UserRegistrationType
    {
        /// <summary>
        /// Standard account creation.
        /// </summary>
        Standard = 1,

        /// <summary>
        /// Email validation is required after registration.
        /// </summary>
        EmailValidation = 2,

        /// <summary>
        /// A customer should be approved by administrator.
        /// </summary>
        AdminApproval = 3,

        /// <summary>
        /// Registration is disabled.
        /// </summary>
        Disabled = 4
    }

    /// <summary>
    /// Represents the reason for creating a wallet history entry.
    /// </summary>
    public enum WalletPostingReason
    {
        /// <summary>
        /// Any administration reason.
        /// </summary>
        Admin = 0,

        /// <summary>
        /// The customer has purchased goods which have been paid in part or in full by wallet.
        /// </summary>
        Purchase,

        /// <summary>
        /// The customer has bought wallet credits.
        /// </summary>
        Refill,

        /// <summary>
        /// The admin has refunded the used credit balance.
        /// </summary>
        Refund,

        /// <summary>
        /// The admin has refunded a part of the used credit balance.
        /// </summary>
        PartialRefund,

        /// <summary>
        /// The admin has debited the wallet, e.g. because the purchase of credit was cancelled.
        /// </summary>
        Debit
    }
}
