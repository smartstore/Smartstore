namespace Smartstore.Admin.Models.Customers;

/// <summary>
/// Contains counts of current saved cookie consent decisions.
/// </summary>
public class DashboardCookieConsentModel : ModelBase
{
    /// <summary>
    /// Gets or sets the number of valid saved decisions.
    /// </summary>
    public int TotalConsents { get; set; }

    /// <summary>
    /// Gets or sets the number of unreadable or incomplete records excluded from the report.
    /// </summary>
    public int SkippedConsents { get; set; }

    /// <summary>
    /// Gets or sets the consent categories in display order.
    /// </summary>
    public IReadOnlyList<DashboardCookieConsentCategoryModel> Categories { get; set; } = [];
}

/// <summary>
/// Counts stored values for one category without treating absent values as a lack of consent.
/// </summary>
public class DashboardCookieConsentCategoryModel : ModelBase
{
    /// <summary>
    /// Gets or sets the existing cookie manager resource key naming the category.
    /// </summary>
    public string CategoryResourceKey { get; set; }

    /// <summary>
    /// Gets or sets the number of stored values granting consent.
    /// </summary>
    public int AcceptedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of stored values not granting consent.
    /// </summary>
    public int DeclinedCount { get; set; }

    /// <summary>
    /// Gets the consent percentage among stored values for this category.
    /// </summary>
    public decimal AcceptedPercentage => AcceptedCount + DeclinedCount == 0 ? 0
        : Math.Round(100m * AcceptedCount / (AcceptedCount + DeclinedCount), 1);

    /// <summary>
    /// Gets the percentage without consent, complementing the rounded consent percentage.
    /// </summary>
    public decimal DeclinedPercentage => AcceptedCount + DeclinedCount == 0 ? 0 : 100m - AcceptedPercentage;
}
