using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Seo;

namespace Smartstore.News.Models
{
    [LocalizedDisplay("Admin.ContentManagement.News.NewsItems.Fields.")]
    public class NewsItemModel : TabbableModel, ILocalizedModel<NewsItemLocalizedModel>
    {
        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 3)]
        [LocalizedDisplay("*Short")]
        public string Short { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Full")]
        public string Full { get; set; }

        [UIHint("Media"), AdditionalMetadata("album", "content")]
        [LocalizedDisplay("*Picture")]
        public int? PictureId { get; set; }

        [UIHint("Media"), AdditionalMetadata("album", "content")]
        [LocalizedDisplay("*PreviewPicture")]
        public int? PreviewPictureId { get; set; }

        [LocalizedDisplay("*AllowComments")]
        public bool AllowComments { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDateUtc { get; set; }
        [LocalizedDisplay("*StartDate")]
        public string StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDateUtc { get; set; }
        [LocalizedDisplay("*EndDate")]
        public string EndDate { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOnUtc { get; set; }
        [LocalizedDisplay("Common.CreatedOn")]
        public string CreatedOn { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }

        [LocalizedDisplay("*Comments")]
        public int Comments { get; set; }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        [LocalizedDisplay("*Language")]
        public int? LanguageId { get; set; }

        [LocalizedDisplay("*Language")]
        public string LanguageName { get; set; }

        public List<NewsItemLocalizedModel> Locales { get; set; } = new();
        public string EditUrl { get; set; }
        public string CommentsUrl { get; set; }
    }

    [LocalizedDisplay("Admin.ContentManagement.News.NewsItems.Fields.")]
    public class NewsItemLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 3)]
        [LocalizedDisplay("*Short")]
        public string Short { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Full")]
        public string Full { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }
    }


    public partial class NewsItemValidator : AbstractValidator<NewsItemModel>
    {
        public NewsItemValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Short).NotEmpty();
            RuleFor(x => x.Full).NotEmpty();
        }
    }

    public class NewsItemMapper :
        IMapper<NewsItem, NewsItemModel>,
        IMapper<NewsItemModel, NewsItem>
    {
        private readonly IUrlHelper _urlHelper;
        private readonly IDateTimeHelper _dateTimeHelper;

        public NewsItemMapper(IUrlHelper urlHelper, IDateTimeHelper dateTimeHelper)
        {
            _urlHelper = urlHelper;
            _dateTimeHelper = dateTimeHelper;
        }

        public async Task MapAsync(NewsItem from, NewsItemModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            MiniMapper.Map(from, to);

            to.SeName = await from.GetActiveSlugAsync(0, true, false);
            to.PictureId = from.MediaFileId;
            to.PreviewPictureId = from.PreviewMediaFileId;
            to.Comments = from.ApprovedCommentCount + from.NotApprovedCommentCount;
            to.EditUrl = _urlHelper.Action("Edit", "News", new { id = from.Id, area = "Admin" });
            to.CommentsUrl = _urlHelper.Action("Comments", "News", new { newsItemId = from.Id, area = "Admin" });

            if (from.LanguageId.HasValue)
            {
                to.LanguageName = from.Language?.Name;
            }

            if (from.StartDateUtc.HasValue)
            {
                to.StartDate = _dateTimeHelper.ConvertToUserTime(from.StartDateUtc.Value, DateTimeKind.Utc).ToShortDateString();
            }
            if (from.EndDateUtc.HasValue)
            {
                to.EndDate = _dateTimeHelper.ConvertToUserTime(from.EndDateUtc.Value, DateTimeKind.Utc).ToShortDateString();
            }

            to.CreatedOn = _dateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc).ToShortDateString();
        }

        public Task MapAsync(NewsItemModel from, NewsItem to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            MiniMapper.Map(from, to);

            to.MediaFileId = from.PictureId.ZeroToNull();
            to.PreviewMediaFileId = from.PreviewPictureId.ZeroToNull();

            // Convert date if updated via edit page. Let MiniMapper just copy the date in all other cases.
            if (from.CreatedOnUtc.Kind != DateTimeKind.Utc)
            {
                to.CreatedOnUtc = _dateTimeHelper.ConvertToUtcTime(from.CreatedOnUtc);
            }

            if (from.StartDateUtc.HasValue && from.StartDateUtc.Value.Kind != DateTimeKind.Utc)
            {
                to.StartDateUtc = _dateTimeHelper.ConvertToUtcTime(from.StartDateUtc.Value);
            }

            if (from.EndDateUtc.HasValue && from.EndDateUtc.Value.Kind != DateTimeKind.Utc)
            {
                to.EndDateUtc = _dateTimeHelper.ConvertToUtcTime(from.EndDateUtc.Value);
            }

            return Task.CompletedTask;
        }
    }
}
