using System;
using System.Collections.Generic;
using FluentValidation;
using Smartstore.Blog.Domain;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Media;

namespace Smartstore.Blog.Models.Public
{
    public partial class PublicBlogPostModel : EntityModelBase
    {
        // TODO: (mh) (core) We should avoid a reference to Smartstore.Web from modules as far as we can.
        // It turns out that this has a heavy impact on build performance and the amount of files being copied to target.
        // I'll start with moving common models to Smartstore.Web.Common, rest is up to you.

        public string MetaKeywords { get; set; }
        public string MetaDescription { get; set; }
        public string MetaTitle { get; set; }
        public string SeName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUTC { get; set; }
        public List<BlogPostTagModel> Tags { get; set; } = new();

        public string Title { get; set; }

        public ImageModel Images { get; set; }

        public ImageModel Preview { get; set; }

        public string Intro { get; set; }

        public string Body { get; set; }

        public string SectionBg { get; set; }

        public bool HasBgImage { get; set; }

        public bool DisplayAdminLink { get; set; }

        public bool DisplayTagsInPreview { get; set; }

        public bool IsPublished { get; set; }

        public PreviewDisplayType PreviewDisplayType { get; set; }

        public AddBlogCommentModel AddNewComment { get; set; } = new();
        public CommentListModel Comments { get; set; } = new();
    }

    public class BlogPostValidator : AbstractValidator<PublicBlogPostModel>
    {
        public BlogPostValidator()
        {
            RuleFor(x => x.AddNewComment.CommentText)
                .NotEmpty()
                .When(x => x.AddNewComment != null);
        }
    }
}
