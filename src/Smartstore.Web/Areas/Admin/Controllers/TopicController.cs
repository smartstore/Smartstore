using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Topics;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class TopicController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;
        private readonly ILinkResolver _linkResolver;
        private readonly IUrlService _urlService;
        private readonly IStoreContext _storeContext;

        public TopicController(
            SmartDbContext db,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            IAclService aclService,
            ILinkResolver linkResolver,
            IUrlService urlService,
            IStoreContext storeContext)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
            _linkResolver = linkResolver;
            _urlService = urlService;
            _storeContext = storeContext;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Cms.Topic.Read)]
        public IActionResult List()
        {
            var model = new TopicListModel
            {
                IsSingleStoreMode = _storeContext.IsSingleStoreMode()
            };

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Cms.Topic.Read)]
        public async Task<IActionResult> TopicList(GridCommand command, TopicListModel model)
        {
            var query = _db.Topics
                .AsNoTracking()
                .ApplyStoreFilter(model.SearchStoreId);

            if (model.SystemName.HasValue())
            {
                query = query.Where(x => x.SystemName.Contains(model.SystemName));
            }

            if (model.Title.HasValue())
            {
                query = query.ApplySearchFilter(model.Title, Core.Rules.LogicalRuleOperator.Or, x => x.Title, x => x.ShortTitle);
            }

            if (model.RenderAsWidget.HasValue)
            {
                query = query.Where(x => x.RenderAsWidget == model.RenderAsWidget.Value);
            }

            if (model.WidgetZone.HasValue())
            {
                query = query.Where(x => x.WidgetZone.Contains(model.WidgetZone));
            }

            var topics = await query
                .OrderBy(x => x.SystemName)
                .ApplyGridCommand(command)
                .SelectSummary()
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<Topic, TopicModel>();
            var rows = await topics
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    await PrepareTopicModelAsync(x, model);

                    model.WidgetZoneValue = string.Join(", ", x.GetWidgetZones());
                    model.CookieType = (int?)x.CookieType;
                    model.Body = string.Empty;  // Otherwise maxJsonLength could be exceeded.
                    model.Intro = string.Empty; // Otherwise grind may slow down
                    model.ViewUrl = Url.Action(nameof(Edit), "Topic", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<TopicModel>
            {
                Rows = rows,
                Total = await topics.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Cms.Topic.Delete)]
        public async Task<IActionResult> TopicDelete(GridSelection selection)
        {
            var success = false;
            var count = 0;
            var entities = await _db.Topics.GetManyAsync(selection.GetEntityIds(), true);

            if (entities.Count > 0)
            {
                try
                {
                    _db.Topics.RemoveRange(entities);
                    count = await _db.SaveChangesAsync();
                    success = true;
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            return Json(new { Success = success, Count = count });
        }

        [Permission(Permissions.Cms.Topic.Create)]
        public IActionResult Create()
        {
            var model = new TopicModel
            {
                TitleTag = "h1"
            };

            AddLocales(model.Locales);
            AddCookieTypes(model);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Cms.Topic.Create)]
        public async Task<IActionResult> Create(TopicModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                if (!model.IsPasswordProtected)
                {
                    model.Password = null;
                }

                var topic = await MapperFactory.MapAsync<TopicModel, Topic>(model);

                if (model.WidgetZone != null)
                {
                    topic.WidgetZone = string.Join(',', model.WidgetZone);
                }

                topic.CookieType = (CookieType?)model.CookieType;

                _db.Topics.Add(topic);
                await _db.SaveChangesAsync();

                var slugResult = await _urlService.SaveSlugAsync(topic, model.SeName, topic.Title.NullEmpty() ?? topic.SystemName, true);
                model.SeName = slugResult.Slug;

                await UpdateLocalesAsync(topic, model);
                await _storeMappingService.ApplyStoreMappingsAsync(topic, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(topic, model.SelectedCustomerRoleIds);

                AddCookieTypes(model, model.CookieType);
                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, topic, Request.Form));
                NotifySuccess(T("Admin.ContentManagement.Topics.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = topic.Id })
                    : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            return View(model);
        }

        [Permission(Permissions.Cms.Topic.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var topic = await _db.Topics.FindByIdAsync(id, false);
            if (topic == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = await MapperFactory.MapAsync<Topic, TopicModel>(topic);
            await PrepareTopicModelAsync(topic, model);

            model.WidgetZone = topic.WidgetZone.SplitSafe(',').ToArray();
            model.CookieType = (int?)topic.CookieType;

            await AddLocalesAsync(model.Locales, async (locale, languageId) =>
            {
                locale.ShortTitle = topic.GetLocalized(x => x.ShortTitle, languageId, false, false);
                locale.Title = topic.GetLocalized(x => x.Title, languageId, false, false);
                locale.Intro = topic.GetLocalized(x => x.Intro, languageId, false, false);
                locale.Body = topic.GetLocalized(x => x.Body, languageId, false, false);
                locale.MetaKeywords = topic.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = topic.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = topic.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await topic.GetActiveSlugAsync(languageId, false, false);
            });

            // Get menu links.
            IPagedList<MenuEntity> menus = null;
            var pageIndex = 0;

            do
            {
                menus = await _db.Menus
                    .ApplyStandardFilter(includeHidden: true)
                    .ApplySorting()
                    .ToPagedList(pageIndex++, 500)
                    .LoadAsync();

                foreach (var menu in menus)
                {
                    foreach (var item in menu.Items.Where(x => x.ProviderName != null && x.ProviderName == "entity"))
                    {
                        try
                        {
                            var link = await _linkResolver.ResolveAsync(item.Model);
                            if (link.Expression.Schema == DefaultLinkProvider.SchemaTopic && link.EntityId == topic.Id)
                            {
                                var url = Url.Action("EditItem", "Menu", new { id = item.Id, area = "Admin" });

                                var label = string.Concat(
                                    menu.Title.NullEmpty() ?? menu.SystemName.NullEmpty() ?? StringExtensions.NotAvailable,
                                    " » ",
                                    item.Title.NullEmpty() ?? link.Label.NullEmpty() ?? StringExtensions.NotAvailable);

                                model.MenuLinks[url] = label;
                            }
                        }
                        catch
                        {
                            ModelState.AddModelError(string.Empty, T("Admin.ContentManagement.Menus.Item.InvalidTargetLink", item.Model, item.Title));
                        }
                    }
                }
            }
            while (menus.HasNextPage);

            return View(model);
        }

        [HttpPost]
        [ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Cms.Topic.Update)]
        public async Task<IActionResult> Edit(TopicModel model, bool continueEditing)
        {
            var topic = await _db.Topics.FindByIdAsync(model.Id);
            if (topic == null)
            {
                return RedirectToAction(nameof(List));
            }

            if (!model.IsPasswordProtected)
            {
                model.Password = null;
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, topic);

                if (model.WidgetZone != null)
                {
                    topic.WidgetZone = string.Join(',', model.WidgetZone);
                }

                topic.CookieType = (CookieType?)model.CookieType;

                var slugResult = await _urlService.SaveSlugAsync(topic, model.SeName, topic.Title.NullEmpty() ?? topic.SystemName, true);
                model.SeName = slugResult.Slug;

                await UpdateLocalesAsync(topic, model);
                await _storeMappingService.ApplyStoreMappingsAsync(topic, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(topic, model.SelectedCustomerRoleIds);

                await _db.SaveChangesAsync();

                AddCookieTypes(model, model.CookieType);
                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, topic, Request.Form));
                NotifySuccess(T("Admin.ContentManagement.Topics.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = topic.Id })
                    : RedirectToAction(nameof(List));
            }

            // If we got this far something failed. Redisplay form.
            await PrepareTopicModelAsync(topic, model);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Cms.Topic.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var topic = await _db.Topics.FindByIdAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            if (topic.IsSystemTopic)
            {
                NotifyError(T("Admin.ContentManagement.Topics.CannotBeDeleted"));
                return RedirectToAction(nameof(Edit), new { id = topic.Id });
            }

            _db.Topics.Remove(topic);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.ContentManagement.Topics.Deleted"));
            return RedirectToAction(nameof(List));
        }

        /// <summary>
        /// (AJAX) Gets a list of all available topics.
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <param name="includeWidgets">Specifies whether to include topics which are defined to be rendered as Widgets.</param>
        /// <param name="includeHomePage">Specifies whether to include homepage text.</param>
        /// <returns>List of all topics as JSON.</returns>
        public async Task<IActionResult> AllTopics(string label, int selectedId, bool includeWidgets = false, bool includeHomePage = false)
        {
            var topics = await _db.Topics
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .Where(x => includeWidgets || !x.RenderAsWidget)
                .ToListAsync();

            var list = topics
                .Select(x =>
                {
                    var item = new ChoiceListItem
                    {
                        Id = x.Id.ToString(),
                        Text = x.GetLocalized(y => y.Title).Value.NullEmpty() ?? x.SystemName,
                        Selected = x.Id == selectedId
                    };

                    if (!item.Text.EqualsNoCase(x.SystemName))
                    {
                        item.Description = x.SystemName;
                    }

                    return item;
                })
                .ToList();

            if (label.HasValue())
            {
                list.Insert(0, new ChoiceListItem { Id = "0", Text = label, Selected = false });
            }

            if (includeHomePage)
            {
                list.Insert(0, new ChoiceListItem { Id = "-10", Text = T("Admin.ContentManagement.Homepage").Value, Selected = false });
            }

            return new JsonResult(list);
        }

        #region Helper functions

        private async Task PrepareTopicModelAsync(Topic topic, TopicModel model)
        {
            model.Url = await GetTopicUrlAsync(topic);
            model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(topic);
            model.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(topic);
            AddCookieTypes(model, model.CookieType);
        }

        private async Task UpdateLocalesAsync(Topic topic, TopicModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(topic, x => x.ShortTitle, localized.ShortTitle, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(topic, x => x.Title, localized.Title, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(topic, x => x.Intro, localized.Intro, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(topic, x => x.Body, localized.Body, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(topic, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(topic, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(topic, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);

                var slugResult = await _urlService.SaveSlugAsync(topic, localized.SeName, localized.Title.NullEmpty() ?? localized.ShortTitle, false, localized.LanguageId);
                model.SeName = slugResult.Slug;
            }
        }

        private async Task<string> GetTopicUrlAsync(Topic topic)
        {
            string url = null;

            try
            {
                if (topic.LimitedToStores)
                {
                    var storeIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(topic);
                    var currentStoreIsAuthorized = storeIds.Contains(Services.StoreContext.CurrentStore.Id);

                    if (currentStoreIsAuthorized)
                    {
                        var store = Services.StoreContext.GetStoreById(Services.StoreContext.CurrentStore.Id);
                        if (store != null)
                        {
                            url = store.GetAbsoluteUrl(Url.RouteUrl("Topic", new { SeName = await topic.GetActiveSlugAsync() }));
                        }
                    }
                }

                if (url.IsEmpty())
                {
                    url = Url.RouteUrl("Topic", new { SeName = await topic.GetActiveSlugAsync() }, Request.Scheme);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return url;
        }

        private static void AddCookieTypes(TopicModel model, int? selectedType = 0)
        {
            model.AvailableCookieTypes.AddRange(new[]
            {
                new SelectListItem
                {
                    Text = "Required",
                    Value = ((int)CookieType.Required).ToString(),
                    Selected = CookieType.Required == (CookieType?)selectedType
                },
                new SelectListItem
                {
                    Text = "Analytics",
                    Value = ((int)CookieType.Analytics).ToString(),
                    Selected = CookieType.Analytics == (CookieType?)selectedType
                },
                new SelectListItem
                {
                    Text = "ThirdParty",
                    Value = ((int)CookieType.ThirdParty).ToString(),
                    Selected = CookieType.ThirdParty == (CookieType?)selectedType
                },
                new SelectListItem
                {
                    Text = "ConsentAdUserData",
                    Value = ((int)CookieType.ConsentAdUserData).ToString(),
                    Selected = CookieType.ConsentAdUserData == (CookieType?)selectedType
                },
                new SelectListItem
                {
                    Text = "ConsentAdPersonalization",
                    Value = ((int)CookieType.ConsentAdPersonalization).ToString(),
                    Selected = CookieType.ConsentAdPersonalization == (CookieType?)selectedType
                }
            });
        }

        #endregion
    }
}
