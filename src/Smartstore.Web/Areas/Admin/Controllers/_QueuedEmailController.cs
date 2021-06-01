using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Messages;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Data.Batching;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Admin.Controllers
{
    // TODO: (ms) (core) Implement missing action methods. (wip)
    // DownloadAttachment, Edit/Edit (view), delete all, GoToEmailByNumber, Requeue, SendNow

    public class QueuedEmailController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IQueuedEmailService _queuedEmailService;

        public QueuedEmailController(
            SmartDbContext db,
            IDateTimeHelper dateTimeHelper,
            IQueuedEmailService queuedEmailService)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
            _queuedEmailService = queuedEmailService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Message.Read)]
        public IActionResult List()
        {
            var model = new QueuedEmailListModel();
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Read)]
        public async Task<IActionResult> QueuedEmailList(GridCommand command, QueuedEmailListModel model)
        {
            DateTime? startDateValue = model.SearchStartDate != null
                ? _dateTimeHelper.ConvertToUtcTime(model.SearchStartDate.Value, _dateTimeHelper.CurrentTimeZone)
                : null;

            DateTime? endDateValue = model.SearchEndDate != null
                ? _dateTimeHelper.ConvertToUtcTime(model.SearchEndDate.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1)
                : null;

            var query = _db.QueuedEmails.AsNoTracking()
                .Include(x => x.EmailAccount)
                .ApplyTimeFilter(startDateValue, endDateValue, model.SearchLoadNotSent)
                .ApplyMailAddressFilter(model.SearchFromEmail, model.SearchToEmail)
                .Where(x => x.SentTries < model.SearchMaxSentTries)
                .ApplyGridCommand(command, false);

            if (model.SearchSendManually.HasValue)
            {
                query = query.Where(x => x.SendManually == model.SearchSendManually);
            }

            var queuedEmails = await query.ToPagedList(command.Page - 1, command.PageSize).LoadAsync();

            var gridModel = new GridModel<QueuedEmailModel>
            {
                Rows = await queuedEmails.SelectAsync(async x =>
                {
                    var model = new QueuedEmailModel();
                    await MapperFactory.MapAsync(x, model);

                    model.CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                    if (x.SentOnUtc.HasValue)
                    {
                        model.SentOn = _dateTimeHelper.ConvertToUserTime(x.SentOnUtc.Value, DateTimeKind.Utc);
                    }

                    model.ViewUrl = Url.Action(nameof(Edit), "QueuedEmail", new { id = x.Id });

                    return model;
                })
                .AsyncToList(),

                Total = queuedEmails.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Delete)]
        public async Task<IActionResult> QueuedEmailDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds().ToList();
            var numDeleted = 0;
            if (ids.Any())
            {
                numDeleted = await _db.QueuedEmails
                    .Where(x => ids.Contains(x.Id))
                    .BatchDeleteAsync();
            }

            return Json(new
            {
                Success = true,
                Count = numDeleted
            });
        }

        [Permission(Permissions.System.Message.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var email = await _db.QueuedEmails.FindByIdAsync(id);
            if (email == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = new QueuedEmailModel();
            await MapperFactory.MapAsync(email, model);

            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(email.CreatedOnUtc, DateTimeKind.Utc);
            if (email.SentOnUtc.HasValue)
            {
                model.SentOn = _dateTimeHelper.ConvertToUserTime(email.SentOnUtc.Value, DateTimeKind.Utc);
            }

            return View(model);
        }
    }
}