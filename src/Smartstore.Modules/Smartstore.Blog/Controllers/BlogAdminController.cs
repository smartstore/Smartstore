using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Blog.Models;
using Smartstore.Blog.Services;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Blog.Controllers
{
    [Area("Admin")]
    [Route("[area]/blog/[action]/{id?}")]
    public class BlogAdminController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IBlogService _blogService;
        private readonly IUrlService _urlService;
        private readonly ILanguageService _languageService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ICustomerService _customerService;

        public BlogAdminController(
            SmartDbContext db,
            IBlogService blogService,
            IUrlService urlService,
            ILanguageService languageService,
            IDateTimeHelper dateTimeHelper,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            ICustomerService customerService)
        {
            _db = db;
            _blogService = blogService;
            _urlService = urlService;
            _languageService = languageService;
            _dateTimeHelper = dateTimeHelper;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _customerService = customerService;
        }

        #region Configure settings

        [AuthorizeAdmin, Permission(BlogPermissions.Read)]
        [LoadSetting]
        public IActionResult Configure(BlogSettings settings)
        {
            var model = MiniMapper.Map<BlogSettings, ConfigurationModel>(settings);
            return View(model);
        }

        [AuthorizeAdmin, Permission(BlogPermissions.Update)]
        [HttpPost, SaveSetting]
        public IActionResult Configure(ConfigurationModel model, BlogSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction("Configure");
        }

        #endregion

        #region Utilities

        private async Task UpdateLocalesAsync(BlogPost blogPost, BlogPostModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.Title, localized.Title, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.Intro, localized.Intro, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.Body, localized.Body, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(blogPost, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);

                var validateSlugResult = await blogPost.ValidateSlugAsync(localized.SeName, false, localized.LanguageId);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;
            }
        }

        private async Task PrepareBlogPostModelAsync(BlogPostModel model, BlogPost blogPost)
        {
            if (blogPost != null)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(blogPost);

                model.Tags = blogPost.Tags
                    .SplitSafe(",")
                    .Select(x => x = x.Trim())
                    .ToArray();
            }

            var allTags = await _blogService.GetAllBlogPostTagsAsync(0, 0, true);
            model.AvailableTags = new MultiSelectList(allTags.Select(x => x.Name).ToList(), model.AvailableTags);

            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
        }

        #endregion

        #region Blog posts

        // AJAX.
        public IActionResult AllBlogPosts(string selectedIds)
        {
            var query = _db.BlogPosts().AsNoTracking();
            var pager = new FastPager<BlogPost>(query, 500);
            var allBlogPosts = new List<dynamic>();
            var ids = selectedIds.ToIntArray().ToList();

            while (pager.ReadNextPage(out var blogPosts))
            {
                foreach (var blogPost in blogPosts)
                {
                    dynamic obj = new
                    {
                        blogPost.Id,
                        blogPost.CreatedOnUtc,
                        Title = blogPost.GetLocalized(x => x.Title).Value
                    };

                    allBlogPosts.Add(obj);
                }
            }

            var data = allBlogPosts
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
            return RedirectToAction("Posts");
        }

        [Permission(BlogPermissions.Read)]
        public async Task<IActionResult> List()
        {
            var allTags = (await _blogService.GetAllBlogPostTagsAsync(0, 0, true))
                .Select(x => x.Name)
                .ToList();

            var model = new BlogListModel
            {
                SearchEndDate = DateTime.UtcNow,
                SearchAvailableTags = new MultiSelectList(allTags)
            };

            var allLanguages = _languageService.GetAllLanguages(true);
            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.IsSingleLanguageMode = allLanguages.Count <= 1;
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            return View(model);
        }

        [Permission(BlogPermissions.Read)]
        public async Task<IActionResult> BlogPostList(GridCommand command, BlogListModel model)
        {
            var query = _db.BlogPosts().AsNoTracking();

            query = query
                .ApplyStandardFilter(
                    model.SearchStoreId,
                    model.SearchStartDate,
                    model.SearchEndDate,
                    model.SearchLanguageId,
                    model.SearchIsPublished ?? false,
                    null,
                    model.SearchTitle ?? string.Empty,
                    model.SearchIntro ?? string.Empty,
                    model.SearchBody ?? string.Empty
                 );

            var blogPosts = await query.ApplyTagFilter(model.SearchTags)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var blogPostModels = await blogPosts
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<BlogPost, BlogPostModel>(x);
                    model.EditUrl = Url.Action(nameof(Edit), "Blog", new { id = x.Id });
                    model.CommentsUrl = Url.Action(nameof(Comments), "Blog", new { filterByBlogPostId = x.Id });
                    model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<BlogPostModel>
            {
                Rows = blogPostModels,
                Total = await blogPosts.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [Permission(BlogPermissions.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new BlogPostModel
            {
                CreatedOnUtc = DateTime.UtcNow,
                AllowComments = true
            };

            await PrepareBlogPostModelAsync(model, null);
            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(BlogPermissions.Create)]
        public async Task<IActionResult> Create(BlogPostModel model, bool continueEditing, IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<BlogPostModel, BlogPost>();
                var blogPost = await mapper.MapAsync(model);
                blogPost.CreatedOnUtc = model.CreatedOnUtc;
                blogPost.StartDateUtc = model.StartDate;
                blogPost.EndDateUtc = model.EndDate;

                _db.BlogPosts().Add(blogPost);
                await _db.SaveChangesAsync();

                var validateSlugResult = await blogPost.ValidateSlugAsync(blogPost.Title, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await UpdateLocalesAsync(blogPost, model);
                await SaveStoreMappingsAsync(blogPost, model.SelectedStoreIds);
                await _db.SaveChangesAsync();
                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, blogPost, form));

                NotifySuccess(T("Admin.ContentManagement.Blog.BlogPosts.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = blogPost.Id }) : RedirectToAction("List");
            }

            await PrepareBlogPostModelAsync (model, null);

            return View(model);
        }

        [Permission(BlogPermissions.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var blogPost = await _db.BlogPosts().FindByIdAsync(id, false);

            if (blogPost == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<BlogPost, BlogPostModel>(blogPost);

            AddLocales(model.Locales, async (locale, languageId) =>
            {
                locale.Title = blogPost.GetLocalized(x => x.Title, languageId, false, false);
                locale.Intro = blogPost.GetLocalized(x => x.Intro, languageId, false, false);
                locale.Body = blogPost.GetLocalized(x => x.Body, languageId, false, false);
                locale.MetaKeywords = blogPost.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = blogPost.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = blogPost.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await blogPost.GetActiveSlugAsync(languageId, false, false);
            });

            model.CreatedOnUtc = blogPost.CreatedOnUtc;
            model.StartDate = blogPost.StartDateUtc;
            model.EndDate = blogPost.EndDateUtc;

            await PrepareBlogPostModelAsync(model, blogPost);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(BlogPermissions.Update)]
        public async Task<IActionResult> Edit(BlogPostModel model, bool continueEditing, IFormCollection form)
        {
            var blogPost = await _db.BlogPosts().FindByIdAsync(model.Id);

            if (blogPost == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, blogPost);

                blogPost.CreatedOnUtc = model.CreatedOnUtc;
                blogPost.StartDateUtc = model.StartDate;
                blogPost.EndDateUtc = model.EndDate;

                var validateSlugResult = await blogPost.ValidateSlugAsync(blogPost.Title, true, 0);
                await _urlService.ApplySlugAsync(validateSlugResult);
                model.SeName = validateSlugResult.Slug;

                await UpdateLocalesAsync(blogPost, model);
                await _db.SaveChangesAsync();
                await SaveStoreMappingsAsync(blogPost, model.SelectedStoreIds);
                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, blogPost, form));

                NotifySuccess(T("Admin.ContentManagement.Blog.BlogPosts.Updated"));

                return continueEditing ? RedirectToAction("Edit", new { id = blogPost.Id }) : RedirectToAction("List");
            }

            await PrepareBlogPostModelAsync(model, blogPost);

            return View(model);
        }

        [HttpPost]
        [Permission(BlogPermissions.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var blogPost = await _db.BlogPosts().FindByIdAsync(id);
            if (blogPost == null)
            {
                return NotFound();
            }

            _db.BlogPosts().Remove(blogPost);
            await _db.SaveChangesAsync();
            
            NotifySuccess(T("Admin.ContentManagement.Blog.BlogPosts.Deleted"));

            return RedirectToAction("List");
        }

        [HttpPost]
        [Permission(BlogPermissions.Delete)]
        public async Task<IActionResult> DeleteSelection(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var blogPosts = await _db.BlogPosts().GetManyAsync(ids, true);

                _db.BlogPosts().RemoveRange(blogPosts);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        #region Comments

        [Permission(BlogPermissions.Read)]
        public ActionResult Comments(int? filterByBlogPostId)
        {
            ViewBag.FilterByBlogPostId = filterByBlogPostId;

            return View();
        }



        #endregion
    }
}
