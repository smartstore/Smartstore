using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Messages;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Data.Batching;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Admin.Controllers
{
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

        [Permission(Permissions.System.Message.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(QueuedEmailModel model)
        {
            var email = await _db.QueuedEmails.FindByIdAsync(model.Id);
            if (email == null)
            {
                return RedirectToAction("List");
            }

            _db.QueuedEmails.Remove(email);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.System.QueuedEmails.Deleted"));
            return RedirectToAction("List");
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

        [Permission(Permissions.System.Message.Delete)]
        [HttpPost, FormValueRequired("delete-all")]
        public async Task<IActionResult> DeleteAll()
        {
            var count = await _queuedEmailService.DeleteAllQueuedMailsAsync();
            NotifySuccess(T("Admin.Common.RecordsDeleted", count));
            return RedirectToAction("List");
        }

        [Permission(Permissions.System.Message.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var email = await _db.QueuedEmails.FindByIdAsync(id, false);
            if (email == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = await MapperFactory.MapAsync<QueuedEmail, QueuedEmailModel>(email);
            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(email.CreatedOnUtc, DateTimeKind.Utc);
            if (email.SentOnUtc.HasValue)
            {
                model.SentOn = _dateTimeHelper.ConvertToUserTime(email.SentOnUtc.Value, DateTimeKind.Utc);
            }

            return View(model);
        }

        [Permission(Permissions.System.Message.Update)]
        [HttpPost, FormValueRequired("save", "save-continue")]
        [ParameterBasedOnFormName("save-continue", "continueEditing")]
        public async Task<IActionResult> Edit(QueuedEmailModel model, bool continueEditing)
        {
            var email = await _db.QueuedEmails.FindByIdAsync(model.Id);
            if (email == null)
            {
                return RedirectToAction(nameof(List));
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, email);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.System.QueuedEmails.Updated"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = email.Id }) : RedirectToAction(nameof(List));
            }

            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(email.CreatedOnUtc, DateTimeKind.Utc);
            if (email.SentOnUtc.HasValue)
            {
                model.SentOn = _dateTimeHelper.ConvertToUserTime(email.SentOnUtc.Value, DateTimeKind.Utc);
            }

            return View(model);
        }

        [Permission(Permissions.System.Message.Create)]
        [HttpPost, ActionName("Edit"), FormValueRequired("requeue")]
        public async Task<IActionResult> Requeue(QueuedEmailModel queuedEmailModel)
        {
            var queuedEmail = await _db.QueuedEmails.FindByIdAsync(queuedEmailModel.Id, false);
            if (queuedEmail == null)
            {
                return RedirectToAction(nameof(List));
            }

            var requeuedEmail = new QueuedEmail
            {
                Priority = queuedEmail.Priority,
                From = queuedEmail.From,
                To = queuedEmail.To,
                CC = queuedEmail.CC,
                Bcc = queuedEmail.Bcc,
                Subject = queuedEmail.Subject,
                Body = queuedEmail.Body,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = queuedEmail.EmailAccountId,
                SendManually = queuedEmail.SendManually
            };

            _db.QueuedEmails.Add(requeuedEmail);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.System.QueuedEmails.Requeued"));

            return RedirectToAction(nameof(Edit), new { id = requeuedEmail.Id });
        }

        [Permission(Permissions.System.Message.Send)]
        [HttpPost, ActionName("Edit"), FormValueRequired("sendnow")]
        public async Task<IActionResult> SendNow(QueuedEmailModel queuedEmailModel)
        {
            var queuedEmail = await _db.QueuedEmails.FindByIdAsync(queuedEmailModel.Id, false);
            if (queuedEmail == null)
            {
                return RedirectToAction(nameof(List));
            }

            var result = await _queuedEmailService.SendMailsAsync(new List<QueuedEmail> { queuedEmail });
            if (result)
            {
                NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));
            }
            else
            {
                NotifyError(T("Common.Error.SendMail"));
            }

            return RedirectToAction(nameof(Edit), new { id = queuedEmail.Id });
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("go-to-email-by-number")]
        public async Task<IActionResult> GoToEmailByNumber(QueuedEmailListModel model)
        {
            var queuedEmail = await _db.QueuedEmails.FindByIdAsync(model.GoDirectlyToNumber ?? 0, false);
            if (queuedEmail != null)
            {
                return RedirectToAction(nameof(Edit), "QueuedEmail", new { id = queuedEmail.Id });
            }

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Message.Read)]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var qea = await _db.QueuedEmailAttachments.FindByIdAsync(id, false);
            if (qea == null)
            {
                return NotFound();
            }

            if (qea.StorageLocation == EmailAttachmentStorageLocation.Blob)
            {
                var data = _queuedEmailService.LoadQueuedMailAttachmentBinary(qea);
                if (data != null)
                {
                    return File(data, qea.MimeType, qea.Name);
                }
            }
            else if (qea.StorageLocation == EmailAttachmentStorageLocation.Path)
            {
                var path = qea.Path;
                if (path[0] == '~' || path[0] == '/')
                {
                    // TODO: (mh) (core) How to replace VirtualPathUtility.ToAppRelative ???
                    //path = CommonHelper.MapPath(VirtualPathUtility.ToAppRelative(path), false);
                    path = CommonHelper.MapPath(path, false);
                }

                if (!System.IO.File.Exists(path))
                {
                    NotifyError(string.Concat(T("Admin.Common.FileNotFound"), ": ", path));

                    return RedirectToReferrer(null, () => RedirectToAction(nameof(List)));
                }

                return File(path, qea.MimeType, qea.Name);
            }
            else if (qea.MediaFileId.HasValue)
            {
                return RedirectToAction("DownloadFile", "Download", new { downloadId = qea.MediaFileId.Value });
            }

            NotifyError(T("Admin.System.QueuedEmails.CouldNotDownloadAttachment"));
            return RedirectToAction(nameof(List));
        }
    }
}