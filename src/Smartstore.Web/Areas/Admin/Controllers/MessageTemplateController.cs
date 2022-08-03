using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Admin.Models.Messages;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Net.Mail;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class MessageTemplateController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly IMemoryCache _memCache;
        private readonly ICampaignService _campaignService;
        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IMessageFactory _messageFactory;
        private readonly IEmailAccountService _emailAccountService;
        private readonly Lazy<IMailService> _mailService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IMediaTracker _mediaTracker;
        private readonly IMessageModelProvider _messageModelProvider;

        public MessageTemplateController(
            SmartDbContext db,
            IApplicationContext appContext,
            IMemoryCache memCache,
            ICampaignService campaignService,
            IMessageTemplateService messageTemplateService,
            IMessageFactory messageFactory,
            IEmailAccountService emailAccountService,
            Lazy<IMailService> mailService,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            ILocalizationService localizationService,
            IStoreMappingService storeMappingService,
            EmailAccountSettings emailAccountSettings,
            IMediaTracker mediaTracker,
            IMessageModelProvider messageModelProvider)
        {
            _db = db;
            _appContext = appContext;
            _memCache = memCache;
            _campaignService = campaignService;
            _messageTemplateService = messageTemplateService;
            _messageFactory = messageFactory;
            _emailAccountService = emailAccountService;
            _mailService = mailService;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _localizationService = localizationService;
            _storeMappingService = storeMappingService;
            _emailAccountSettings = emailAccountSettings;
            _mediaTracker = mediaTracker;
            _messageModelProvider = messageModelProvider;
        }

        #region Utilities

        private async Task UpdateLocalesAsync(MessageTemplate mt, MessageTemplateModel model)
        {
            foreach (var localized in model.Locales)
            {
                int lid = localized.LanguageId;

                // Attachments: handle tracking of localized media file uploads
                var attachments = new List<(int? prevId, int? curId, string prop)>(3)
                {
                    (mt.GetLocalized(x => x.Attachment1FileId, lid, false, false), localized.Attachment1FileId, $"Attachment1FileId[{lid}]"),
                    (mt.GetLocalized(x => x.Attachment2FileId, lid, false, false), localized.Attachment2FileId, $"Attachment2FileId[{lid}]"),
                    (mt.GetLocalized(x => x.Attachment3FileId, lid, false, false), localized.Attachment3FileId, $"Attachment3FileId[{lid}]")
                };

                foreach (var attach in attachments)
                {
                    if (attach.prevId != attach.curId)
                    {
                        if (attach.prevId.HasValue)
                        {
                            await _mediaTracker.UntrackAsync(mt, attach.prevId.Value, attach.prop);
                        }
                        if (attach.curId.HasValue)
                        {
                            await _mediaTracker.TrackAsync(mt, attach.curId.Value, attach.prop);
                        }
                    }
                }

                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.To, localized.To, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.ReplyTo, localized.ReplyTo, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.BccEmailAddresses, localized.BccEmailAddresses, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.Subject, localized.Subject, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.Body, localized.Body, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.EmailAccountId, localized.EmailAccountId, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.Attachment1FileId, localized.Attachment1FileId, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.Attachment2FileId, localized.Attachment2FileId, lid);
                await _localizedEntityService.ApplyLocalizedValueAsync(mt, x => x.Attachment3FileId, localized.Attachment3FileId, lid);
            }
        }

        private async Task PrepareStoresMappingModelAsync(MessageTemplateModel model, MessageTemplate messageTemplate, bool excludeProperties)
        {
            Guard.NotNull(model, nameof(model));

            if (!excludeProperties)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(messageTemplate);
            }
        }

        private async Task PrepareMessageTemplateModelAsync(MessageTemplate template)
        {
            ViewBag.LastModelTreeJson = template.LastModelTree;
            ViewBag.LastModelTree = _messageModelProvider.GetLastModelTree(template);

            var mapper = MapperFactory.GetMapper<EmailAccount, EmailAccountModel>();
            ViewBag.EmailAccounts = await _db.EmailAccounts
                .AsNoTracking()
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();
        }

        #endregion

        #region List / Edit / Delete

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Cms.MessageTemplate.Read)]
        public IActionResult List()
        {
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Cms.MessageTemplate.Read)]
        public async Task<IActionResult> MessageTemplateList(GridCommand command, MessageTemplateListModel model)
        {
            var query = _db.MessageTemplates.AsNoTracking();

            if (model.SearchName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchName);
            }
            if (model.SearchSubject.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Subject, model.SearchSubject);
            }

            var messageTemplates = await query
                .ApplyStoreFilter(model.SearchStoreId)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<MessageTemplate, MessageTemplateModel>();
            var messageTemplateModels = await messageTemplates
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.EditUrl = Url.Action(nameof(Edit), "MessageTemplate", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<MessageTemplateModel>
            {
                Rows = messageTemplateModels,
                Total = await messageTemplates.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [Permission(Permissions.Cms.MessageTemplate.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var messageTemplate = await _db.MessageTemplates.FindByIdAsync(id, false);
            if (messageTemplate == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<MessageTemplate, MessageTemplateModel>(messageTemplate);

            await PrepareMessageTemplateModelAsync(messageTemplate);
            await PrepareStoresMappingModelAsync(model, messageTemplate, false);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.To = messageTemplate.GetLocalized(x => x.To, languageId, false, false);
                locale.ReplyTo = messageTemplate.GetLocalized(x => x.ReplyTo, languageId, false, false);
                locale.BccEmailAddresses = messageTemplate.GetLocalized(x => x.BccEmailAddresses, languageId, false, false);
                locale.Subject = messageTemplate.GetLocalized(x => x.Subject, languageId, false, false);
                locale.Body = messageTemplate.GetLocalized(x => x.Body, languageId, false, false);
                locale.Attachment1FileId = messageTemplate.GetLocalized(x => x.Attachment1FileId, languageId, false, false);
                locale.Attachment2FileId = messageTemplate.GetLocalized(x => x.Attachment2FileId, languageId, false, false);
                locale.Attachment3FileId = messageTemplate.GetLocalized(x => x.Attachment3FileId, languageId, false, false);

                var emailAccountId = messageTemplate.GetLocalized(x => x.EmailAccountId, languageId, false, false);
                locale.EmailAccountId = emailAccountId > 0 ? emailAccountId : _emailAccountSettings.DefaultEmailAccountId;
            });

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Cms.MessageTemplate.Update)]
        public async Task<IActionResult> Edit(MessageTemplateModel model, bool continueEditing, IFormCollection form)
        {
            var messageTemplate = await _db.MessageTemplates.FindByIdAsync(model.Id);
            if (messageTemplate == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, messageTemplate);
                await _storeMappingService.ApplyStoreMappingsAsync(messageTemplate, model.SelectedStoreIds);
                await UpdateLocalesAsync(messageTemplate, model);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, messageTemplate, form));
                NotifySuccess(T("Admin.ContentManagement.MessageTemplates.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), messageTemplate.Id)
                    : RedirectToAction(nameof(List));
            }

            await PrepareMessageTemplateModelAsync(messageTemplate);
            await PrepareStoresMappingModelAsync(model, messageTemplate, true);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Cms.MessageTemplate.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var messageTemplate = await _db.MessageTemplates.FindByIdAsync(id);
            if (messageTemplate == null)
            {
                return NotFound();
            }

            _db.MessageTemplates.Remove(messageTemplate);

            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.ContentManagement.MessageTemplates.Deleted"));
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Delete)]
        public async Task<IActionResult> MessageTemplateDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var messageTemplates = await _db.MessageTemplates.GetManyAsync(ids, true);

                _db.MessageTemplates.RemoveRange(messageTemplates);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Preview 

        [Permission(Permissions.Cms.MessageTemplate.Read)]
        public async Task<IActionResult> Preview(int id, bool isCampaign = false)
        {
            var model = new MessageTemplatePreviewModel();

            // TODO: (mc) Liquid > Display info about preview models
            try
            {
                CreateMessageResult result = null;

                if (isCampaign)
                {
                    var campaign = await _db.Campaigns.FindByIdAsync(id, false);
                    if (campaign == null)
                    {
                        model.Error = "The requested campaign does not exist.";
                        return View(model);
                    }

                    result = await _campaignService.PreviewAsync(campaign);
                }
                else
                {
                    var template = await _db.MessageTemplates.FindByIdAsync(id, false);
                    if (template == null)
                    {
                        model.Error = "The requested message template does not exist.";
                        return View(model);
                    }

                    var messageContext = new MessageContext
                    {
                        MessageTemplate = template,
                        TestMode = true
                    };

                    result = await _messageFactory.CreateMessageAsync(messageContext, false);
                }

                var email = result.Email;

                model.AccountEmail = email.EmailAccount?.Email ?? result.MessageContext.EmailAccount?.Email;
                model.EmailAccountId = email.EmailAccountId;
                model.Bcc = email.Bcc;
                model.Body = email.Body;
                model.From = email.From;
                model.ReplyTo = email.ReplyTo;
                model.Subject = email.Subject;
                model.To = email.To;
                model.Error = null;
                model.Token = Guid.NewGuid().ToString();
                model.BodyUrl = Url.Action("PreviewBody", new { token = model.Token });

                // INFO: (mh) (core) Our hybrid cache does not support sliding or file expirations. Whenever you need expiration logic
                // other than absolute expiration you need to switch over to native IMemoryCache.
                using (var entry = _memCache.CreateEntry("mtpreview:" + model.Token))
                {
                    entry.Value = model;
                    entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
                }
            }
            catch (Exception ex)
            {
                model.Error = ex.ToAllMessages();
            }

            return View(model);
        }

        [Permission(Permissions.Cms.MessageTemplate.Read)]
        public IActionResult PreviewBody(string token)
        {
            var body = GetPreviewMailModel(token)?.Body;

            if (body.IsEmpty())
            {
                body = "<div style='padding:20px;font-family:sans-serif;color:red'>{0}</div>".FormatCurrent(T("Admin.MessageTemplate.Preview.NoBody"));
            }

            return Content(body, "text/html");
        }

        [HttpPost]
        public IActionResult PreservePreview(string token)
        {
            // While the preview window is open, the preview model should not expire.
            GetPreviewMailModel(token);
            return Content(token);
        }

        private MessageTemplatePreviewModel GetPreviewMailModel(string token)
        {
            return _memCache.Get<MessageTemplatePreviewModel>("mtpreview:" + token);
        }

        [HttpPost]
        [Permission(Permissions.System.Message.Send)]
        public async Task<IActionResult> SendTestMail(string token, string to)
        {
            var model = GetPreviewMailModel(token);
            if (model == null)
            {
                return Json(new { success = false, message = "Preview result not available anymore. Try again." });
            }

            try
            {
                var account = (await _db.EmailAccounts.FindByIdAsync(model.EmailAccountId, false)) ?? _emailAccountService.GetDefaultEmailAccount();

                using var msg = new MailMessage(to, model.Subject, model.Body, model.From);
                using var client = await _mailService.Value.ConnectAsync(account);

                await client.SendAsync(msg);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                NotifyError(ex);
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Templates

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("save-in-file")]
        [Permission(Permissions.Cms.MessageTemplate.Update)]
        public async Task<IActionResult> SaveInFile(int id)
        {
            var template = await _db.MessageTemplates.FindByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            try
            {
                _messageTemplateService.SaveTemplate(template, Services.WorkContext.WorkingLanguage.LanguageCulture);
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), template.Id);
        }

        [HttpPost]
        [Permission(Permissions.Cms.MessageTemplate.Create)]
        public async Task<IActionResult> CopyTemplate(MessageTemplateModel model)
        {
            var template = await _db.MessageTemplates.FindByIdAsync(model.Id);
            if (template == null)
            {
                return NotFound();
            }

            try
            {
                var newTemplate = await _messageTemplateService.CopyTemplateAsync(template);
                NotifySuccess(T("Admin.ContentManagement.MessageTemplates.SuccessfullyCopied"));

                return RedirectToAction(nameof(Edit), new { id = newTemplate.Id });
            }
            catch (Exception ex)
            {
                NotifyError(ex.Message);

                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }
        }

        [Permission(Permissions.Cms.MessageTemplate.Create)]
        public async Task<IActionResult> ImportAllTemplates()
        {
            // Hidden action for admins.
            await _messageTemplateService.ImportAllTemplatesAsync(Services.WorkContext.WorkingLanguage.LanguageCulture);

            NotifySuccess("All file based message templates imported successfully.");

            return RedirectToAction(nameof(List));
        }

        #endregion
    }
}
