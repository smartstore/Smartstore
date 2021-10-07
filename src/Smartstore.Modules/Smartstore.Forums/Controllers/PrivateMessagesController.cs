using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Messaging;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;
using Smartstore.Web.Controllers;

namespace Smartstore.Forums.Controllers
{
    // TODO: (mg) (core) inject frontend menu link for PM inbox.
    // TODO: (mg) (core) Check AccountDropdownViewComponent
    public class PrivateMessagesController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;
        private readonly IMessageFactory _messageFactory;
        private readonly ForumSettings _forumSettings;
        private readonly CustomerSettings _customerSettings;

        public PrivateMessagesController(
            SmartDbContext db,
            IForumService forumService,
            IMessageFactory messageFactory,
            ForumSettings forumSettings,
            CustomerSettings customerSettings)
        {
            _db = db;
            _forumService = forumService;
            _messageFactory = messageFactory;
            _forumSettings = forumSettings;
            _customerSettings = customerSettings;
        }

        [LocalizedRoute("privatemessages/{tab?}", Name = "PrivateMessages")]
        public async Task<IActionResult> Index(int? page, string tab, int? inboxPage, int? sentPage)
        {
            if (!_forumSettings.AllowPrivateMessages)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            if (currentCustomer.IsGuest())
            {
                return Unauthorized();
            }

            var store = Services.StoreContext.CurrentStore;

            var query = _db.PrivateMessages()
                .Include(x => x.FromCustomer)
                .Include(x => x.ToCustomer)
                .AsNoTracking();

            var inboxMessages = await query
                .ApplyStandardFilter(toCustomerId: currentCustomer.Id, storeId: store.Id)
                .ApplyStatusFilter(isDeletedByRecipient: false)
                .ToPagedList(inboxPage.HasValue ? inboxPage.Value - 1 : 0, _forumSettings.PrivateMessagesPageSize)
                .LoadAsync();

            var sentMessages = await query
                .ApplyStandardFilter(fromCustomerId: currentCustomer.Id, storeId: store.Id)
                .ApplyStatusFilter(isDeletedByAuthor: false)
                .ToPagedList(sentPage.HasValue ? sentPage.Value - 1 : 0, _forumSettings.PrivateMessagesPageSize)
                .LoadAsync();

            var model = new PrivateMessageListModel
            {
                SentMessagesSelected = tab.EqualsNoCase("sent"),
                InboxMessages = inboxMessages
                    .Select(x => CreatePrivateMessageModel(x))
                    .ToPagedList(inboxMessages.PageIndex, inboxMessages.PageSize, inboxMessages.TotalCount),
                SentMessages = sentMessages
                    .Select(x => CreatePrivateMessageModel(x))
                    .ToPagedList(sentMessages.PageIndex, sentMessages.PageSize, sentMessages.TotalCount),
            };

            return View(model);
        }

        [HttpPost, FormValueRequired("delete-inbox"), ActionName("InboxUpdate")]
        public async Task<IActionResult> DeleteInboxPM(IFormCollection form)
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var ids = GetPrivateMessageIds(form);

            if (ids.Any())
            {
                var messages = await _db.PrivateMessages()
                    .Where(x => ids.Contains(x.Id) && x.ToCustomerId == currentCustomer.Id)
                    .ToListAsync();

                if (messages.Any())
                {
                    foreach (var pm in messages)
                    {
                        if (pm.IsDeletedByAuthor)
                        {
                            // Marked as deleted by author and by recipient -> physically delete message.
                            _db.PrivateMessages().Remove(pm);
                        }
                        else
                        {
                            pm.IsDeletedByRecipient = true;
                        }
                    }

                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost, FormValueRequired("mark-unread"), ActionName("InboxUpdate")]
        public async Task<IActionResult> MarkUnread(IFormCollection form)
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var ids = GetPrivateMessageIds(form);

            if (ids.Any())
            {
                var messages = await _db.PrivateMessages()
                    .Where(x => ids.Contains(x.Id) && x.ToCustomerId == currentCustomer.Id)
                    .ToListAsync();

                messages.Each(pm => pm.IsRead = false);

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost, FormValueRequired("delete-sent"), ActionName("SentUpdate")]
        public async Task<IActionResult> DeleteSentPM(IFormCollection form)
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var ids = GetPrivateMessageIds(form, "si");

            if (ids.Any())
            {
                var messages = await _db.PrivateMessages()
                    .Where(x => ids.Contains(x.Id) && x.FromCustomerId == currentCustomer.Id)
                    .ToListAsync();

                if (messages.Any())
                {
                    foreach (var pm in messages)
                    {
                        if (pm.IsDeletedByRecipient)
                        {
                            // Marked as deleted by author and by recipient -> physically delete message.
                            _db.PrivateMessages().Remove(pm);
                        }
                        else
                        {
                            pm.IsDeletedByAuthor = true;
                        }
                    }

                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index", new { tab = "sent" });
        }

        public async Task<IActionResult> Send(int id /* toCustomerId */, int? replyToMessageId)
        {
            if (!_forumSettings.AllowPrivateMessages)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            if (currentCustomer.IsGuest())
            {
                return Unauthorized();
            }

            var customerTo = await _db.Customers.FindByIdAsync(id, false);
            if (customerTo == null || customerTo.IsGuest())
            {
                return RedirectToAction("Index");
            }

            var model = new SendPrivateMessageModel
            {
                ToCustomerId = customerTo.Id,
                CustomerToName = customerTo.FormatUserName(),
                HasCustomerProfile = _customerSettings.AllowViewingProfiles && !customerTo.IsGuest()
            };

            if (replyToMessageId.HasValue)
            {
                var replyToPM = await _db.PrivateMessages().FindByIdAsync(replyToMessageId.Value, false);
                if (replyToPM == null)
                {
                    return RedirectToAction("Index");
                }

                if (replyToPM.ToCustomerId == currentCustomer.Id || replyToPM.FromCustomerId == currentCustomer.Id)
                {
                    model.ReplyToMessageId = replyToPM.Id;
                    model.Subject = $"Re: {replyToPM.Subject}";
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Send(SendPrivateMessageModel model)
        {
            if (!_forumSettings.AllowPrivateMessages)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            if (currentCustomer.IsGuest())
            {
                return Unauthorized();
            }

            Customer toCustomer;
            var replyToPM = await _db.PrivateMessages().FindByIdAsync(model.ReplyToMessageId, false);
            if (replyToPM != null)
            {
                if (replyToPM.ToCustomerId == currentCustomer.Id || replyToPM.FromCustomerId == currentCustomer.Id)
                {
                    toCustomer = replyToPM.FromCustomer;
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            else
            {
                toCustomer = await _db.Customers.FindByIdAsync(model.ToCustomerId, false);
            }

            if (toCustomer == null || toCustomer.IsGuest())
            {
                return RedirectToAction("Index");
            }

            model.ToCustomerId = toCustomer.Id;
            model.CustomerToName = toCustomer.FormatUserName();
            model.HasCustomerProfile = _customerSettings.AllowViewingProfiles && !toCustomer.IsGuest();

            if (ModelState.IsValid)
            {
                try
                {
                    var pm = new PrivateMessage
                    {
                        StoreId = Services.StoreContext.CurrentStore.Id,
                        ToCustomerId = toCustomer.Id,
                        FromCustomerId = currentCustomer.Id,
                        IsDeletedByAuthor = false,
                        IsDeletedByRecipient = false,
                        IsRead = false,
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    pm.Subject = _forumSettings.PMSubjectMaxLength > 0 && model.Subject.Length > _forumSettings.PMSubjectMaxLength
                        ? model.Subject.Substring(0, _forumSettings.PMSubjectMaxLength)
                        : model.Subject;

                    pm.Text = _forumSettings.PMTextMaxLength > 0 && model.Message.Length > _forumSettings.PMTextMaxLength
                        ? model.Message.Substring(0, _forumSettings.PMTextMaxLength)
                        : model.Message;

                    _db.PrivateMessages().Add(pm);
                    await _db.SaveChangesAsync();

                    Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreSendPM, T("ActivityLog.PublicStore.SendPM"), toCustomer.Email);

                    if (_forumSettings.NotifyAboutPrivateMessages)
                    {
                        await _messageFactory.SendPrivateMessageNotificationAsync(toCustomer, pm, Services.WorkContext.WorkingLanguage.Id);
                    }

                    return RedirectToAction("Index", new { tab = "sent" });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            return View(model);
        }

        public async Task<IActionResult> View(int id /* privateMessageId */)
        {
            if (!_forumSettings.AllowPrivateMessages)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            if (currentCustomer.IsGuest())
            {
                return Unauthorized();
            }

            var pm = await _db.PrivateMessages().FindByIdAsync(id);
            if (pm != null)
            {
                if (pm.ToCustomerId != currentCustomer.Id && pm.FromCustomerId != currentCustomer.Id)
                {
                    return RedirectToAction("Index");
                }

                if (!pm.IsRead && pm.ToCustomerId ==  currentCustomer.Id)
                {
                    pm.IsRead = true;
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                return RedirectToAction("Index");
            }

            var model = CreatePrivateMessageModel(pm, true);

            return View(model);
        }

        public async Task<IActionResult> Delete(int id /* privateMessageId */)
        {
            if (!_forumSettings.AllowPrivateMessages)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            if (currentCustomer.IsGuest())
            {
                return Unauthorized();
            }

            var pm = await _db.PrivateMessages().FindByIdAsync(id);
            if (pm != null)
            {
                if ((pm.FromCustomerId == currentCustomer.Id && pm.IsDeletedByRecipient) ||
                    (pm.ToCustomerId == currentCustomer.Id && pm.IsDeletedByAuthor))
                {
                    // Marked as deleted by author and by recipient -> physically delete message.
                    _db.PrivateMessages().Remove(pm);
                }
                else
                {
                    if (pm.FromCustomerId == currentCustomer.Id)
                    {
                        pm.IsDeletedByAuthor = true;
                    }

                    if (pm.ToCustomerId == currentCustomer.Id)
                    {
                        pm.IsDeletedByRecipient = true;
                    }
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        private PrivateMessageModel CreatePrivateMessageModel(PrivateMessage pm, bool forDetailView = false)
        {
            var model = new PrivateMessageModel
            {
                Id = pm.Id,
                FromCustomerId = pm.FromCustomer.Id,
                CustomerFromName = pm.FromCustomer.FormatUserName(),
                HasCustomerProfile = _customerSettings.AllowViewingProfiles && pm.FromCustomer != null && !pm.FromCustomer.IsGuest(),
                ToCustomerId = pm.ToCustomer.Id,
                CustomerToName = pm.ToCustomer.FormatUserName(),
                AllowViewingToProfile = _customerSettings.AllowViewingProfiles && pm.ToCustomer != null && !pm.ToCustomer.IsGuest(),
                Subject = pm.Subject,
                Message = forDetailView ? _forumService.FormatPrivateMessage(pm) : pm.Text,
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(pm.CreatedOnUtc, DateTimeKind.Utc),
                IsRead = pm.IsRead
            };

            return model;
        }

        private static List<int> GetPrivateMessageIds(IFormCollection form, string keyPrefix = "pm")
        {
            var ids = new List<int>();

            foreach (var key in form.Keys)
            {
                var value = form[key].FirstOrDefault();

                if (value.EqualsNoCase("on") && key.StartsWith(keyPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (int.TryParse(key.Replace(keyPrefix, string.Empty).Trim(), out var privateMessageId))
                    {
                        ids.Add(privateMessageId);
                    }
                }
            }

            return ids;
        }
    }
}
