namespace Smartstore.Admin.Models.Customers;

/// <summary>
/// Contains the visible details and existing message link for a recorded contact enquiry.
/// </summary>
public class DashboardContactRequestModel : ModelBase
{
    /// <summary>
    /// Gets or sets the identifier of the existing queued message.
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// Gets or sets the email address entered in the contact form.
    /// </summary>
    [LocalizedDisplay("Common.Email")]
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the creation date formatted in the current user's time zone.
    /// </summary>
    [LocalizedDisplay("Common.Date")]
    public string CreatedOn { get; set; }
}
