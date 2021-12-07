using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Forums.Controllers
{
    [Route("[area]/forum/{action=index}/{id?}")]
    public class ForumAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILanguageService _languageService;
        private readonly StoreDependingSettingHelper _settingHelper;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;
        private readonly IUrlService _urlService;
        private readonly IMessageFactory _messageFactory;
        private readonly ForumSettings _forumSettings;

        public ForumAdminController(
            SmartDbContext db,
            ILocalizedEntityService localizedEntityService,
            ILanguageService languageService,
            StoreDependingSettingHelper settingHelper,
            IStoreMappingService storeMappingService,
            IAclService aclService,
            IUrlService urlService,
            IMessageFactory messageFactory,
            ForumSettings forumSettings)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
            _languageService = languageService;
            _settingHelper = settingHelper;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
            _urlService = urlService;
            _messageFactory = messageFactory;
            _forumSettings = forumSettings;
        }

        [Permission(ForumPermissions.Read)]
        public IActionResult List()
        {
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            return View(new ForumGroupListModel());
        }

        #region Forum group

        [Permission(ForumPermissions.Read)]
        public async Task<IActionResult> ForumGroupList(GridCommand command, ForumGroupListModel model)
        {
            var query = _db.ForumGroups()
                .Include(x => x.Forums)
                .ApplyStoreFilter(model.SearchStoreId)
                .AsNoTracking();

            if (model.SearchName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchName);
            }

            var groups = await query
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command, false)
                .ToListAsync();

            var rows = groups
                .Select(x => new ForumGroupModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayOrder = x.DisplayOrder,
                    LimitedToStores = x.LimitedToStores,
                    SubjectToAcl = x.SubjectToAcl,
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    EditUrl = Url.Action("ForumGroupUpdate", "Forum", new { id = x.Id, area = "Admin" })
                })
                .ToList();

            return Json(new GridModel<ForumGroupModel>
            {
                Rows = rows,
                Total = groups.Count
            });
        }

        [Permission(ForumPermissions.Create)]
        public IActionResult ForumGroupInsert()
        {
            var model = new ForumGroupModel 
            {
                DisplayOrder = 1
            };

            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(ForumPermissions.Create)]
        public async Task<IActionResult> ForumGroupInsert(ForumGroupModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var group = MiniMapper.Map<ForumGroupModel, ForumGroup>(model);
                _db.ForumGroups().Add(group);

                await _db.SaveChangesAsync();

                var validateSlugResult = await group.ValidateSlugAsync(group.Name, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await ApplyLocales(model, group);
                await _storeMappingService.ApplyStoreMappingsAsync(group, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(group, model.SelectedCustomerRoleIds);

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.ContentManagement.Forums.ForumGroup.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(ForumGroupUpdate), new { id = group.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareForumGroupModel(model, null);

            return View(model);
        }

        [Permission(ForumPermissions.Read)]
        public async Task<IActionResult> ForumGroupUpdate(int id)
        {
            var group = await _db.ForumGroups().FindByIdAsync(id, false);
            if (group == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<ForumGroup, ForumGroupModel>();
            var model = await mapper.MapAsync(group);

            AddLocales(model.Locales, async (locale, languageId) =>
            {
                locale.Name = group.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = group.GetLocalized(x => x.Description, languageId, false, false);
                locale.SeName = await group.GetActiveSlugAsync(languageId, false, false);
            });

            await PrepareForumGroupModel(model, group);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(ForumPermissions.Update)]
        public async Task<IActionResult> ForumGroupUpdate(ForumGroupModel model, bool continueEditing)
        {
            var group = await _db.ForumGroups().FindByIdAsync(model.Id);
            if (group == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                MiniMapper.Map(model, group);

                var validateSlugResult = await group.ValidateSlugAsync(group.Name, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await ApplyLocales(model, group);
                await _storeMappingService.ApplyStoreMappingsAsync(group, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(group, model.SelectedCustomerRoleIds);

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.ContentManagement.Forums.ForumGroup.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(ForumGroupUpdate), new { id = group.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareForumGroupModel(model, group);

            return View(model);
        }

        [HttpPost]
        [Permission(ForumPermissions.Delete)]
        public async Task<IActionResult> ForumGroupDelete(int id)
        {
            var group = await _db.ForumGroups().FindByIdAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            _db.ForumGroups().Remove(group);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.ContentManagement.Forums.ForumGroup.Deleted"));

            return RedirectToAction(nameof(List));
        }

        private async Task PrepareForumGroupModel(ForumGroupModel model, ForumGroup group)
        {
            if (group != null)
            {
                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(group.CreatedOnUtc, DateTimeKind.Utc);
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(group);
                model.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(group);
            }
        }

        private async Task ApplyLocales(ForumGroupModel model, ForumGroup group)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(group, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(group, x => x.Description, localized.Description, localized.LanguageId);

                var validateSlugResult = await group.ValidateSlugAsync(localized.Name, false, localized.LanguageId);
                await _urlService.ApplySlugAsync(validateSlugResult);
            }
        }

        #endregion

        #region Forum

        [HttpPost]
        [Permission(ForumPermissions.Read)]
        public async Task<IActionResult> ForumList(int forumGroupId)
        {
            var forums = await _db.Forums()
                .AsNoTracking()
                .ApplyStandardFilter(forumGroupId)
                .ToListAsync();

            var rows = forums
                .Select(x => new ForumModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayOrder = x.DisplayOrder,
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    EditUrl = Url.Action("ForumUpdate", "Forum", new { id = x.Id, area = "Admin" })
                })
                .ToList();

            return Json(new GridModel<ForumModel>
            {
                Rows = rows,
                Total = forums.Count
            });
        }

        [Permission(ForumPermissions.Create)]
        public async Task<IActionResult> ForumInsert(int forumGroupId)
        {
            var model = new ForumModel 
            { 
                DisplayOrder = 1,
                ForumGroupId = forumGroupId
            };

            AddLocales(model.Locales);
            await PrepareForumModel(model);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(ForumPermissions.Create)]
        public async Task<IActionResult> ForumInsert(ForumModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var forum = MiniMapper.Map<ForumModel, Forum>(model);
                _db.Forums().Add(forum);

                await _db.SaveChangesAsync();

                var validateSlugResult = await forum.ValidateSlugAsync(forum.Name, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await ApplyLocales(model, forum);

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.ContentManagement.Forums.Forum.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(ForumUpdate), new { id = forum.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareForumModel(model);

            return View(model);
        }

        [Permission(ForumPermissions.Read)]
        public async Task<IActionResult> ForumUpdate(int id)
        {
            var forum = await _db.Forums().FindByIdAsync(id, false);
            if (forum == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<Forum, ForumModel>();
            var model = await mapper.MapAsync(forum);

            AddLocales(model.Locales, async (locale, languageId) =>
            {
                locale.Name = forum.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = forum.GetLocalized(x => x.Description, languageId, false, false);
                locale.SeName = await forum.GetActiveSlugAsync(languageId, false, false);
            });

            await PrepareForumModel(model);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(ForumPermissions.Update)]
        public async Task<IActionResult> ForumUpdate(ForumModel model, bool continueEditing)
        {
            var forum = await _db.Forums().FindByIdAsync(model.Id);
            if (forum == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                MiniMapper.Map(model, forum);

                var validateSlugResult = await forum.ValidateSlugAsync(forum.Name, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await ApplyLocales(model, forum);

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.ContentManagement.Forums.Forum.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(ForumUpdate), new { id = forum.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareForumModel(model);

            return View(model);
        }

        [HttpPost]
        [Permission(ForumPermissions.Delete)]
        public async Task<IActionResult> ForumDelete(int id)
        {
            var forum = await _db.Forums().FindByIdAsync(id);
            if (forum == null)
            {
                return NotFound();
            }

            // INFO: hook in ForumService deletes associated forum and topic subscriptions.
            _db.Forums().Remove(forum);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.ContentManagement.Forums.Forum.Deleted"));

            return RedirectToAction(nameof(List));
        }

        private async Task PrepareForumModel(ForumModel model)
        {
            var groups = await _db.ForumGroups()
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.ForumGroups = groups
                .Select(x => new SelectListItem { Text = x.GetLocalized(y => y.Name), Value = x.Id.ToString(), Selected = x.Id == model.ForumGroupId })
                .ToList();
        }

        private async Task ApplyLocales(ForumModel model, Forum forum)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(forum, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(forum, x => x.Description, localized.Description, localized.LanguageId);

                var validateSlugResult = await forum.ValidateSlugAsync(localized.Name, false, localized.LanguageId);
                await _urlService.ApplySlugAsync(validateSlugResult);
            }
        }

        #endregion

        #region Settings

        [Permission(ForumPermissions.Read)]
        [LoadSetting]
        public IActionResult ForumSettings(ForumSettings settings, int storeScope)
        {
            // TODO: (mg) (core) multistore settings doesn't work anymore (no override checkbox checked).
            // Maybe HtmlFieldPrefix needs to be set earlier so that StoreDependingSettingHelper can create override key names.
            var model = MiniMapper.Map<ForumSettings, ForumSettingsModel>(settings);

            model.SeoModel.MetaTitle = settings.MetaTitle;
            model.SeoModel.MetaDescription = settings.MetaDescription;
            model.SeoModel.MetaKeywords = settings.MetaKeywords;

            AddLocales(model.SeoModel.Locales, (locale, languageId) =>
            {
                locale.MetaTitle = settings.GetLocalizedSetting(x => x.MetaTitle, languageId, storeScope, false, false);
                locale.MetaDescription = settings.GetLocalizedSetting(x => x.MetaDescription, languageId, storeScope, false, false);
                locale.MetaKeywords = settings.GetLocalizedSetting(x => x.MetaKeywords, languageId, storeScope, false, false);
            });

            return View(model);
        }

        [Permission(ForumPermissions.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> ForumSettings(ForumSettingsModel model, ForumSettings settings, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return ForumSettings(settings, storeScope);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            settings.MetaTitle = model.SeoModel.MetaTitle;
            settings.MetaDescription = model.SeoModel.MetaDescription;
            settings.MetaKeywords = model.SeoModel.MetaKeywords;

            foreach (var localized in model.SeoModel.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId, storeScope);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId, storeScope);
            }

            return RedirectToAction(nameof(ForumSettings));
        }

        [Permission(ForumPermissions.Read)]
        public async Task<IActionResult> ForumSearchSettings()
        {
            // INFO: set HtmlFieldPrefix early because StoreDependingSettingHelper use it to create override key names.
            ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[ForumSearchSettings]";

            var i = 0;
            var storeScope = GetActiveStoreScopeConfiguration();
            var languages = await _languageService.GetAllLanguagesAsync(true);
            var megaSearchDescriptor = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.MegaSearch");
            var megaSearchPlusDescriptor = Services.ApplicationContext.ModuleCatalog.GetModuleByName("Smartstore.MegaSearchPlus");
            var settings = await Services.SettingFactory.LoadSettingsAsync<ForumSearchSettings>(storeScope);

            var model = MiniMapper.Map<ForumSearchSettings, ForumSearchSettingsModel>(settings);

            model.ForumFacet.Disabled = settings.ForumDisabled;
            model.ForumFacet.DisplayOrder = settings.ForumDisplayOrder;
            model.CustomerFacet.Disabled = settings.CustomerDisabled;
            model.CustomerFacet.DisplayOrder = settings.CustomerDisplayOrder;
            model.DateFacet.Disabled = settings.DateDisabled;
            model.DateFacet.DisplayOrder = settings.DateDisplayOrder;

            await _settingHelper.GetOverrideKeysAsync(settings, model, storeScope);

            foreach (var language in _languageService.GetAllLanguages(true))
            {
                var forumKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Forum, language.Id, "Forum");
                var customerKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Customer, language.Id, "Forum");
                var dateKey = FacetUtility.GetFacetAliasSettingKey(FacetGroupKind.Date, language.Id, "Forum");

                await _settingHelper.GetOverrideKeyAsync($"CustomProperties[ForumSearchSettings].ForumFacet.Locales[{i}].Alias", forumKey, storeScope);
                await _settingHelper.GetOverrideKeyAsync($"CustomProperties[ForumSearchSettings].CustomerFacet.Locales[{i}].Alias", forumKey, storeScope);
                await _settingHelper.GetOverrideKeyAsync($"CustomProperties[ForumSearchSettings].DateFacet.Locales[{i}].Alias", forumKey, storeScope);

                model.ForumFacet.Locales.Add(new ForumFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(forumKey, null, storeScope)
                });
                model.CustomerFacet.Locales.Add(new ForumFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(customerKey, null, storeScope)
                });
                model.DateFacet.Locales.Add(new ForumFacetSettingsLocalizedModel
                {
                    LanguageId = language.Id,
                    Alias = Services.Settings.GetSettingByKey<string>(dateKey, null, storeScope)
                });

                i++;
            }

            foreach (var prefix in new string[] { "Forum", "Customer", "Date" })
            {
                await _settingHelper.GetOverrideKeyAsync($"CustomProperties[ForumSearchSettings].{prefix}Facet.Disabled", prefix + "Disabled", settings, storeScope);
                await _settingHelper.GetOverrideKeyAsync($"CustomProperties[ForumSearchSettings].{prefix}Facet.DisplayOrder", prefix + "DisplayOrder", settings, storeScope);
            }


            ViewBag.IsMegaSearchInstalled = megaSearchDescriptor != null;

            ViewBag.SearchModes = settings.SearchMode.ToSelectList()
                .Where(x => megaSearchDescriptor != null || x.Value.ToInt() != (int)SearchMode.ExactMatch)
                .ToList();

            ViewBag.SearchFields = new List<SelectListItem>
            {
                new SelectListItem { Text = T("Admin.Customers.Customers.Fields.Username"), Value = "username" },
                new SelectListItem { Text = T("Forum.PostText"), Value = "text" },
            };

            return PartialView(model);
        }

        #endregion

        #region Customer

        [Permission(Permissions.Customer.SendPm)]
        [HttpPost]
        public async Task<IActionResult> SendPm(SendPrivateMessageModel model)
        {
            if (!_forumSettings.AllowPrivateMessages)
            {
                throw new SmartException(T("PrivateMessages.Disabled"));
            }

            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(model.ToCustomerId, false);

            if (customer == null)
            {
                return NotFound();
            }

            if (customer.IsGuest())
            {
                throw new SmartException(T("Common.MethodNotSupportedForGuests"));
            }

            if (ModelState.IsValid)
            {
                var pm = new PrivateMessage
                {
                    StoreId = Services.StoreContext.CurrentStore.Id,
                    ToCustomerId = customer.Id,
                    FromCustomerId = Services.WorkContext.CurrentCustomer.Id,
                    Subject = model.Subject,
                    Text = model.Message,
                    IsDeletedByAuthor = false,
                    IsDeletedByRecipient = false,
                    IsRead = false,
                    CreatedOnUtc = DateTime.UtcNow
                };

                _db.PrivateMessages().Add(pm);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreSendPM, T("ActivityLog.PublicStore.SendPM"), customer.Email);

                if (_forumSettings.NotifyAboutPrivateMessages)
                {
                    await _messageFactory.SendPrivateMessageNotificationAsync(customer, pm, Services.WorkContext.WorkingLanguage.Id);
                }

                NotifySuccess(T("Admin.Customers.Customers.SendPM.Sent"));

                return RedirectToAction("Edit", "Customer", new { id = customer.Id });
            }

            return View(model);
        }

        #endregion
    }
}
