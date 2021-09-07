using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Blog.Data;
using Smartstore.Blog.Models;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Settings;

namespace Smartstore.Blog.Controllers
{
    [Area("Admin")]
    [Route("[area]/blog/[action]/{id?}")]
    public class BlogAdminController : AdminController
    {
        //private readonly BlogDbContext _dbBlog;
        //private readonly IBlogService _blogService;
        private readonly IUrlService _urlService;
        private readonly ILanguageService _languageService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ICustomerService _customerService;

        public BlogAdminController(
            //BlogDbContext dbBlog,
            //IBlogService blogService,
            IUrlService urlService,
            ILanguageService languageService,
            IDateTimeHelper dateTimeHelper,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            ICustomerService customerService)
        {
            //_dbBlog = dbBlog;
            //_blogService = blogService;
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

        //private async Task PrepareBlogPostModelAsync(BlogPostModel model, BlogPost blogPost)
        //{
        //    if (blogPost != null)
        //    {
        //        model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(blogPost);

        //        model.Tags = blogPost.Tags
        //            .SplitSafe(",")
        //            .Select(x => x = x.Trim())
        //            .ToArray();
        //    }

        //    var allTags = await _blogService.GetAllBlogPostTagsAsync(0, 0, true);
        //    model.AvailableTags = new MultiSelectList(allTags.Select(x => x.Name).ToList(), model.AvailableTags);

        //    var allLanguages = _languageService.GetAllLanguages(true);
        //    ViewBag.AvailableLanguages = allLanguages
        //        .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
        //        .ToList();

        //    model.IsSingleLanguageMode = allLanguages.Count <= 1;
        //}

        #endregion

        #region Blog posts

        // AJAX.
        //public ActionResult AllBlogPosts(string selectedIds)
        //{
        //    // TODO: (mh) (core) Use db context
        //    var query = _blogService.GetAllBlogPosts(0, null, null, 0, int.MaxValue, 0, true).SourceQuery;
        //    var pager = new FastPager<BlogPost>(query, 500);
        //    var allBlogPosts = new List<dynamic>();
        //    var ids = selectedIds.ToIntArray().ToList();

        //    while (pager.ReadNextPage(out var blogPosts))
        //    {
        //        foreach (var blogPost in blogPosts)
        //        {
        //            dynamic obj = new
        //            {
        //                blogPost.Id,
        //                blogPost.CreatedOnUtc,
        //                Title = blogPost.GetLocalized(x => x.Title).Value
        //            };

        //            allBlogPosts.Add(obj);
        //        }
        //    }

        //    var data = allBlogPosts
        //        .OrderByDescending(x => x.CreatedOnUtc)
        //        .Select(x => new ChoiceListItem
        //        {
        //            Id = x.Id.ToString(),
        //            Text = x.Title,
        //            Selected = ids.Contains(x.Id)
        //        })
        //        .ToList();

        //    return new JsonResult(data);
        //}

        public IActionResult Index()
        {
            return RedirectToAction("Posts");
        }

        [Permission(BlogPermissions.Read)]
        public IActionResult Posts()
        {
            //var allTags = _blogService.GetAllBlogPostTags(0, 0, true)
            //    .Select(x => x.Name)
            //    .ToList();

            var allLanguages = _languageService.GetAllLanguages(true);

            var model = new BlogListModel
            {
                IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode(),
                IsSingleLanguageMode = allLanguages.Count <= 1,
                SearchEndDate = DateTime.UtcNow,
                //SearchAvailableTags = new MultiSelectList(allTags)
            };

            ViewBag.AvailableLanguages = allLanguages
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            return View(model);
        }

        #endregion
    }
}
