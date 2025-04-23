using Autofac;
using Smartstore.Admin.Models.Messages;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Http;
using Smartstore.Threading;
using Smartstore.Utilities;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class QueuedEmailController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly AsyncRunner _asyncRunner;

        public QueuedEmailController(
            SmartDbContext db,
            IDateTimeHelper dateTimeHelper,
            IQueuedEmailService queuedEmailService,
            AsyncRunner asyncRunner)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
            _queuedEmailService = queuedEmailService;
            _asyncRunner = asyncRunner;
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

            var query = _db.QueuedEmails
                .AsNoTracking()
                .Include(x => x.EmailAccount)
                .Include(x => x.Attachments)
                .ApplyTimeFilter(startDateValue, endDateValue, model.SearchLoadNotSent)
                .ApplyMailAddressFilter(model.SearchFromEmail, model.SearchToEmail)
                .Where(x => x.SentTries < model.SearchMaxSentTries);

            if (model.SearchSendManually.HasValue)
            {
                query = query.Where(x => x.SendManually == model.SearchSendManually);
            }

            query = query
                .ApplySorting(true)
                .ApplyGridCommand(command, false)
                .SelectSummary();

            var queuedEmails = await query
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<QueuedEmail, QueuedEmailModel>();
            var rows = await queuedEmails.SelectAwait(async x =>
            {
                var model = await mapper.MapAsync(x);
                model.ViewUrl = Url.Action(nameof(Edit), "QueuedEmail", new { id = x.Id });

                return model;
            })
            .AsyncToList();

            return Json(new GridModel<QueuedEmailModel>
            {
                Rows = rows,
                Total = queuedEmails.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var email = await _db.QueuedEmails.FindByIdAsync(id);
            if (email == null)
            {
                return NotFound();
            }

            _db.QueuedEmails.Remove(email);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.System.QueuedEmails.Deleted"));
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Delete)]
        public async Task<IActionResult> QueuedEmailDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var toDelete = await _db.QueuedEmails.GetManyAsync(ids, true);
                _db.QueuedEmails.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
                numDeleted = toDelete.Count;
            }

            return Json(new
            {
                Success = true,
                Count = numDeleted
            });
        }


        [Permission(Permissions.System.Message.Delete)]
        public async Task<IActionResult> DeleteAll()
        {
            var count = await _queuedEmailService.DeleteAllQueuedMailsAsync();
            NotifySuccess(T("Admin.Common.RecordsDeleted", count));
            return RedirectToAction(nameof(List));
        }

        /// <summary>
        /// Deletes orphaned data caused by the deletion of <see cref="QueuedEmail"/> entities.
        /// </summary>
        /// <remarks>
        /// Should only be executed once, as no more new orphaned data will be created after deletion.
        /// That is why we do not have a service method for this.
        /// </remarks>
        [MaintenanceAction]
        [Permission(Permissions.System.Message.Delete)]
        public async Task<IActionResult> Cleanup(int batchSize = 128)
        {
            var numTotalDeleted = 0;

            try
            {
                // MediaStorages that are neither referenced by MediaFiles nor by QueuedEmailAttachments.
                var query = (
                    from ms in _db.MediaStorage
                    join mf in _db.MediaFiles on ms.Id equals mf.MediaStorageId into smf
                    from mf in smf.DefaultIfEmpty()
                    join ea in _db.QueuedEmailAttachments on ms.Id equals ea.MediaStorageId into sea
                    from ea in sea.DefaultIfEmpty()
                    where mf == null && ea == null
                    select ms)
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .Take(batchSize);

                while (true)
                {
                    var ids = await query.ToListAsync();
                    if (ids.Count == 0)
                    {
                        break;
                    }

                    var numDeleted = await _db.MediaStorage
                        .Where(x => ids.Contains(x.Id))
                        .ExecuteDeleteAsync();

                    if (numDeleted == 0)
                    {
                        break;
                    }

                    numTotalDeleted += numDeleted;
                }

                if (numTotalDeleted > 500 && _db.DataProvider.CanOptimizeTable)
                {
                    var tableName = _db.Model.FindEntityType(typeof(MediaStorage)).GetTableName();
                    await CommonHelper.TryAction(() => _db.DataProvider.OptimizeTableAsync(tableName));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Content($"ERROR: {ex.Message}");
            }

            return Content(T("Admin.System.Maintenance.CleanupOrphanedRecords", numTotalDeleted.ToString("N0"), nameof(MediaStorage)));
        }

        [Permission(Permissions.System.Message.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var email = await _db.QueuedEmails
                .Include(x => x.EmailAccount)
                .Include(x => x.Attachments)
                .FindByIdAsync(id, false);

            if (email == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = await MapperFactory.MapAsync<QueuedEmail, QueuedEmailModel>(email);

            return View(model);
        }

        [Permission(Permissions.System.Message.Update)]
        [HttpPost, FormValueRequired("save", "save-continue")]
        [ParameterBasedOnFormName("save-continue", "continueEditing")]
        public async Task<IActionResult> Edit(QueuedEmailModel model, bool continueEditing)
        {
            var queuedEmail = await _db.QueuedEmails.FindByIdAsync(model.Id);
            if (queuedEmail == null)
            {
                return RedirectToAction(nameof(List));
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, queuedEmail);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.System.QueuedEmails.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = queuedEmail.Id })
                    : RedirectToAction(nameof(List));
            }

            await MapperFactory.MapAsync(queuedEmail, model);

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
            var queuedEmail = await _db.QueuedEmails
                .Include(x => x.EmailAccount)
                .FindByIdAsync(queuedEmailModel.Id);

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
            var id = model.GoDirectlyToNumber ?? 0;

            if (id != 0 && await _db.QueuedEmails.AnyAsync(x => x.Id == id))
            {
                return RedirectToAction(nameof(Edit), new { id });
            }

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Message.Read)]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var qea = await _db.QueuedEmailAttachments
                .Include(x => x.MediaStorage)
                .FindByIdAsync(id, false);

            if (qea == null)
            {
                return NotFound();
            }

            if (qea.StorageLocation == EmailAttachmentStorageLocation.Blob)
            {
                var data = qea.MediaStorage?.Data ?? Array.Empty<byte>();

                return File(data, qea.MimeType, qea.Name);
            }
            else if (qea.StorageLocation == EmailAttachmentStorageLocation.Path)
            {
                var path = qea.Path;
                if (path[0] == '~' || path[0] == '/')
                {
                    path = CommonHelper.MapPath(WebHelper.ToAppRelativePath(path), false);
                }

                if (!System.IO.File.Exists(path))
                {
                    NotifyError(string.Concat(T("Admin.Common.FileNotFound"), ": ", path));

                    return RedirectToReferrer(null, () => RedirectToAction(nameof(List)));
                }

                return PhysicalFile(path, qea.MimeType, qea.Name);
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