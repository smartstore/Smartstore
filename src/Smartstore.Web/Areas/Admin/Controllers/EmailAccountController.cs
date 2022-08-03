using Smartstore.Admin.Models.Messages;
using Smartstore.ComponentModel;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Net.Mail;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class EmailAccountController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly Lazy<IMailService> _mailService;

        public EmailAccountController(
            SmartDbContext db,
            IStoreContext storeContext,
            EmailAccountSettings emailAccountSettings,
            Lazy<IMailService> mailService)
        {
            _db = db;
            _emailAccountSettings = emailAccountSettings;
            _storeContext = storeContext;
            _mailService = mailService;
        }

        [Permission(Permissions.Configuration.Measure.Read)]
        public IActionResult List()
        {
            return View();
        }

        [Permission(Permissions.Configuration.EmailAccount.Read)]
        public async Task<IActionResult> EmailAccountList(GridCommand command)
        {
            var emailAccounts = await _db.EmailAccounts
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<EmailAccount, EmailAccountModel>();
            var emailAccountModels = await emailAccounts.SelectAwait(async x =>
            {
                var model = await mapper.MapAsync(x);
                model.IsDefaultEmailAccount = x.Id == _emailAccountSettings.DefaultEmailAccountId;
                model.EditUrl = Url.Action(nameof(Edit), "EmailAccount", new { id = x.Id });

                return model;
            })
            .AsyncToList();

            var gridModel = new GridModel<EmailAccountModel>
            {
                Rows = emailAccountModels,
                Total = await emailAccounts.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.EmailAccount.Delete)]
        public async Task<IActionResult> EmailAccountDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                try
                {
                    var emailAccounts = await _db.EmailAccounts.GetManyAsync(ids, true);
                    _db.EmailAccounts.RemoveRange(emailAccounts);

                    numDeleted = await _db.SaveChangesAsync();
                    success = true;
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.EmailAccount.Update)]
        public async Task<IActionResult> SetDefaultEmailAccount(int id)
        {
            var emailAccount = await _db.EmailAccounts.FindByIdAsync(id, false);
            if (emailAccount == null)
            {
                return NotFound();
            }

            _emailAccountSettings.DefaultEmailAccountId = emailAccount.Id;
            await Services.SettingFactory.SaveSettingsAsync(_emailAccountSettings);

            return Json(new { Success = true });
        }

        [Permission(Permissions.Configuration.EmailAccount.Create)]
        public IActionResult Create()
        {
            var model = new EmailAccountModel
            {
                Port = 25
            };

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.EmailAccount.Create)]
        public async Task<IActionResult> Create(EmailAccountModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var emailAccount = await MapperFactory.MapAsync<EmailAccountModel, EmailAccount>(model);
                _db.EmailAccounts.Add(emailAccount);
                await _db.SaveChangesAsync();

                if (model.IsDefaultEmailAccount)
                {
                    _emailAccountSettings.DefaultEmailAccountId = emailAccount.Id;
                    await Services.SettingFactory.SaveSettingsAsync(_emailAccountSettings);
                }

                NotifySuccess(T("Admin.Configuration.EmailAccounts.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = emailAccount.Id })
                    : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            return View(model);
        }

        [Permission(Permissions.Configuration.EmailAccount.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var emailAccount = await _db.EmailAccounts.FindByIdAsync(id, false);
            if (emailAccount == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<EmailAccount, EmailAccountModel>(emailAccount);
            model.IsDefaultEmailAccount = emailAccount.Id == _emailAccountSettings.DefaultEmailAccountId;

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Configuration.EmailAccount.Update)]
        public async Task<IActionResult> Edit(EmailAccountModel model, bool continueEditing)
        {
            var emailAccount = await _db.EmailAccounts.FindByIdAsync(model.Id);
            if (emailAccount == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, emailAccount);

                if (model.IsDefaultEmailAccount && _emailAccountSettings.DefaultEmailAccountId != emailAccount.Id)
                {
                    _emailAccountSettings.DefaultEmailAccountId = emailAccount.Id;
                    await Services.Settings.ApplySettingAsync(_emailAccountSettings, x => x.DefaultEmailAccountId);
                }

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.EmailAccounts.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = emailAccount.Id })
                    : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.EmailAccount.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var emailAccount = await _db.EmailAccounts.FindByIdAsync(id);
            if (emailAccount == null)
            {
                return NotFound();
            }

            _db.EmailAccounts.Remove(emailAccount);
            await _db.SaveChangesAsync();
            NotifySuccess(T("Admin.Configuration.EmailAccounts.Deleted"));

            return RedirectToAction(nameof(List));
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("sendtestemail")]
        [Permission(Permissions.Configuration.EmailAccount.Update)]
        public async Task<IActionResult> SendTestEmail(EmailAccountModel model)
        {
            var emailAccount = await _db.EmailAccounts.FindByIdAsync(model.Id);
            if (emailAccount == null)
            {
                return NotFound();
            }

            try
            {
                if (model.SendTestEmailTo.IsEmpty())
                {
                    NotifyError(T("Admin.Common.EnterEmailAdress"));
                }
                else
                {
                    // Avoid System.ArgumentException: "The specified string is not in the form required for a subject" when testing mails.
                    var subject = string.Concat(_storeContext.CurrentStore.Name, ". ", T("Admin.Configuration.EmailAccounts.TestingEmail"))
                        .RegexReplace(@"\p{C}+", " ")
                        .TrimSafe();

                    using var msg = new MailMessage(model.SendTestEmailTo, subject, T("Admin.Common.EmailSuccessfullySent"), emailAccount.Email);
                    using var client = await _mailService.Value.ConnectAsync(emailAccount);

                    await client.SendAsync(msg);

                    NotifySuccess(T("Admin.Configuration.EmailAccounts.SendTestEmail.Success"), false);
                }
            }
            catch (Exception ex)
            {
                model.TestEmailShortErrorMessage = ex.ToAllMessages();
                model.TestEmailFullErrorMessage = ex.ToString();
            }

            return View(model);
        }
    }
}
