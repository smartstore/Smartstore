using System.Net.Mail;
using Smartstore.Admin.Infrastructure.Dashboard;
using Smartstore.Admin.Models.Customers;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Messaging;

namespace Smartstore.Admin.Components;

/// <summary>
/// Renders recently recorded contact messages without inspecting their subjects.
/// </summary>
public class DashboardContactRequestsViewComponent : SmartViewComponent
{
    /// <summary>
    /// Gets the data permission required to display contact messages.
    /// </summary>
    public const string RequiredPermission = Permissions.Dashboard.Widgets.Customers.ContactRequests;

    private readonly SmartDbContext _db;

    public DashboardContactRequestsViewComponent(SmartDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Loads the newest marked contact messages from authorized stores.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
            return Empty();

        var authorizedStoreIds = await Services.StoreMappingService.GetAuthorizedStoreIdsAsync(nameof(Customer), Services.WorkContext.CurrentCustomer.Id);
        var storeIds = _db.Stores.Select(store => store.Id);
        var query = _db.GenericAttributes
            .AsNoTracking()
            .Where(attribute => attribute.KeyGroup == nameof(QueuedEmail) 
                && attribute.Key == ContactRequestsDashboardHook.AttributeKey
                && storeIds.Contains(attribute.StoreId));

        if (authorizedStoreIds.Length > 0)
        {
            query = query.Where(attribute => authorizedStoreIds.Contains(attribute.StoreId));
        }
            
        var messages = await _db.QueuedEmails
            .AsNoTracking()
            .Join(query,
                message => message.Id,
                attribute => attribute.EntityId,
                (message, attribute) => new { message.Id, Email = attribute.Value, message.CreatedOnUtc })
            .OrderByDescending(message => message.CreatedOnUtc)
            .ThenByDescending(message => message.Id)
            .Take(ContactRequestsDashboardHook.MaxEntries)
            .ToListAsync();

        var model = messages.Select(message => new DashboardContactRequestModel
        {
            MessageId = message.Id,
            Email = MailAddress.TryCreate(message.Email, out var sender) ? sender.Address : string.Empty,
            CreatedOn = Services.DateTimeHelper.ConvertToUserTime(message.CreatedOnUtc, DateTimeKind.Utc).ToString("g")
        }).ToList();

        return View(model);
    }
}
