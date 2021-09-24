using System;
using FluentValidation;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Media;

namespace Smartstore.News.Models.Public
{
    public partial class PublicNewsItemModel : EntityModelBase
    {
        public string MetaKeywords { get; set; }
        public string MetaDescription { get; set; }
        public string MetaTitle { get; set; }
        public string SeName { get; set; }
        public MetaPropertiesModel MetaProperties { get; set; } = new();
        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUTC { get; set; }

        public string Title { get; set; }
        public string Short { get; set; }
        public string Full { get; set; }

        public ImageModel PictureModel { get; set; }
        public ImageModel PreviewPictureModel { get; set; }

        public bool DisplayAdminLink { get; set; }

        public bool Published { get; set; }

        public AddNewsCommentModel AddNewComment { get; set; } = new();
        public CommentListModel Comments { get; set; } = new();
    }

    public class NewsItemValidator : AbstractValidator<PublicNewsItemModel>
    {
        public NewsItemValidator()
        {
            RuleFor(x => x.AddNewComment.CommentTitle)
                .NotEmpty()
                .When(x => x.AddNewComment != null);

            RuleFor(x => x.AddNewComment.CommentTitle)
                .Length(1, 200)
                .When(x => x.AddNewComment != null && !string.IsNullOrEmpty(x.AddNewComment.CommentTitle));

            RuleFor(x => x.AddNewComment.CommentText)
                .NotEmpty()
                .When(x => x.AddNewComment != null);
        }
    }
}
