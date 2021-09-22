using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Seo;
using Smartstore.News.Domain;
using Smartstore.Web.Modelling;

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
        [LocalizedDisplay("*PreviewPictureId")]
        public int? PreviewPictureId { get; set; }

        [LocalizedDisplay("*AllowComments")]
        public bool AllowComments { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }

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

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

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
        public async Task MapAsync(NewsItem from, NewsItemModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
            to.PictureId = from.MediaFileId;
            to.PreviewPictureId = from.PreviewMediaFileId;
        }

        public Task MapAsync(NewsItemModel from, NewsItem to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.MediaFileId = from.PictureId.ZeroToNull();
            to.PreviewMediaFileId = from.PreviewPictureId.ZeroToNull();

            return Task.CompletedTask;
        }
    }
}
