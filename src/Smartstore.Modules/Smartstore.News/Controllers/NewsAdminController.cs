using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.News.Domain;
using Smartstore.News.Models;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.News.Controllers
{
    [Area("Admin")]
    [Route("[area]/news/[action]/{id?}")]
    public class NewsAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IUrlService _urlService;
        private readonly ILanguageService _languageService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ICustomerService _customerService;

        public NewsAdminController  (
            SmartDbContext db,
            IUrlService urlService,
            ILanguageService languageService,
            IDateTimeHelper dateTimeHelper,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            ICustomerService customerService)
        {
            _db = db;
            _urlService = urlService;
            _languageService = languageService;
            _dateTimeHelper = dateTimeHelper;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _customerService = customerService;
        }

        #region Configure settings

        [AuthorizeAdmin, Permission(NewsPermissions.Read)]
        [LoadSetting]
        public IActionResult Settings(NewsSettings settings, int storeId)
        {
            var model = MiniMapper.Map<NewsSettings, NewsSettingsModel>(settings);

            // TODO: (mh) (core) Localization is broken in core.
            model.SeoModel.MetaTitle = settings.MetaTitle;
            model.SeoModel.MetaDescription = settings.MetaDescription;
            model.SeoModel.MetaKeywords = settings.MetaKeywords;

            AddLocales(model.SeoModel.Locales, (locale, languageId) =>
            {
                locale.MetaTitle = settings.GetLocalizedSetting(x => x.MetaTitle, languageId, storeId, false, false);
                locale.MetaDescription = settings.GetLocalizedSetting(x => x.MetaDescription, languageId, storeId, false, false);
                locale.MetaKeywords = settings.GetLocalizedSetting(x => x.MetaKeywords, languageId, storeId, false, false);
            });

            return View(model);
        }

        [AuthorizeAdmin, Permission(NewsPermissions.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> Settings(NewsSettingsModel model, NewsSettings settings, int storeId)
        {
            if (!ModelState.IsValid)
            {
                return Settings(settings, storeId);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            settings.MetaTitle = model.SeoModel.MetaTitle;
            settings.MetaDescription = model.SeoModel.MetaDescription;
            settings.MetaKeywords = model.SeoModel.MetaKeywords;

            foreach (var localized in model.SeoModel.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId, storeId);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId, storeId);
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId, storeId);
            }

            return RedirectToAction("Settings");
        }

        #endregion

        #region Utilities

        private async Task UpdateLocalesAsync(NewsItem newsItem, NewsItemModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(newsItem, x => x.Title, localized.Title, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(newsItem, x => x.Short, localized.Short, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(newsItem, x => x.Full, localized.Full, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(newsItem, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(newsItem, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(newsItem, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);


                var validateSlugResult = await newsItem.ValidateSlugAsync(localized.SeName, false, localized.LanguageId);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;
            }
        }

        private async Task PrepareNewsItemModelAsync(NewsItemModel model, NewsItem newsItem)
        {
            if (newsItem != null)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(newsItem);
            }

            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
        }

        #endregion

        #region News items

        // AJAX.
        public async Task<IActionResult> AllNewsAsync(string selectedIds)
        {
            var query = _db.NewsItems().AsNoTracking();
            var pager = new FastPager<NewsItem>(query, 500);
            var allNewsItems = new List<dynamic>();
            var ids = selectedIds.ToIntArray().ToList();

            while ((await pager.ReadNextPageAsync<NewsItem>()).Out(out var newsItems))
            {
                foreach (var newsItem in newsItems)
                {
                    dynamic obj = new
                    {
                        newsItem.Id,
                        newsItem.CreatedOnUtc,
                        Title = newsItem.GetLocalized(x => x.Title).Value
                    };

                    allNewsItems.Add(obj);
                }
            }

            var data = allNewsItems
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Title,
                    Selected = ids.Contains(x.Id)
                })
                .ToList();

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(NewsPermissions.Read)]
        public IActionResult List()
        {
            var model = new NewsListModel
            {
                SearchEndDate = DateTime.UtcNow
            };

            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            return View(model);
        }

        [HttpPost]
        [Permission(NewsPermissions.Read)]
        public async Task<IActionResult> NewsItemList(GridCommand command, NewsListModel model)
        {
            var query = _db.NewsItems().Include(x => x.Language).AsNoTracking();

            query = query
                .ApplyStandardFilter(model.SearchStoreId, model.SearchLanguageId, true)
                .Where(x => x.Published == model.SearchIsPublished || model.SearchIsPublished == null);

            if (model.SearchTitle.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Title, model.SearchTitle);
            }

            if (model.SearchShort.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Short, model.SearchShort);
            }

            if (model.SearchFull.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Full, model.SearchFull);
            }

            var newsItems = await query
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var newsItemModels = await newsItems
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<NewsItem, NewsItemModel>(x);
                    if (x.StartDateUtc.HasValue)
                    {
                        model.StartDate = _dateTimeHelper.ConvertToUserTime(x.StartDateUtc.Value, DateTimeKind.Utc);
                    }
                    if (x.EndDateUtc.HasValue)
                    {
                        model.EndDate = _dateTimeHelper.ConvertToUserTime(x.EndDateUtc.Value, DateTimeKind.Utc);
                    }
                    if (x.LanguageId.HasValue)
                    {
                        model.LanguageName = x.Language?.Name;
                    }

                    model.EditUrl = Url.Action(nameof(Edit), "News", new { id = x.Id });
                    model.CommentsUrl = Url.Action(nameof(Comments), "News", new { blogPostId = x.Id });
                    model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                    model.Comments = x.ApprovedCommentCount + x.NotApprovedCommentCount;

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<NewsItemModel>
            {
                Rows = newsItemModels,
                Total = await newsItems.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(NewsPermissions.Update)]
        public async Task<IActionResult> NewsItemUpdate(NewsItemModel model)
        {
            var success = false;
            var newsItems = await _db.NewsItems().FindByIdAsync(model.Id);

            if (newsItems != null)
            {
                await MapperFactory.MapAsync(model, newsItems);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [Permission(NewsPermissions.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new NewsItemModel
            {
                Published = true,
                AllowComments = true,
                CreatedOn = DateTime.UtcNow
            };

            AddLocales(model.Locales);
            await PrepareNewsItemModelAsync(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [ValidateAntiForgeryToken]
        [Permission(NewsPermissions.Create)]
        public async Task<IActionResult> Create(NewsItemModel model, bool continueEditing, IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<NewsItemModel, NewsItem>();
                var newsItem = await mapper.MapAsync(model);

                newsItem.StartDateUtc = model.StartDate;
                newsItem.EndDateUtc = model.EndDate;
                newsItem.CreatedOnUtc = model.CreatedOn;

                _db.NewsItems().Add(newsItem);
                await _db.SaveChangesAsync();

                var validateSlugResult = await newsItem.ValidateSlugAsync(newsItem.Title, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await UpdateLocalesAsync(newsItem, model);
                await SaveStoreMappingsAsync(newsItem, model.SelectedStoreIds);
                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, newsItem, form));

                NotifySuccess(T("Admin.ContentManagement.News.NewsItems.Added"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = newsItem.Id }) : RedirectToAction(nameof(List));
            }

            await PrepareNewsItemModelAsync(model, null);

            return View(model);
        }

        [Permission(NewsPermissions.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var newsItem = await _db.NewsItems().FindByIdAsync(id, false);
            if (newsItem == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<NewsItem, NewsItemModel>(newsItem);

            AddLocales(model.Locales, async (locale, languageId) =>
            {
                locale.Title = newsItem.GetLocalized(x => x.Title, languageId, false, false);
                locale.Short = newsItem.GetLocalized(x => x.Short, languageId, false, false);
                locale.Full = newsItem.GetLocalized(x => x.Full, languageId, false, false);
                locale.MetaKeywords = newsItem.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = newsItem.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = newsItem.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await newsItem.GetActiveSlugAsync(languageId, false, false);
            });

            model.StartDate = newsItem.StartDateUtc;
            model.EndDate = newsItem.EndDateUtc;
            model.CreatedOn = newsItem.CreatedOnUtc;

            await PrepareNewsItemModelAsync(model, newsItem);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [ValidateAntiForgeryToken]
        [Permission(NewsPermissions.Update)]
        public async Task<IActionResult> Edit(NewsItemModel model, bool continueEditing, IFormCollection form)
        {
            var newsItem = await _db.NewsItems().FindByIdAsync(model.Id, false);
            if (newsItem == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, newsItem);

                newsItem.StartDateUtc = model.StartDate;
                newsItem.EndDateUtc = model.EndDate;
                newsItem.CreatedOnUtc = model.CreatedOn;

                var validateSlugResult = await newsItem.ValidateSlugAsync(newsItem.Title, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await UpdateLocalesAsync(newsItem, model);
                await SaveStoreMappingsAsync(newsItem, model.SelectedStoreIds);
                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, newsItem, form));

                NotifySuccess(T("Admin.ContentManagement.News.NewsItems.Updated"));

                return continueEditing ? RedirectToAction(nameof(Edit), new { id = newsItem.Id }) : RedirectToAction(nameof(List));
            }

            await PrepareNewsItemModelAsync(model, newsItem);

            return View(model);
        }

        [HttpPost]
        [Permission(NewsPermissions.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var newsItem = await _db.NewsItems().FindByIdAsync(id, false);
            if (newsItem == null)
            {
                return NotFound();
            }

            _db.NewsItems().Remove(newsItem);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.ContentManagement.News.NewsItems.Deleted"));
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(NewsPermissions.Delete)]
        public async Task<IActionResult> DeleteSelection(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var newsItems = await _db.NewsItems().GetManyAsync(ids, true);

                _db.NewsItems().RemoveRange(newsItems);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Comments

        [Permission(NewsPermissions.Read)]
        public IActionResult Comments(int? newsItemId)
        {
            ViewBag.NewsItemId = newsItemId;

            return View();
        }

        [HttpPost]
        [Permission(NewsPermissions.Read)]
        public async Task<IActionResult> Comments(int? newsItemId, GridCommand command)
        {
            var query = _db.CustomerContent
                    .AsNoTracking()
                    .OfType<NewsComment>();

            if (newsItemId.HasValue)
            {
                query = query.Where(x => x.NewsItemId == newsItemId.Value);
            }

            var comments = await query
                .Include(x => x.NewsItem)
                .Include(x => x.Customer)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToPagedList(command.Page - 1, command.PageSize)
                .LoadAsync();

            var commentsModel = comments.Select(newsComment =>
            {
                var commentModel = new NewsCommentModel
                {
                    Id = newsComment.Id,
                    NewsItemId = newsComment.NewsItemId,
                    NewsItemTitle = newsComment.NewsItem.GetLocalized(x => x.Title),
                    CustomerId = newsComment.CustomerId,
                    IpAddress = newsComment.IpAddress,
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(newsComment.CreatedOnUtc, DateTimeKind.Utc),
                    CommentText = newsComment.CommentText.Truncate(270, "..."),
                    CommentTitle = newsComment.CommentTitle,
                    CustomerName = newsComment.Customer.GetDisplayName(T),
                    EditNewsItemUrl = Url.Action(nameof(Edit), "News", new { id = newsComment.NewsItemId }),
                    EditCustomerUrl = Url.Action("Edit", "Customer", new { id = newsComment.CustomerId })
                };

                return commentModel;
            });

            var gridModel = new GridModel<NewsCommentModel>
            {
                Rows = commentsModel,
                Total = await comments.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(NewsPermissions.EditComment)]
        public async Task<IActionResult> DeleteCommentSelection(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var newsComments = await _db.NewsComments().GetManyAsync(ids, true);
                _db.NewsComments().RemoveRange(newsComments);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion
    }
}
