using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.News.Domain;
using Smartstore.News.Messaging;
using Smartstore.News.Models.Public;
using Smartstore.Caching.OutputCache;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Http;
using Smartstore.Net;
using Smartstore.Web.Controllers;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.Media;
using Smartstore.News.Hooks;
using Smartstore.Caching;


namespace Smartstore.News.Controllers
{
    // TODO: (mh) (core) Remove if not needed anymore

    //public partial class NewsModelHelper
    //{

    //    private readonly SmartDbContext _db;
    //    private readonly ICommonServices _services;
    //    private readonly IMediaService _mediaService;
    //    private readonly IDateTimeHelper _dateTimeHelper;
    //    private readonly IStoreMappingService _storeMappingService;
    //    private readonly IPageAssetBuilder _pageAssetBuilder;
    //    private readonly ICacheManager _cache;
    //    private readonly Lazy<IWebHelper> _webHelper;
    //    private readonly Lazy<IActivityLogger> _activityLogger;
    //    private readonly Lazy<IMessageFactory> _messageFactory;
    //    private readonly Lazy<LinkGenerator> _linkGenerator;

    //    private readonly NewsSettings _newsSettings;
    //    private readonly LocalizationSettings _localizationSettings;
    //    private readonly CustomerSettings _customerSettings;
    //    private readonly CaptchaSettings _captchaSettings;
    //    private readonly SeoSettings _seoSettings;

    //    public NewsModelHelper(
    //        SmartDbContext db,
    //        ICommonServices services,
    //        IMediaService mediaService,
    //        IDateTimeHelper dateTimeHelper,
    //        IStoreMappingService storeMappingService,
    //        IPageAssetBuilder pageAssetBuilder,
    //        ICacheManager cache,
    //        Lazy<IWebHelper> webHelper,
    //        Lazy<IActivityLogger> activityLogger,
    //        Lazy<IMessageFactory> messageFactory,
    //        Lazy<LinkGenerator> linkGenerator,
    //        NewsSettings newsSettings,
    //        LocalizationSettings localizationSettings,
    //        CustomerSettings customerSettings,
    //        CaptchaSettings captchaSettings,
    //        SeoSettings seoSettings)
    //    {
    //        _db = db;
    //        _services = services;
    //        _mediaService = mediaService;
    //        _dateTimeHelper = dateTimeHelper;
    //        _storeMappingService = storeMappingService;
    //        _pageAssetBuilder = pageAssetBuilder;
    //        _cache = cache;
    //        _webHelper = webHelper;
    //        _activityLogger = activityLogger;
    //        _messageFactory = messageFactory;
    //        _linkGenerator = linkGenerator;

    //        _newsSettings = newsSettings;
    //        _localizationSettings = localizationSettings;
    //        _customerSettings = customerSettings;
    //        _captchaSettings = captchaSettings;
    //        _seoSettings = seoSettings;
    //    }
        
    //    public async Task<ImageModel> PrepareNewsItemPictureModelAsync(NewsItem newsItem, int? fileId)
    //    {
    //        var file = await _mediaService.GetFileByIdAsync(fileId ?? 0, MediaLoadFlags.AsNoTracking);

    //        var pictureModel = new ImageModel
    //        {
    //            File = file,
    //            ThumbSize = MediaSettings.ThumbnailSizeLg,
    //            Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? newsItem.GetLocalized(x => x.Title),
    //            Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? newsItem.GetLocalized(x => x.Title),
    //        };

    //        _services.DisplayControl.Announce(file?.File);

    //        return pictureModel;
    //    }

    //    [NonAction]
    //    protected async Task<NewsItemListModel> PrepareNewsItemListModelAsync(NewsPagingFilteringModel command)
    //    {
    //        Guard.NotNull(command, nameof(command));

    //        if (command.PageSize <= 0)
    //            command.PageSize = _newsSettings.NewsArchivePageSize;
    //        if (command.PageNumber <= 0)
    //            command.PageNumber = 1;

    //        var model = await PrepareNewsItemListModelAsync(true, null, false, command.PageNumber - 1, command.PageSize, true);
    //        return model;
    //    }

    //    [NonAction]
    //    protected async Task<NewsItemListModel> PrepareNewsItemListModelAsync(
    //        bool renderHeading,
    //        string newsHeading,
    //        bool disableCommentCount,
    //        int? pageIndex = null,
    //        int? maxPostAmount = null,
    //        bool displayPaging = false,
    //        int? maxAgeInDays = null)
    //    {
    //        var model = new NewsItemListModel
    //        {
    //            NewsHeading = newsHeading,
    //            RenderHeading = renderHeading,
    //            DisableCommentCount = disableCommentCount
    //        };

    //        DateTime? maxAge = null;
    //        if (maxAgeInDays.HasValue)
    //        {
    //            maxAge = DateTime.UtcNow.AddDays(-maxAgeInDays.Value);
    //        }

    //        var query = _db.NewsItems()
    //            .AsNoTracking()
    //            .ApplyStandardFilter(_services.StoreContext.CurrentStore.Id, _services.WorkContext.WorkingLanguage.Id, _services.WorkContext.CurrentCustomer.IsAdmin());

    //        if (maxAge.HasValue)
    //        {
    //            query = (IOrderedQueryable<NewsItem>)query.Where(n => n.CreatedOnUtc >= maxAge.Value);
    //        }

    //        var newsItems = await query
    //            .ToPagedList(pageIndex ?? 0, maxPostAmount ?? _newsSettings.NewsArchivePageSize)
    //            .LoadAsync();

    //        if (displayPaging)
    //        {
    //            model.PagingFilteringContext.LoadPagedList(newsItems);
    //        }

    //        model.NewsItems = await newsItems
    //            .SelectAsync(async x =>
    //            {
    //                var newsItemModel = new PublicNewsItemModel();
    //                await PrepareNewsItemModelAsync(newsItemModel, x, false);
    //                return newsItemModel;
    //            })
    //            .AsyncToList();

    //        Services.DisplayControl.AnnounceRange(newsItems);

    //        return model;
    //    }

    //    [NonAction]
    //    protected async Task PrepareNewsItemModelAsync(PublicNewsItemModel model, NewsItem newsItem, bool prepareComments)
    //    {
    //        Guard.NotNull(newsItem, nameof(newsItem));
    //        Guard.NotNull(model, nameof(model));

    //        Services.DisplayControl.Announce(newsItem);

    //        MiniMapper.Map(newsItem, model);

    //        model.Title = newsItem.GetLocalized(x => x.Title);
    //        model.Short = newsItem.GetLocalized(x => x.Short);
    //        model.Full = newsItem.GetLocalized(x => x.Full, true);
    //        model.MetaTitle = newsItem.GetLocalized(x => x.MetaTitle);
    //        model.MetaDescription = newsItem.GetLocalized(x => x.MetaDescription);
    //        model.MetaKeywords = newsItem.GetLocalized(x => x.MetaKeywords);
    //        model.SeName = await newsItem.GetActiveSlugAsync(ensureTwoPublishedLanguages: false);
    //        model.CreatedOn = _dateTimeHelper.ConvertToUserTime(newsItem.CreatedOnUtc, DateTimeKind.Utc);
    //        model.CreatedOnUTC = newsItem.CreatedOnUtc;
    //        model.AddNewComment.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnNewsCommentPage;
    //        model.DisplayAdminLink = _services.Permissions.Authorize(Permissions.System.AccessBackend, _services.WorkContext.CurrentCustomer);
    //        model.PictureModel = await PrepareNewsItemPictureModelAsync(newsItem, newsItem.MediaFileId);
    //        model.PreviewPictureModel = await PrepareNewsItemPictureModelAsync(newsItem, newsItem.PreviewMediaFileId);

    //        model.Comments.AllowComments = newsItem.AllowComments;
    //        model.Comments.NumberOfComments = newsItem.ApprovedCommentCount;
    //        model.Comments.AllowCustomersToUploadAvatars = _customerSettings.AllowCustomersToUploadAvatars;

    //        if (prepareComments)
    //        {
    //            var newsComments = newsItem.NewsComments.Where(n => n.IsApproved).OrderBy(pr => pr.CreatedOnUtc);
    //            foreach (var nc in newsComments)
    //            {
    //                var isGuest = nc.Customer.IsGuest();

    //                var commentModel = new CommentModel(model.Comments)
    //                {
    //                    Id = nc.Id,
    //                    CustomerId = nc.CustomerId,
    //                    CustomerName = nc.Customer.FormatUserName(_customerSettings, T, false),
    //                    CommentTitle = nc.CommentTitle,
    //                    CommentText = nc.CommentText,
    //                    CreatedOn = _dateTimeHelper.ConvertToUserTime(nc.CreatedOnUtc, DateTimeKind.Utc),
    //                    CreatedOnPretty = _services.DateTimeHelper.ConvertToUserTime(nc.CreatedOnUtc, DateTimeKind.Utc).Humanize(false),
    //                    AllowViewingProfiles = _customerSettings.AllowViewingProfiles && !isGuest,
    //                };

    //                commentModel.Avatar = nc.Customer.ToAvatarModel(null, false);

    //                model.Comments.Comments.Add(commentModel);
    //            }
    //        }

    //        ViewBag.CanonicalUrlsEnabled = _seoSettings.CanonicalUrlsEnabled;
    //        ViewBag.StoreName = _services.StoreContext.CurrentStore.Name;
    //    }


    //}
}
