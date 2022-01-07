namespace Smartstore.Core.DataExchange
{
    /// <summary>
    /// Supported entity types
    /// </summary>
    public enum ImportEntityType
    {
        Product = 0,
        Category,
        Customer,
        NewsletterSubscription
    }

    public enum ImportFileType
    {
        Csv = 0,
        Xlsx
    }

    [Flags]
    public enum ImportModeFlags
    {
        Insert = 1,
        Update = 2
    }
}