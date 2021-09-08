using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Blog.Domain;
using Smartstore.ComponentModel;
using Smartstore.Core.Seo;
using Smartstore.Web.Modelling;

namespace Smartstore.Blog.Models
{
    [LocalizedDisplay("Admin.ContentManagement.Blog.BlogPosts.Fields.")]
    public class BlogPostModel : TabbableModel, ILocalizedModel<BlogPostLocalizedModel>
    {
        [LocalizedDisplay("Admin.Common.IsPublished")]
        public bool IsPublished { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [LocalizedDisplay("*Intro")]
        public string Intro { get; set; }

        [LocalizedDisplay("*Body")]
        public string Body { get; set; }

        [LocalizedDisplay("*PreviewDisplayType")]
        public PreviewDisplayType PreviewDisplayType { get; set; }

        [UIHint("Media"), AdditionalMetadata("album", "content")]
        [LocalizedDisplay("*Picture")]
        public int? PictureId { get; set; }

        [UIHint("Media"), AdditionalMetadata("album", "content")]
        [LocalizedDisplay("*PreviewPicture")]
        public int? PreviewPictureId { get; set; }

        [LocalizedDisplay("*SectionBg")]
        public string SectionBg { get; set; }

        [LocalizedDisplay("*AllowComments")]
        public bool AllowComments { get; set; }

        [LocalizedDisplay("*DisplayTagsInPreview")]
        public bool DisplayTagsInPreview { get; set; } = true;

        [LocalizedDisplay("*Tags")]
        public string[] Tags { get; set; }
        public MultiSelectList AvailableTags { get; set; }

        [LocalizedDisplay("*Comments")]
        public int Comments { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOnUtc { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("*MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("*Language")]
        public int? LanguageId { get; set; }
        //public List<SelectListItem> AvailableLanguages { get; set; }

        [LocalizedDisplay("*Language")]
        public string LanguageName { get; set; }

        public bool IsSingleLanguageMode { get; set; }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        public List<BlogPostLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.ContentManagement.Blog.BlogPosts.Fields.")]
    public class BlogPostLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [LocalizedDisplay("*Intro")]
        public string Intro { get; set; }

        [LocalizedDisplay("*Body")]
        public string Body { get; set; }

        [LocalizedDisplay("*MetaKeywords")]
        public string MetaKeywords { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }
    }


    public partial class BlogPostValidator : AbstractValidator<BlogPostModel>
    {
        public BlogPostValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Body).NotEmpty();
            RuleFor(x => x.PictureId)
                .NotNull()
                .When(x => x.PreviewDisplayType == PreviewDisplayType.Default || x.PreviewDisplayType == PreviewDisplayType.DefaultSectionBg);
            RuleFor(x => x.PreviewPictureId)
                .NotNull()
                .When(x => x.PreviewDisplayType == PreviewDisplayType.Preview || x.PreviewDisplayType == PreviewDisplayType.PreviewSectionBg);
        }
    }

    public class BlogPostMapper :
        IMapper<BlogPost, BlogPostModel>,
        IMapper<BlogPostModel, BlogPost>
    {
        public async Task MapAsync(BlogPost from, BlogPostModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
            to.PictureId = from.MediaFileId;
            to.PreviewPictureId = from.PreviewMediaFileId;
        }

        public Task MapAsync(BlogPostModel from, BlogPost to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.MediaFileId = from.PictureId.ZeroToNull();
            to.PreviewMediaFileId = from.PreviewPictureId.ZeroToNull();

            return Task.CompletedTask;
        }
    }
}
