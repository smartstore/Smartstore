using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Messaging;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Mappers;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Search;
using Smartstore.Forums.Services;
using Smartstore.Net;
using Smartstore.Web.Controllers;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Search;

namespace Smartstore.Forums.Controllers
{
    public class PrivateMessagesController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ForumSettings _forumSettings;

        public PrivateMessagesController(
            SmartDbContext db,
            ForumSettings forumSettings)
        {
            _db = db;
            _forumSettings = forumSettings;
        }

        [Route("privatemessages/{tab?}", Name = "PrivateMessages")]
        public async Task<IActionResult> Index(int? page, string tab)
        {
            if (!_forumSettings.AllowPrivateMessages)
            {
                return NotFound();
            }

            if (Services.WorkContext.CurrentCustomer.IsGuest())
            {
                return Unauthorized();
            }

            var store = Services.StoreContext.CurrentStore;
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var inboxPage = 0;
            var sentItemsPage = 0;
            var sentItemsTabSelected = false;
            int? fromCustomerId = null;
            int? toCustomerId = null;
            bool? isDeletedByAuthor = null;
            bool? isDeletedByRecipient = null;
            List<PrivateMessageModel> messageModels = null;

            if (tab.EqualsNoCase("sent"))
            {
                sentItemsPage = page ?? 0;
                sentItemsTabSelected = true;
                fromCustomerId = currentCustomer.Id;
                isDeletedByAuthor = false;
            }
            else
            {
                // Inbox.
                inboxPage = page ?? 0;
                toCustomerId = currentCustomer.Id;
                isDeletedByRecipient = false;
            }

            var messages = await _db.PrivateMessages()
                .AsNoTracking()
                .ApplyStandardFilter(fromCustomerId, toCustomerId, store.Id)
                .ApplyStatusFilter(null, isDeletedByAuthor, isDeletedByRecipient)
                .ToPagedList(page.HasValue ? page.Value - 1 : 0, _forumSettings.PrivateMessagesPageSize)
                .LoadAsync();

            //...

            var model = new PrivateMessageListModel(messages)
            {
                Messages = messageModels
            };

            ViewBag.InboxPage = inboxPage;
            ViewBag.SentItemsPage = sentItemsPage;
            ViewBag.SentItemsTabSelected = sentItemsTabSelected;

            return View(model);
        }
    }
}
