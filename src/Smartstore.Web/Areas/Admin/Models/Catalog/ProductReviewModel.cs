using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.ProductReviews.List.")]
    public class ProductReviewListModel : ModelBase
    {
        [LocalizedDisplay("*CreatedOnFrom")]
        public DateTime? CreatedOnFrom { get; set; }

        [LocalizedDisplay("*CreatedOnTo")]
        public DateTime? CreatedOnTo { get; set; }

        [LocalizedDisplay("*Rating")]
        public int[] Ratings { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.ProductReviews.Fields.")]
    public class ProductReviewModel : EntityModelBase
    {
        [LocalizedDisplay("*Product")]
        public int ProductId { get; set; }

        [LocalizedDisplay("*Product")]
        public string ProductName { get; set; }
        public string ProductTypeName { get; set; }
        public string ProductTypeLabelHint { get; set; }

        [LocalizedDisplay("*Customer")]
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        [LocalizedDisplay("*IPAddress")]
        public string IpAddress { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 6)]
        [LocalizedDisplay("*ReviewText")]
        public string ReviewText { get; set; }

        [LocalizedDisplay("*Rating")]
        public int Rating { get; set; }

        [LocalizedDisplay("*IsApproved")]
        public bool IsApproved { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Common.UpdatedOn")]
        public DateTime UpdatedOn { get; set; }

        [LocalizedDisplay("*HelpfulYesTotal")]
        public int HelpfulYesTotal { get; set; }

        [LocalizedDisplay("*HelpfulNoTotal")]
        public int HelpfulNoTotal { get; set; }

        public string EditUrl { get; set; }
        public string ProductEditUrl { get; set; }
        public string CustomerEditUrl { get; set; }
    }

    public partial class ProductReviewValidator : AbstractValidator<ProductReviewModel>
    {
        public ProductReviewValidator()
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.ReviewText).NotEmpty();
        }
    }
}
