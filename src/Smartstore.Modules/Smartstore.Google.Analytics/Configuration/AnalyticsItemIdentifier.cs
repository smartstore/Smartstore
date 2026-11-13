namespace Smartstore.Google.Analytics.Settings;

/// <summary>
/// Specifies which value is transmitted to Google Analytics as item_id.
/// </summary>
public enum AnalyticsItemIdentifier
{
    /// <summary>
    /// The SKU of the product or of the selected attribute combination.
    /// </summary>
    Sku = 0,

    /// <summary>
    /// The product ID. Matches the ID of the Google Merchant Center feed
    /// if attribute combinations are not exported as products.
    /// </summary>
    ProductId = 10,

    /// <summary>
    /// The product ID, combined with the attribute combination ID (e.g. 123-45) if a combination is selected.
    /// Matches the ID of the Google Merchant Center feed if attribute combinations are exported as products.
    /// </summary>
    ProductIdWithCombinationId = 20
}
